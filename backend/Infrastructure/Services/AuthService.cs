using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Auth;
using Parlamento.Application.Users;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

public class AuthService : IAuthService
{
    private static readonly HttpClient ExternalAuthHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private readonly DatabaseContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAppTokenService _appTokenService;

    public AuthService(
        DatabaseContext context,
        IConfiguration configuration,
        IAppTokenService appTokenService)
    {
        _context = context;
        _configuration = configuration;
        _appTokenService = appTokenService;
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(UserLoginRequest request, CancellationToken cancellationToken = default)
    {
        User? user;

        if (request.Email == null)
        {
            user = await UsersWithDetails()
                .FirstOrDefaultAsync(x => x.UserName == request.UserName, cancellationToken);
        }
        else
        {
            user = await UsersWithDetails()
                .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        }

        if (user == null)
        {
            return ServiceResult<AuthResponse>.Failure(401, "This User does not exist!");
        }

        if (!PasswordHashingService.VerifyPassword(request.Password!, user.Password))
        {
            return ServiceResult<AuthResponse>.Failure(401, "Invalid Password!");
        }

        if (PasswordHashingService.NeedsRehash(user.Password))
        {
            user.Password = PasswordHashingService.HashPassword(request.Password!);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<AuthResponse>.Success(CreateAuthResponse(user));
    }

    public async Task<ServiceResult<AuthResponse>> AuthenticateGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        var identity = await ValidateGoogleTokenAsync(request, cancellationToken);
        if (identity == null)
        {
            return ServiceResult<AuthResponse>.Failure(401, "Invalid Google credential.");
        }

        var user = await UsersWithDetails()
            .FirstOrDefaultAsync(x => x.googleIDToken == identity.ProviderUserId, cancellationToken);

        if (user != null)
        {
            return ServiceResult<AuthResponse>.Success(CreateAuthResponse(user));
        }

        var newUser = new User
        {
            Email = identity.Email,
            googleIDToken = identity.ProviderUserId,
            ProfilePic = request.ProfilePic,
            UserName = identity.Name,
            Password = "external:google"
        };

        await UserPartyStatsBuilder.PopulateAsync(newUser, _context, cancellationToken);

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(CreateAuthResponse(newUser));
    }

    public async Task<ServiceResult<AuthResponse>> AuthenticateFacebookAsync(FacebookLoginRequest request, CancellationToken cancellationToken = default)
    {
        var identity = await ValidateFacebookTokenAsync(request, cancellationToken);
        if (identity == null)
        {
            return ServiceResult<AuthResponse>.Failure(401, "Invalid Facebook credential.");
        }

        var user = await UsersWithDetails()
            .FirstOrDefaultAsync(x => x.facebookIDToken == identity.ProviderUserId, cancellationToken);

        if (user != null)
        {
            return ServiceResult<AuthResponse>.Success(CreateAuthResponse(user));
        }

        var newUser = new User
        {
            Email = identity.Email,
            facebookIDToken = identity.ProviderUserId,
            ProfilePic = request.ProfilePic,
            UserName = identity.Name,
            Password = "external:facebook"
        };

        await UserPartyStatsBuilder.PopulateAsync(newUser, _context, cancellationToken);

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(CreateAuthResponse(newUser));
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _appTokenService.CreateToken(user);
        return new AuthResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = AuthenticatedUserResponse.FromUser(user)
        };
    }

    private IQueryable<User> UsersWithDetails()
    {
        return _context.Users
            .Include(user => user.Votes)
            .Include(user => user.PartyStats)
            .ThenInclude(partyStats => partyStats.PoliticalParty);
    }

    private async Task<ExternalIdentity?> ValidateGoogleTokenAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(request.GoogleIdToken!)}";
            using var response = await ExternalAuthHttpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;

            var configuredClientId = _configuration["Authentication:GoogleClientId"];
            var audience = GetString(root, "aud");
            if (!string.IsNullOrWhiteSpace(configuredClientId) &&
                !string.Equals(audience, configuredClientId, StringComparison.Ordinal))
            {
                return null;
            }

            var subject = GetString(root, "sub");
            var email = GetString(root, "email");
            var emailVerified = GetString(root, "email_verified");
            if (string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(email) ||
                string.Equals(emailVerified, "false", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var name = GetString(root, "name") ?? request.UserName ?? email;
            return new ExternalIdentity(subject, email, name);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<ExternalIdentity?> ValidateFacebookTokenAsync(
        FacebookLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                "https://graph.facebook.com/v19.0/me" +
                $"?fields=id,name,email&access_token={Uri.EscapeDataString(request.FacebookAccessToken!)}";
            using var response = await ExternalAuthHttpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;

            var userId = GetString(root, "id");
            var email = GetString(root, "email");
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            if (!await IsFacebookTokenValidForConfiguredAppAsync(request.FacebookAccessToken!, userId, cancellationToken))
            {
                return null;
            }

            var name = GetString(root, "name") ?? request.UserName ?? email;
            return new ExternalIdentity(userId, email, name);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<bool> IsFacebookTokenValidForConfiguredAppAsync(
        string accessToken,
        string userId,
        CancellationToken cancellationToken)
    {
        var appAccessToken = _configuration["Authentication:FacebookAppAccessToken"];
        if (string.IsNullOrWhiteSpace(appAccessToken))
        {
            return true;
        }

        var url =
            "https://graph.facebook.com/debug_token" +
            $"?input_token={Uri.EscapeDataString(accessToken)}" +
            $"&access_token={Uri.EscapeDataString(appAccessToken)}";
        using var response = await ExternalAuthHttpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            return false;
        }

        var isValid = data.TryGetProperty("is_valid", out var isValidProperty) &&
            isValidProperty.ValueKind == JsonValueKind.True;
        var tokenUserId = GetString(data, "user_id");
        if (!isValid || !string.Equals(tokenUserId, userId, StringComparison.Ordinal))
        {
            return false;
        }

        var configuredAppId = _configuration["Authentication:FacebookAppId"];
        var tokenAppId = GetString(data, "app_id");
        return string.IsNullOrWhiteSpace(configuredAppId) ||
            string.Equals(tokenAppId, configuredAppId, StringComparison.Ordinal);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private sealed record ExternalIdentity(string ProviderUserId, string Email, string Name);
}

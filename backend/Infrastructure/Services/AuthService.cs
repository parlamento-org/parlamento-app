using Microsoft.EntityFrameworkCore;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Auth;
using Parlamento.Application.Users;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly DatabaseContext _context;

    public AuthService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<User>> LoginAsync(UserLoginRequest request, CancellationToken cancellationToken = default)
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
            return ServiceResult<User>.Failure(401, "This User does not exist!");
        }

        if (!PasswordHashingService.VerifyPassword(request.Password!, user.Password))
        {
            return ServiceResult<User>.Failure(401, "Invalid Password!");
        }

        if (PasswordHashingService.NeedsRehash(user.Password))
        {
            user.Password = PasswordHashingService.HashPassword(request.Password!);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<User>.Success(user);
    }

    public async Task<User> AuthenticateGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UsersWithDetails()
            .FirstOrDefaultAsync(x => x.googleIDToken == request.GoogleIdToken, cancellationToken);

        if (user != null)
        {
            return user;
        }

        var newUser = new User
        {
            Email = request.Email,
            googleIDToken = request.GoogleIdToken,
            ProfilePic = request.ProfilePic,
            UserName = request.UserName,
            Password = "external:google"
        };

        await UserPartyStatsBuilder.PopulateAsync(newUser, _context, cancellationToken);

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);

        return newUser;
    }

    public async Task<User> AuthenticateFacebookAsync(FacebookLoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UsersWithDetails()
            .FirstOrDefaultAsync(x => x.facebookIDToken == request.FacebookIdToken, cancellationToken);

        if (user != null)
        {
            return user;
        }

        var newUser = new User
        {
            Email = request.Email,
            facebookIDToken = request.FacebookIdToken,
            ProfilePic = request.ProfilePic,
            UserName = request.UserName,
            Password = "external:facebook"
        };

        await UserPartyStatsBuilder.PopulateAsync(newUser, _context, cancellationToken);

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);

        return newUser;
    }

    private IQueryable<User> UsersWithDetails()
    {
        return _context.Users
            .Include(user => user.Votes)
            .Include(user => user.PartyStats)
            .ThenInclude(partyStats => partyStats.PoliticalParty);
    }
}

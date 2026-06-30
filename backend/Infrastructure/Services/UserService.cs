using Microsoft.EntityFrameworkCore;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Users;
using Parlamento.Application.Votes;
using Parlamento.Domain.Entities;
using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly DatabaseContext _context;

    public UserService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<User>> SearchAsync(string? searchString, CancellationToken cancellationToken = default)
    {
        var query = UsersWithDetails();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var loweredSearch = searchString.ToLowerInvariant();
            query = query.Where(user => user.UserName != null && user.UserName.ToLower().Contains(loweredSearch));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<User>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (existingUser != null)
        {
            return ServiceResult<User>.Failure(401, "An account with this email already exists!");
        }

        var existingUserName = await _context.Users
            .FirstOrDefaultAsync(x => x.UserName == request.UserName, cancellationToken);
        if (existingUserName != null)
        {
            return ServiceResult<User>.Failure(402, "An account with this username already exists!");
        }

        var newUser = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            Password = PasswordHashingService.HashPassword(request.Password!),
            ProfilePic = request.ProfilePic
        };

        await UserPartyStatsBuilder.PopulateAsync(newUser, _context, cancellationToken);

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<User>.Success(newUser);
    }

    public async Task<ServiceResult<User>> AddVoteAsync(VoteRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(x => x.Votes)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return ServiceResult<User>.Failure(404, "There is no User with this ID!");
        }

        var newVote = new Vote
        {
            VoteDate = DateTime.Now,
            ProjectLawID = request.ProjectLawId,
            VotingOrientation = request.VotingOrientation
        };

        user.Votes.Add(newVote);

        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<User>.Success(user);
    }

    public async Task<ServiceResult<User>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(existingUser => existingUser.PartyStats)
            .Include(existingUser => existingUser.Votes)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user == null)
        {
            return ServiceResult<User>.Failure(404, "No User found with the given id.");
        }

        user.PartyStats.RemoveAll(_ => true);
        user.Votes.RemoveAll(_ => true);

        _context.Users.Remove(user);

        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<User>.Success(user);
    }

    private IQueryable<User> UsersWithDetails()
    {
        return _context.Users
            .Include(user => user.Votes)
            .Include(user => user.PartyStats)
            .ThenInclude(partyStats => partyStats.PoliticalParty);
    }
}

using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("/user")]
public class UserController : ControllerBase
{
    private readonly DbSet<User> _dbUserSet;
    private readonly DatabaseContext _context;


    public UserController(DatabaseContext context)
    {
        this._context = context;
        this._dbUserSet = _context.Set<User>();


    }

    [HttpGet(Name = "GetUsers")]
    public Dictionary<string, List<User>> Get(string? searchString)
    {

        if (!String.IsNullOrEmpty(searchString))
        {
            searchString = searchString.ToLower();
            return new Dictionary<string, List<User>>
            {

                ["users"] = _dbUserSet.Where(user => user.UserName!.ToLower().Contains(searchString)).ToList()
            };
        }
        else
            return new Dictionary<string, List<User>>
            {

                ["users"] = _dbUserSet.ToList()
            };
    }




    [HttpPost(Name = "CreateUser")]
    public async Task<IActionResult> Create(UserDTO dto)
    {

        var user = _context.Users?.FirstOrDefault(x => x.Email == dto.email);
        if (user != null)
        {
            return StatusCode(401, "An account with this email already exists!");
        }
        var checkUserName = _context.Users?.FirstOrDefault(x => x.UserName == dto.userName);
        if (checkUserName != null)
        {
            return StatusCode(402, "An account with this username already exists!");
        }

        User newUser = new User();

        newUser.UserName = dto.userName;
        newUser.Email = dto.email;
        newUser.Password = dto.password;
        newUser.ProfilePic = dto.profilePic;

        //add an empty party stat for every party in the database
        var parties = _context.PoliticalParties?.ToList();
        foreach (var party in parties!)
        {
            PartyStats newPartyStat = new PartyStats();
            newPartyStat.PoliticalParty = party;
            newPartyStat.PartyAffectionScore = 0;
            newPartyStat.totalAmountOfProposalsVotedOn = 0;
            newPartyStat.totalAffectionPoints = 0;
            newUser.PartyStats.Add(newPartyStat);
        }

        _dbUserSet.Add(newUser);

        await _context.SaveChangesAsync();
        return Ok(newUser);
    }



    [HttpPut(Name = "AddUserVote")]
    public async Task<IActionResult> Vote(VoteDTO dto)
    {

        var user = _context.Users?.FirstOrDefault(x => x.Id == dto.userID);
        if (user == null)
        {
            return NotFound("There is no User with this ID!");
        }

        var projectLaw = _context.ProjectLaws?.FirstOrDefault(x => x.Id == dto.projectLawID);
        if (projectLaw == null)
        {
            return NotFound("There is no ProjectLaw with this ID!");
        }

        Vote newVote = new Vote();

        newVote.VoteDate = DateTime.Now;

        newVote.ProjectLaw = projectLaw;

        newVote.VotingOrientation = dto.votingOrientation;

        user.Votes.Add(newVote);


        await _context.SaveChangesAsync();
        return Ok(user);
    }



    [HttpDelete("{email}", Name = "DeleteUser")]
    public IActionResult Delete(String email)
    {
        var userQuery = _dbUserSet.Where(x => x.Email == email);

        if (!userQuery.Any())
        {
            return NotFound("No User found with the given email.");
        }

        var user = userQuery.First();
        _dbUserSet.Remove(user);

        _context.SaveChanges();

        return Ok(user);
    }



}
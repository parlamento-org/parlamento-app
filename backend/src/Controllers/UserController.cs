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

                ["users"] = _dbUserSet.Include("Votes").Include("PartyStats.PoliticalParty").Where(user => user.UserName!.ToLower().Contains(searchString)).ToList()
            };
        }
        else
            return new Dictionary<string, List<User>>
            {

                ["users"] = _dbUserSet.Include("Votes").Include("PartyStats.PoliticalParty").ToList()
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



        Vote newVote = new Vote();

        newVote.VoteDate = DateTime.Now;

        newVote.ProjectLawID = dto.projectLawID;

        newVote.VotingOrientation = dto.votingOrientation;

        user.Votes.Add(newVote);


        await _context.SaveChangesAsync();
        return Ok(user);
    }



    [HttpDelete("{id}", Name = "DeleteUser")]
    public IActionResult Delete(int id)
    {
        var userQuery = _dbUserSet.Include(user => user.PartyStats).Include(user => user.Votes).Where(x => x.Id == id);

        if (!userQuery.Any())
        {
            return NotFound("No User found with the given id.");
        }

        var user = userQuery.First();
        //delete all party stats and votes
        user.PartyStats.RemoveAll(x => true);
        user.Votes.RemoveAll(x => true);

        _dbUserSet.Remove(user);

        _context.SaveChanges();

        return Ok(user);
    }



}
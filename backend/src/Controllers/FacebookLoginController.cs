using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("/fb-login")]
public class FacebookLoginController : ControllerBase
{
    private readonly DatabaseContext _context;

    public FacebookLoginController(DatabaseContext context)
    {
        this._context = context;

    }

    [HttpPost(Name = "ValidateFacebookUser")]
    public IActionResult ValidateFacebookUser(FacebookLoginValidateDTO dto)
    {

        var user = _context.Users?.FirstOrDefault(x => x.facebookIDToken == dto.facebookIDToken);
        if (user == null)
        {
            //add the new user to the database
            var newUser = new User
            {
                Email = dto.email,
                facebookIDToken = dto.facebookIDToken,
                ProfilePic = dto.profilePic,
                UserName = dto.userName,
                Password = "facebook"
            };
            _context.Users?.Add(newUser);
            _context.SaveChanges();
            return Ok(newUser);
        }


        return Ok(user);
    }

}
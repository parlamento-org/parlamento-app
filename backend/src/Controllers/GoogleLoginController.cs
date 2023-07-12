using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("/google-login")]
public class GoogleLoginController : ControllerBase
{
    private readonly DatabaseContext _context;

    public GoogleLoginController(DatabaseContext context)
    {
        this._context = context;

    }

    [HttpPost(Name = "ValidateGoogleUser")]
    public IActionResult ValidateGoogleUser(GoogleLoginValidateDTO dto)
    {

        var user = _context.Users?.FirstOrDefault(x => x.googleIDToken == dto.googleIDToken);
        if (user == null)
        {
            //add the new user to the database
            var newUser = new User
            {
                Email = dto.email,
                googleIDToken = dto.googleIDToken,
                ProfilePic = dto.profilePic,
                UserName = dto.userName,
                Password = "google"
            };
            _context.Users?.Add(newUser);
            _context.SaveChanges();
            return Ok(newUser);
        }


        return Ok(user);
    }

}
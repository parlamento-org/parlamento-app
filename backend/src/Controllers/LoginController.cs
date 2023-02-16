using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("/user-login")]
public class LoginController : ControllerBase
{
    private readonly DbSet<User> _dbUserSet;
    private readonly DatabaseContext _context;


    public LoginController(DatabaseContext context)
    {
        this._context = context;
        this._dbUserSet = _context.Set<User>();


    }


    [HttpPost(Name = "ValidateUser")]
    public IActionResult Validate(UserValidateDTO dto)
    {

        var user = _context.Users?.FirstOrDefault(x => x.Email == dto.email);
        if (user == null)
        {
            return NotFound("There is no User with this Email!");
        }

        if (user.Password != dto.password)
        {
            return NotFound("This User exists but you have provided the wrong password!");
        }

        return Ok(user);
    }











}
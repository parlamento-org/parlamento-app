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

        var user = _context.Users?.FirstOrDefault(x => x.Email == dto.Email);
        if (user == null)
        {
            return StatusCode(401, "This User does not exist!");
        }

        if (user.Password != dto.Password)
        {
            return StatusCode(401, "Invalid Password!");
        }

        return Ok(user);
    }











}
using HuquqApi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }




    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        // Check if the model is valid
        if (!ModelState.IsValid)
            return BadRequest("Giriş məlumatı etibarsızdır.");

        // Ensure email and password are not null
        if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
        {
            return BadRequest("E-poçt və parol tələb olunur.");
        }

        // Find user by email
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
            // Create claims for the JWT token
            var authClaims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id) // Ensure user.Id is correct
            };

            // Generate the JWT token
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            // Return token and user details
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                user = new
                {
                    Id = user.Id, // Ensure Id is correctly returned
                    user.Email,
                    user.UserName,
                    user.IsPremium,
                    user.PremiumExpirationDate,
                    user.RequestCount,
                    user.MonthlyQuestionCount,
                    user.LastQuestionDate
                }
            });
        }

        // If authentication fails
        return Unauthorized("Giriş uğursuz oldu. Zəhmət olmasa məlumatlarınızı yoxlayın.");
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            IsPremium = false, 
            RequestCount = 0
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
        
            return Ok(new { Message = "Qeydiyyat uğurlu oldu!" });
        }

        
        return BadRequest(result.Errors);
    }
}

public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}
public class RegisterModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}
using HuquqApi.Dtos.UserDtos;
using HuquqApi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;

namespace HuquqApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        public AuthController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration = null)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registeDto)
        {
            if (!ModelState.IsValid)
            {
               
                return BadRequest(ModelState);
            }

            User user=await _userManager.FindByNameAsync(registeDto.UserName);
            if (user != null) return Conflict();
            user = new ()
            {
                FullName= registeDto.UserName,
                LastName = registeDto.LastName,
                UserName = registeDto.UserName,
                Email = registeDto.Email,
           
            };
          IdentityResult result=  await _userManager.CreateAsync(user, registeDto.Password);
            if (!result.Succeeded) 
                return BadRequest(result.Errors);
            await _userManager.AddToRoleAsync(user, "User");
            
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            User user = await _userManager.FindByNameAsync(loginDto.UserName);

            if (user == null)  user = await _userManager.FindByEmailAsync(loginDto.UserName);
            
            if (user == null)  return Conflict("Pasword and Mail not found");

            if (await _userManager.IsLockedOutAsync(user))
                return StatusCode(403, $"User is locked out due to multiple failed login attempts. Please try again after {user.LockoutEnd?.LocalDateTime}");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);


            if (!isPasswordValid)
            {
                await _userManager.AccessFailedAsync(user); 

                if (await _userManager.IsLockedOutAsync(user))
                {
                
                    return StatusCode(403, $"User is locked out due to multiple failed login attempts. Please try again after {user.LockoutEnd?.LocalDateTime}");
                }

                return BadRequest("Password and Mail not found");
            }

            if (isPasswordValid)
            {
                await _userManager.ResetAccessFailedCountAsync(user);

                if (user.LockoutEnd != null)
                {
                
                    user.LockoutEnd = null; 
                    await _userManager.UpdateAsync(user); 

                }

            }

            //todo jwt 
            var userRoles = await _userManager.GetRolesAsync(user);

            var audience = _configuration.GetSection("Jwt:Audience").Value;
           
            var issuer = _configuration.GetSection("Jwt:Issuer").Value;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
           
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>()
            {

               new Claim("id",user.Id),
               new Claim("UserName",user.UserName),
               new Claim("Email",user.Email),
               new Claim("FullName",user.FullName),     
               new Claim("RequestCount",user.RequestCount.ToString("")),
               new Claim("LastQuestionDate",user.LastQuestionDate.ToString("dd-MM-yyyy HH:mm:ss")),
               new Claim("FullName",user.FullName),
                new Claim("IsPremium", user.IsPremium.ToString()),
                 new Claim("PremiumExpirationDate", user.PremiumExpirationDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? string.Empty)
            };







          claims.AddRange(userRoles.Select(r=>new Claim("Role",r)).ToList());

            var sectoken = new JwtSecurityToken(   
                issuer,
                audience,
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(sectoken);
            return Ok(new { token});
        }

        [HttpGet("")]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            return Ok(new {id= user.Id,name=user.UserName });
        }
    }
}

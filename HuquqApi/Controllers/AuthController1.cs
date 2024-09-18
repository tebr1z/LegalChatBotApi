using HuquqApi.Dtos.UserDtos;
using HuquqApi.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HuquqApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController1 : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthController1(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
         

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto registeDto)
        {
            User user=await _userManager.FindByNameAsync(registeDto.UserName);
            if (user != null) return Conflict();
            user = new User()
            {
                FullName= registeDto.UserName,
                UserName= registeDto.UserName,
                Email= registeDto.Email,
            };
            return Ok();
        }


 
    
    
    }
}

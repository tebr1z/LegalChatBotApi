using HuquqApi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public AdminController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    // Kullanıcıları listele
    [HttpGet("get-users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users.Select(u => new
        {
            u.Id,
            u.UserName,
            u.Email,
            u.IsPremium,
            u.PremiumExpirationDate,
            u.RequestCount
        }).ToListAsync();

        return Ok(users);
    }

    //İstifadəçiyə premium  verin
    [HttpPost("set-premium/{userId}")]
    public async Task<IActionResult> SetPremium(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("İstifadəçi tapılmadı.");
        }
        //Premium statusu verin və 30 günlük etibarlılıq müddəti təyin edin
        user.IsPremium = true;
        user.PremiumExpirationDate = DateTime.UtcNow.AddDays(30);
        user.RequestCount = int.MaxValue; //Premium istifadəçilər üçün limitsiz sorğular

        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Kullanıcıya premium statüsü verildi." });
    }

    //İstifadəçidən premium statusu silin
    [HttpPost("remove-premium/{userId}")]
    public async Task<IActionResult> RemovePremium(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("İstifadəçi tapılmadı.");
        }

        //Premium statusunu silin və sorğu limitini 10-a təyin edin
        user.IsPremium = false;
        user.PremiumExpirationDate = null;
        user.RequestCount = 10; // Premium deyilsə, maksimum 10 sorğu

        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Premium statusu istifadəçidən silindi." });
    }
}

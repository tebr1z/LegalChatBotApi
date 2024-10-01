using HuquqApi.Dtos.UserDtos;
using HuquqApi.Model;
using HuquqApi.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[Route("api/admin")]
[ApiController]
[Authorize(Roles = "Admin")]


public class AdminController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly HuquqDbContext _context;
    private readonly IEmailService _emailService;
    public AdminController(UserManager<User> userManager, HuquqDbContext context, IEmailService emailService)
    {

        _userManager = userManager;
        _context = context;
        _emailService = emailService;
    }


    [HttpGet("get-users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users.Select(u => new
        {
            u.Id,
            u.UserName,
            u.LastName,
            u.FullName,
            u.Email,
            u.IsPremium,
            u.PremiumExpirationDate,
            u.RequestCount,
            u.EmailConfirmed,
            u.LockoutEnabled,
            u.LockoutEnd
            
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

        return Ok(new { message = "Kullanıcıya premium  verildi." });
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


    [HttpGet]
    [Route("GetAllForms")]
    public async Task<IActionResult> GetAllForms()
    {
        var forms = await _context.contactForms.ToListAsync();
        return Ok(forms);
    }


    [HttpPost]
    [Route("ApproveForm/{id}")]
    public async Task<IActionResult> ApproveForm(int id)
    {
        var form = await _context.contactForms.FindAsync(id);
        if (form == null)
        {
            return NotFound("Form not found.");
        }


        form.IsApproved = true;
        await _context.SaveChangesAsync();


        string body = string.Empty;
        using (StreamReader reader = new StreamReader("wwwroot/template/approveFormTemplate.html"))
        {
            body = reader.ReadToEnd();
        }
        body = body.Replace("{{FullName}}", form.FullName);
        body = body.Replace("{{PhoneNumber}}", form.PhoneNumber);

        _emailService.SendEmail(new List<string> { form.Email }, "Your Contact Form Has Been Approved", body);

        return Ok("Form approved and email sent.");
    }

    [HttpPut]
    [Route("UpdateRequestCount")]
    public async Task<IActionResult> UpdateRequestCount([FromBody] int newRequestCount)
    {
        var requestCountSetting = _context.Settings.FirstOrDefault(s => s.Key == "RequestCount");

        if (requestCountSetting == null)
        {
            
            requestCountSetting = new Setting
            {
                Key = "RequestCount",
                Value = newRequestCount.ToString()
            };
            _context.Settings.Add(requestCountSetting);
        }
        else
        {
           
            requestCountSetting.Value = newRequestCount.ToString();
        }

        await _context.SaveChangesAsync();
        return Ok("RequestCount Yenilendi");
    }



    [HttpPost("ban-user")]
    public async Task<IActionResult> BanUser([FromBody] UserBanDto banDto)
    {
        var user = await _userManager.FindByIdAsync(banDto.UserId);

        if (user == null)
        {
            return NotFound("Isfadeci Taoilmadi .");
        }

       
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(banDto.BanDurationInMinutes); 
        await _userManager.UpdateAsync(user); 

        return Ok($"Isfadeci {banDto.BanDurationInMinutes} deqiqe  Blok oldu");
    }


    [HttpPost("unban-user/{userId}")]
    public async Task<IActionResult> UnbanUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound("User id tapilmadi");
        }

        
        user.LockoutEnd = null; 
        await _userManager.UpdateAsync(user); 

        return Ok("Istafdeci bani acildi");
    }





}

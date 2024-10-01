
using HuquqApi.Dtos.UserDtos;
using HuquqApi.Model;
using HuquqApi.Service.Interfaces;
using HuquqApi.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Tls;
using System.Web;


namespace HuquqApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly JwtSetting _jwtSetting;
        private readonly IEmailService _emailService;
        private readonly TimeSpan otpExpiryDuration = TimeSpan.FromMinutes(10);
        public AuthController(IOptions<JwtSetting> jwtsetting, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ITokenService tokenService, IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _jwtSetting = jwtsetting.Value;
            _emailService = emailService;
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







            string token =await _userManager.GenerateEmailConfirmationTokenAsync(user);
            string link = Url.Action(nameof(VerifyEamil),"Auth", new { email = user.Email, token = token },
                Request.Scheme, Request.Host.ToString());
                
                string body = string.Empty;
            using (StreamReader stream = new StreamReader("wwwroot/template/verifyEmailTemplate.html"))
            {
                body = stream.ReadToEnd();
            }
            body = body.Replace("{{link}}", link);
            body = body.Replace("{{username}}", user.UserName);

            _emailService.SendEmail(new List<string>() { user.Email }, "verify Email", body);



            return Ok();
        } 
       
        
        
        [HttpGet("verifyEmail")]
     
        
        
        public async Task<IActionResult> VerifyEamil(string token, string email)
        {
            User user =await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound();
            await _userManager.ConfirmEmailAsync(user, token);

            return Ok("Email confirim");
        }

        [HttpPost("sendOtpToWhatsApp")]
        public async Task<IActionResult> SendOtpToWhatsApp(string phoneNumber)
        {
            
            Random random = new Random();
            string otpCode = random.Next(100000, 999999).ToString(); 

            var whatsAppService = new WhatsAppService(new HttpClient());
            var result = await whatsAppService.SendWhatsAppOtp(phoneNumber, otpCode);

            if (result is BadRequestObjectResult)
                return BadRequest("OT PUgurlu ");

            return Ok("OTP Xeta");
        }






        [HttpPost("resetPasswordSendOtp")]
        public async Task<IActionResult> SendResetPasswordOtp(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound();

           
            Random random = new Random();
            string otpCode = random.Next(000001, 999999).ToString();

            user.ResetPasswordOtp = otpCode;
            user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(10);   
            await _userManager.UpdateAsync(user);

            // OTP'yi içeren bir e-posta gönder
            string body = string.Empty;
            using (StreamReader stream = new StreamReader("wwwroot/template/otpTemplate.html"))
            {
                body = stream.ReadToEnd();
            }
            body = body.Replace("{{otpCode}}", otpCode);
            body = body.Replace("{{username}}", user.UserName);

            _emailService.SendEmail(new List<string>() { user.Email }, "Your OTP Code", body);

            return Ok("OTP kodu e-poçta adresinə göndərildi");
        }




        [HttpPost("resetPasswordWithOtp")]
        public async Task<IActionResult> ResetPasswordWithOtp(string email, string otpCode, [FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otpCode))
                return BadRequest(new { error = "Email və otp mütləqdiq!" });

            if (resetPasswordDto.Password != resetPasswordDto.RePassword)
                return BadRequest("Kod uygunlaşmır ");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return BadRequest("User Not Found");

            
            if (user.ResetPasswordOtp != otpCode || user.OtpExpiryTime < DateTime.UtcNow)
                return BadRequest("Geçərsiz Ve ya Vaxtı dolmuş Otp Kod");

            // Şifre sıfırlama
            var result = await _userManager.RemovePasswordAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            result = await _userManager.AddPasswordAsync(user, resetPasswordDto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // OTP ve süresini temizle
            user.ResetPasswordOtp = null;
            user.OtpExpiryTime = null;
            await _userManager.UpdateAsync(user);

            return Ok("Şirə dəyişdi");
        }


        [HttpPost("verifyOtp")]
        public async Task<IActionResult> VerifyOtp(string email, string otpCode)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            if (string.IsNullOrEmpty(user.ResetPasswordOtp) || user.OtpExpiryTime == null)
            {
                return BadRequest(new { error = "OTP has not been set or is invalid" });
            }

            if (user.ResetPasswordOtp != otpCode)
            {
                return BadRequest(new { error = "Invalid OTP code" });
            }

            if (DateTime.UtcNow > user.OtpExpiryTime)
            {
                return BadRequest(new { error = "OTP has expired" });
            }

            return Ok(new { message = "OTP is valid" });
        }









        [HttpPost("resetPasswordSendEmail")]
        public async Task<IActionResult> ForgotPassword(string email)
        {

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound();

            string token = await _userManager.GeneratePasswordResetTokenAsync(user);

            string frontendUrl = "http://127.0.0.1/:5500/resetPassword.html";
            string link = $"{frontendUrl}?email={user.Email}&token={HttpUtility.UrlEncode(token)}";

            string body = string.Empty;
            using (StreamReader stream = new StreamReader("wwwroot/template/forgotPasswordTemplate.html"))
            {
                body = stream.ReadToEnd();
            }
            body = body.Replace("{{link}}", link);
            body = body.Replace("{{username}}", user.UserName);

            _emailService.SendEmail(new List<string>() { user.Email }, "Reset Your Password", body);

            return Ok();
        }


        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword(string email, string token, [FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return BadRequest(new { error = "Email ve token gereklidir." });
            }

            if (resetPasswordDto.Password != resetPasswordDto.RePassword)
                return BadRequest("Şifreler eşleşmiyor.");

            token = HttpUtility.UrlDecode(token);  
            User user = await _userManager.FindByEmailAsync(email);  

            if (user == null)
                return BadRequest("User Not Foud");

            var result = await _userManager.ResetPasswordAsync(user, token, resetPasswordDto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.UpdateSecurityStampAsync(user);
            return Ok("Sifre deyisdi");
        }








        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
        
            var user = await _userManager.FindByNameAsync(loginDto.UserName) ?? await _userManager.FindByEmailAsync(loginDto.UserName);

      
            if (user == null)
                return Conflict("Password or Email not found");

            if (await _userManager.IsLockedOutAsync(user))
                return StatusCode(403, $"User is locked out due to multiple failed login attempts. Please try again after {user.LockoutEnd?.LocalDateTime}");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);

           
            if (!user.EmailConfirmed)
                return BadRequest("Please confirm your email address.");

           
            if (!isPasswordValid)
            {
                await _userManager.AccessFailedAsync(user);

          
                if (await _userManager.IsLockedOutAsync(user))
                    return StatusCode(403, $"User is locked out due to multiple failed login attempts. Please try again after {user.LockoutEnd?.LocalDateTime}");

                return BadRequest("Invalid password or email.");
            }

       
            await _userManager.ResetAccessFailedCountAsync(user);

           
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTime.UtcNow)
            {
                user.LockoutEnd = null; 
                await _userManager.UpdateAsync(user); 
            }


            var userRoles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GetToken(userRoles, user, _jwtSetting);

            return Ok(new { token });
        }

    }
}

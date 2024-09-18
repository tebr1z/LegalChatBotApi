using Azure;
using HuquqApi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ChatService _chatService;
    private readonly HuquqDbContext _dbContext; // Veritabanı bağlantısı için
    private readonly string _pdfContent;
    public ChatController(UserManager<User> userManager, ChatService chatService, HuquqDbContext dbContext)
    {
        _userManager = userManager;
        _chatService = chatService;
        _dbContext = dbContext;
   
    }


    private async Task<bool> CanUserMakeRequest(User user)
    {
        //Premium  müddəti bitibsə, premium  statusunu silin
        if (user.IsPremium && user.PremiumExpirationDate < DateTime.UtcNow)
        {
            user.IsPremium = false;
            user.PremiumExpirationDate = null;
            user.RequestCount = 10; // Premium deyilsə, maksimum 10 sorğu
            await _userManager.UpdateAsync(user);
        }

        // Premium istifadəçilər limitsiz sorğu verə bilər
        if (user.IsPremium)
        {
            return true;
        }

        // Əgər Premium  deyilsə, qalan sorğu istək yoxlayın
        if (user.RequestCount > 0)
        {
            user.RequestCount--; // Qalan sorğu istək  azaldın

            await _userManager.UpdateAsync(user);
            return true;
        }

        // Tələb etmək hüququ yoxdur
        return false;
    }


















    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        // _chatService kontrolu
        if (_chatService == null)
        {
            return StatusCode(500, "_chatService Başlamadı.");
        }

        // request kontrolu
        if (request == null)
        {
            return BadRequest("Uğursuz istək. 'request'  null.");
        }

        // request.Message kontrolu
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Uğursuz istək. 'message' Məcburdir.");
        }

        // request.UserId kontroli
        if (string.IsNullOrEmpty(request.UserId))
        {
            return BadRequest("Uğursuz istək. 'userId' Məcburdir.");
        }

        // user kontrol
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return BadRequest("User Not Found.");
        }

        if (!user.EmailConfirmed)
        {
            return BadRequest("E-poçt ünvanınız Təsdiqlənmiyib. Mesaj göndərmək üçün E-poçt adresini təsdiqləyin.");
        }

        if (!await CanUserMakeRequest(user))
        {
            return BadRequest("Aylıq istək hakkınızı doldurdunuz. Daha çox istək üçün Premium olun. ");
        }


       
        if (_dbContext == null)
        {
            return StatusCode(500, "_dbContext DataBase Not Fount");
        }

        Chat chat;

        if (!request.ChatId.HasValue || request.ChatId.Value == 0)
        {

            chat = new Chat
            {
                UserId = user.Id,
                Title = request.Message.Length > 10 ? request.Message.Substring(0, 10) : request.Message,
                CreatedAt = DateTime.UtcNow,
                UpdateTime = DateTime.UtcNow,
                Messages = new List<Message>()
            };

            _dbContext.Chats.Add(chat);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
          
            chat = await _dbContext.Chats.FirstOrDefaultAsync(c => c.Id == request.ChatId.Value);

            if (chat == null)
            {
                return BadRequest("Bu chat id uyğun chat tapilmadı");
            }
        }

        var userMessage = new Message
        {
            Content = request.Message,
            Role = "user",
            SentAt = DateTime.UtcNow,
            ChatId = chat.Id
        };
        _dbContext.Messages.Add(userMessage);

      
        string response = null;
        try
        {
            response = await _chatService.SendMessageToChatGPTAsync(request.Message, null); 
            if (string.IsNullOrEmpty(response))
            {
                return StatusCode(500, "ChatBot düzgün cavab vermədi.");
            }

          
            var assistantMessage = new Message
            {
                Content = response,
                Role = "assistant",
                SentAt = DateTime.UtcNow,
                ChatId = chat.Id
            };
            _dbContext.Messages.Add(assistantMessage);
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, $"ChatBot API Çağrısı sırasında bir xəta oldu: {ex.Message}");
        }

     
        try
        {
            _dbContext.Chats.Update(chat);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Chat DataBase Update olarakən bir xəta oldu:  {ex.Message}");
        }

       
        return Ok(new { chatId = chat.Id, userMessage = request.Message, botResponse = response });
    }


    [HttpGet("get-chats")]
    public async Task<IActionResult> GetChats(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(" İstfadəçi İD mütləqdər.");
        }

        var chats = await _dbContext.Chats
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.CreatedAt,
           
            })
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        if (chats == null || chats.Count == 0)
        {
            return NotFound("İstfadəçi ait söhbət tapılmadı.");
        }

        return Ok(chats);
    }










    [HttpPost("create-chat")] 
    public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest request)
    {
        // Validate the incoming request
        if (string.IsNullOrEmpty(request.UserId))
        {
            return BadRequest("Etibarsız sorğu. 'userId' Mütləqdir.");
        }

        if (string.IsNullOrEmpty(request.Title))
        {
            return BadRequest("Etibarsız sorğu. 'title' Mütləqdir.");
        }

        // Find the user
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return BadRequest("İstfadəçi tapılmadı.");
        }

        // Create a new chat
        var chat = new Chat
        {
            UserId = user.Id,
            Title = request.Title,
            CreatedAt = DateTime.UtcNow,
            UpdateTime = DateTime.UtcNow,
            Messages = new List<Message>()
        };

        // Save the new chat
        _dbContext.Chats.Add(chat);
        await _dbContext.SaveChangesAsync();

        return Ok(new { chatId = chat.Id });
    }




    public class CreateChatRequest
    {
        public string UserId { get; set; }
        public string Title { get; set; }
    }





}




public class SendMessageRequest
{
    public string Message { get; set; }
    public string UserId { get; set; }
    public int? ChatId { get; set; } // Add ChatId property to pass chat ID
}

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public class WhatsAppService
{
    private readonly HttpClient _httpClient;
    private const string WhatsAppApiUrl = "https://graph.facebook.com/v16.0/+994507197557/messages";
    private const string WhatsAppToken = ""; // Sizin WhatsApp token'ınız

    public WhatsAppService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IActionResult> SendWhatsAppOtp(string phoneNumber, string otpCode)
    {
        var messageData = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "text",
            text = new
            {
                body = $"Your OTP code is {otpCode}" // OTP mesajı
            }
        };

        var content = new StringContent(JsonConvert.SerializeObject(messageData), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", WhatsAppToken);

        var response = await _httpClient.PostAsync(WhatsAppApiUrl, content);

        if (response.IsSuccessStatusCode)
        {
            return new OkObjectResult("OTP Ugurlu");
        }

        return new BadRequestObjectResult("OTP WhatsApp Xtea");
    }
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Clean.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Clean.Infrastructure.Email;

public class PostmarkEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public PostmarkEmailService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task SendVacationRequestEmailAsync(
        int vacationRequestId,
        string hrEmail,
        string employeeName,
        decimal payment,
        string fromDate,
        string toDate
    )
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"];

        var approveUrl = $"{baseUrl}/api/vacation/approve?id={vacationRequestId}";
        var rejectUrl = $"{baseUrl}/api/vacation/reject?id={vacationRequestId}";

        var subject = "New Vacation Request";
        var body = $@"
                <h3>New Vacation Request</h3>
                <p><b>{employeeName}</b> requested vacation from <b>{fromDate}</b> to <b>{toDate}</b>.</p>
                <p>Estimated Payment Amount: <b>{payment:C}</b></p>
                <a href='{approveUrl}' 
                   style='background-color: #28a745; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>
                   ✅ Accept
                </a>
                &nbsp;
                <a href='{rejectUrl}' 
                   style='background-color: #dc3545; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>
                   ❌ Reject
                </a>";
        
        var fromEmail = _configuration["EmailSettings:FromEmail"];
        var messageStream = _configuration["EmailSettings:MessageStream"]; 

        var payload = new
        {
            From = fromEmail,
            To = hrEmail,
            Subject = subject,
            HtmlBody = body,
            MessageStream = messageStream 
        };

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.postmarkapp.com/email")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        request.Headers.Add("X-Postmark-Server-Token", _configuration["EmailSettings:PostmarkApiKey"]);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
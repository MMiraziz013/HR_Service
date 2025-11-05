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
        DateTime fromDate,
        DateTime toDate
    )
    {
        string baseUrl = _configuration["AppSettings:BaseUrl"];

        string approveUrl = $"{baseUrl}/api/vacation/approve?id={vacationRequestId}";
        string rejectUrl = $"{baseUrl}/api/vacation/reject?id={vacationRequestId}";

        string subject = "New Vacation Request";
        string body = $@"
                <h3>New Vacation Request</h3>
                <p><b>{employeeName}</b> requested vacation from <b>{fromDate:yyyy-MM-dd}</b> to <b>{toDate:yyyy-MM-dd}</b>.</p>
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

        var payload = new
        {
            From = fromEmail,
            To = hrEmail,
            Subject = subject,
            HtmlBody = body,
            MessageStream = "notifications"
        };

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.postmarkapp.com/email")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
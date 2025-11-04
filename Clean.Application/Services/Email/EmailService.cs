using System.Net;
using System.Net.Mail;
using Clean.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Clean.Application.Services.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var smtpHost = _configuration["EmailSettings:SmtpHost"];
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
        var fromEmail = _configuration["EmailSettings:FromEmail"];
        var fromPassword = _configuration["EmailSettings:Password"];

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(fromEmail, fromPassword),
            EnableSsl = true
        };

        var mailMessage = new MailMessage(fromEmail, to, subject, body)
        {
            IsBodyHtml = isHtml
        };

        await client.SendMailAsync(mailMessage);
    }

    public async Task SendVacationRequestEmailAsync(int vacationRequestId, string hrEmail, string employeeName, DateTime fromDate,
        DateTime toDate)
    {
        // TODO: replace with actual email
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
                </a>
            ";

        await SendEmailAsync(hrEmail, subject, body);
    }
}
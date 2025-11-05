namespace Clean.Application.Abstractions;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendVacationRequestEmailAsync(int vacationRequestId, string hrEmail, string employeeName, 
        DateTime fromDate, DateTime toDate);
}
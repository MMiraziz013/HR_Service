namespace Clean.Application.Abstractions;

public interface IEmailService
{
    Task SendVacationRequestEmailAsync(
        int vacationRequestId, 
        string hrEmail, 
        string employeeName, 
        decimal payment,
        string fromDate, 
        string toDate
    );
    
}

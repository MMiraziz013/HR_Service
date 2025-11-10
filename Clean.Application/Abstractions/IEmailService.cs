namespace Clean.Application.Abstractions;

public interface IEmailService
{
    Task SendVacationRequestEmailAsync(
        int vacationRequestId, 
        string hrEmail, 
        string employeeName, 
        string fromDate, 
        string toDate
    );
    
}

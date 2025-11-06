using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IPayrollRecordService
{
    
    Task<Response<GetPayrollRecordDto>> AddPayrollRecordAsync(AddPayrollRecordDto payrollDto);
    
    Task<Response<List<GetPayrollRecordDto>>> GetAllPayrollRecordsAsync();
    
    Task<Response<GetPayrollRecordDto>> GetPayrollRecordByIdAsync(int id);
    
    Task<Response<List<GetPayrollRecordDto>>> GetPayrollRecordsByEmployeeIdAsync(int employeeId);
    
    Task<Response<GetPayrollRecordDto>> GetLatestPayrollRecordByEmployeeIdAsync(int employeeId);
    
    Task<Response<bool>> UpdatePayrollRecordAsync(UpdatePayrollRecordDto payrollDto);
    
    Task<Response<bool>> DeletePayrollRecordAsync(int id);
    Task<Response<List<MonthPayrollDto>>> GetPayrollForLastSixMonthAsync();
    
}
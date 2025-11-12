using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IPayrollRecordService
{
    
    Task<Response<GetPayrollRecordDto>> AddPayrollRecordAsync(AddPayrollRecordDto payrollDto);
    
    Task<Response<List<GetPayrollRecordDto>>> GetAllPayrollRecordsAsync();
    
    Task<Response<GetPayrollRecordDto>> GetPayrollRecordByIdAsync(int id);
    
    Task<Response<List<GetPayrollWithSalaryDto>>> GetPayrollRecordsByEmployeeIdAsync(int employeeId);
    
    Task<Response<GetPayrollWithSalaryDto>> GetLatestPayrollRecordByEmployeeIdAsync(int employeeId); 
    Task GenerateMonthlyPayrollRecordsAsync();
    Task<Response<UpdatePayrollDto>> UpdatePayrollDeductionsAsync(UpdatePayrollDto dto);
    Task<Response<bool>> DeletePayrollRecordAsync(int id);
    Task<Response<List<MonthPayrollDto>>> GetPayrollForLastSixMonthAsync();
    Task<Response<List<PayrollGraphDto>>> GetPayrollSummaryAsync(int monthsRange);

}
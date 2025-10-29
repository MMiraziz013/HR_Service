using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Services.PayrollRecord;

public class PayrollRecordService : IPayrollRecordService
{
    private readonly IPayrollRecordRepository _payrollRecordRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ISalaryHistoryRepository _salaryHistoryRepository;

    public PayrollRecordService(IPayrollRecordRepository payrollRecordRepository, IEmployeeRepository employeeRepository, ISalaryHistoryRepository salaryHistoryRepository)
    {
        _payrollRecordRepository = payrollRecordRepository;
        _employeeRepository = employeeRepository;
        _salaryHistoryRepository = salaryHistoryRepository;
    }

    public async Task<Response<bool>> AddPayrollRecordAsync(AddPayrollRecordDto payrollDto)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(payrollDto.EmployeeId);
        if (employee is null)
        {
            return new Response<bool>(HttpStatusCode.NotFound, "Employee not found.");

        }
        var latestSalary = await _salaryHistoryRepository.GetLatestSalaryHistoryAsync(payrollDto.EmployeeId);
        if (latestSalary is null)
        {
            return new Response<bool>(HttpStatusCode.NotFound, "No Salary record found for this employee");
        }

        var payroll = new Domain.Entities.PayrollRecord
        {
            EmployeeId = payrollDto.EmployeeId,
            PeriodStart = payrollDto.PeriodStart,
            PeriodEnd = payrollDto.PeriodEnd,
            GrossPay = latestSalary.ExpectedTotal,
            Deductions = payrollDto.Deductions
        };
        var isAdded = await _payrollRecordRepository.AddAsync(payroll);
        if (isAdded == false)
        {
            return new Response<bool>(HttpStatusCode.BadRequest, "Error while adding new payroll record.");
        }

        return new Response<bool>(HttpStatusCode.OK, $"Payroll record for {employee.FirstName} was added successfully");


    }

    public async Task<Response<List<GetPayrollRecordDto>>> GetAllPayrollRecordsAsync()
    {
        var records = await _payrollRecordRepository.GetAllAsync();
        if (records.Count == 0)
        {
            return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.NotFound, "No payroll records were found.");
        }

        var dto = records.Select(p => new GetPayrollRecordDto
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = p.Employee.FirstName,
            CreatedAt = p.CreatedAt,
            GrossPay = p.GrossPay,
            Deductions = p.Deductions,
            NetPay = p.NetPay,
            PeriodStart = p.PeriodStart,
            PeriodEnd = p.PeriodEnd
        }).ToList();

        return new Response<List<GetPayrollRecordDto>>(
            HttpStatusCode.OK, "Payroll records retrieved successfully!",dto);
    }

    public async Task<Response<GetPayrollRecordDto>> GetPayrollRecordByIdAsync(int id)
    {
        var record = await _payrollRecordRepository.GetByIdAsync(id);
        if (record is null)
        {
            return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, "Payroll record is not found.");
        }

        var mapped = new GetPayrollRecordDto
        {
          Id=record.Id,
          CreatedAt = record.CreatedAt,
          GrossPay = record.GrossPay,
          Deductions = record.Deductions,
          NetPay = record.NetPay,
          PeriodStart = record.PeriodStart,
          PeriodEnd = record.PeriodEnd,
          EmployeeId = record.EmployeeId,
          EmployeeName = record.Employee.FirstName
        };

        return new Response<GetPayrollRecordDto>(
            HttpStatusCode.OK, 
            "Payroll record is retrieved successfully!",
            mapped);

    }

    public async Task<Response<List<GetPayrollRecordDto>>> GetPayrollRecordsByEmployeeIdAsync(int employeeId)
    {
        if (await _employeeRepository.GetEmployeeByIdAsync(employeeId) is null)
        {
            return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.NotFound, $"Employee with ID:{employeeId} not found.");

        }
        var record = await _payrollRecordRepository.GetByEmployeeIdAsync(employeeId);
        if (record.Count==0)
        {
            return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.NotFound, $"Payroll records for employee with ID:{employeeId} are not found.");
        }

        var mapped = record.Select(r => new GetPayrollRecordDto
        {
            Id = r.Id,
            CreatedAt = r.CreatedAt,
            GrossPay = r.GrossPay,
            Deductions = r.Deductions,
            NetPay = r.NetPay,
            PeriodStart = r.PeriodStart,
            PeriodEnd = r.PeriodEnd,
            EmployeeId = r.EmployeeId,
            EmployeeName = r.Employee.FirstName
        }).ToList();
       

        return new Response<List<GetPayrollRecordDto>>(
            HttpStatusCode.OK, 
            "Payroll record is retrieved successfully!",
            mapped);

    }

    public async Task<Response<GetPayrollRecordDto>> GetLatestPayrollRecordByEmployeeIdAsync(int employeeId)
    {
        if (await _employeeRepository.GetEmployeeByIdAsync(employeeId) is null)
        {
            return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, $"Employee with ID:{employeeId} not found.");
        }

        var record = await _payrollRecordRepository.GetLatestByEmployeeIdAsync(employeeId);
       
        if (record is null)
        {
            return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, $"Latest record for employee with ID:{employeeId} not found.");
        }

        var mapped = new GetPayrollRecordDto
        {
            Id = record.Id,
            CreatedAt = record.CreatedAt,
            GrossPay = record.GrossPay,
            Deductions = record.Deductions,
            NetPay = record.NetPay,
            PeriodStart = record.PeriodStart,
            PeriodEnd = record.PeriodEnd,
            EmployeeId = record.EmployeeId,
            EmployeeName = record.Employee.FirstName
        };
        return new Response<GetPayrollRecordDto>(
            HttpStatusCode.OK,
            message: "Records for employee retrieved successfully",
            mapped);

    }

    public async Task<Response<bool>> UpdatePayrollRecordAsync(UpdatePayrollRecordDto payrollDto)
    {
        var exists = await _payrollRecordRepository.GetByIdAsync(payrollDto.Id);
        if (exists is null)
        {
            return new Response<bool>(HttpStatusCode.NotFound, "Payroll record is not found.");
        }

        exists.Deductions = payrollDto.Deductions;

        var isUpdated = await _payrollRecordRepository.UpdateAsync(exists);
        if (isUpdated == false)
        {
            return new Response<bool>(HttpStatusCode.InternalServerError, "Payroll record could not be updated.");
        }

        return new Response<bool>(HttpStatusCode.OK, "Record is updated successfully!",true);

    }

    public async Task<Response<bool>> DeletePayrollRecordAsync(int id)
    {
        var isDeleted = await _payrollRecordRepository.DeleteAsync(id);
        if (isDeleted == false)
        {
            return new Response<bool>(HttpStatusCode.BadRequest, "Failed to delete payroll record.");
        }

        return new Response<bool>(HttpStatusCode.OK, "Payroll record is deleted successfully", true);

    }
}
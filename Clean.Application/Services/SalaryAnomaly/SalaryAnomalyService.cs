using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryAnomaly;

namespace Clean.Application.Services.SalaryAnomaly;

public class SalaryAnomalyService : ISalaryAnomalyService
{
    private readonly ISalaryAnomalyRepository _repository;
    private readonly ISalaryHistoryRepository _salaryRepository;
    private readonly IPayrollRecordRepository _payrollRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public SalaryAnomalyService(ISalaryAnomalyRepository repository, ISalaryHistoryRepository salaryRepository, IPayrollRecordRepository payrollRepository,IEmployeeRepository employeeRepository)
    {
        _repository = repository;
        _salaryRepository = salaryRepository;
        _payrollRepository = payrollRepository;
        _employeeRepository = employeeRepository;
    }
    
   public async Task<Response<int>> GenerateAnomaliesAsync()
{
    const float deviationThreshold = 10f;
    
    var payrolls = await _payrollRepository.GetLatestPayrollAsync();

    if (payrolls == null || !payrolls.Any())
        return new Response<int>(HttpStatusCode.OK, "No payroll data found.", 0);

    int anomaliesCreated = 0;


    var grouped = payrolls
        .Where(p => p != null)
        .GroupBy(p => new { p.Employee.DepartmentId,p.Employee.Position})
        .Select(g => new
        {
            g.Key.DepartmentId,
            g.Key.Position,
            AverageGrossPay = g.Average(x => x!.GrossPay),
            Payrolls = g.ToList()
        })
        .ToList();

    foreach (var group in grouped)
    {
        foreach (var payroll in group.Payrolls)
        {
            var actualNet = payroll.NetPay;
            var avgGross = group.AverageGrossPay;

            if (avgGross == 0) continue;

            var deviationPercent = (float)(((actualNet - avgGross) / avgGross) * 100m);

            if (Math.Abs(deviationPercent) > deviationThreshold)
            {
                var exists = await _repository.ExistsForEmployeeAndMonthAsync(
                    payroll.EmployeeId,
                    payroll.PeriodEnd);

                if (exists) continue;

                var anomaly = new Domain.Entities.SalaryAnomaly
                {
                    EmployeeId = payroll.EmployeeId,
                    ExpectedAmount = avgGross,
                    ActualAmount = actualNet,
                    DeviationPercent = deviationPercent,
                    Month = payroll.PeriodEnd,
                    IsReviewed = false
                };

                await _repository.AddAsync(anomaly);
                anomaliesCreated++;
            }
        }
    }

    return new Response<int>(
        HttpStatusCode.OK,
        $"{anomaliesCreated} salary anomalies created successfully.",
        anomaliesCreated);
}

    public async  Task<PaginatedResponse<GetSalaryAnomalyDto>> GetAllAsync()
    {
        var anomalies = await _repository.GetAllAsync();
        if (!anomalies.Any())
        {
            return new PaginatedResponse<GetSalaryAnomalyDto>(
                new List<GetSalaryAnomalyDto>(),
                1,
                1,
                0
            )
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = "No salary anomalies are found"
            };
        }

        
        var mapped = anomalies.Select(h => new GetSalaryAnomalyDto
            {
                Id = h.Id,
                ActualAmount = h.ActualAmount,
                ExpectedAmount = h.ExpectedAmount,
                DeviationPercent = h.DeviationPercent,
                EmployeeId = h.EmployeeId,
                EmployeeName = h.Employee.FirstName,
                IsViewed = h.IsReviewed,
                Month = h.Month,
                ReviewComment = h.ReviewComment
            }).ToList();

            return new PaginatedResponse<GetSalaryAnomalyDto>(
                mapped,
                pageNumber: 1,
                pageSize: mapped.Count,
                totalRecords: mapped.Count)
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Salary anomalies retrieved successfully."
            };
        
        
    }

    public async Task<Response<List<GetSalaryAnomalyDto>>> GetUnviewedAsync()
    {
        var unviewed = await _repository.GetUnviewedAsync();
        if (!unviewed.Any())
        {
            return new Response<List<GetSalaryAnomalyDto>>(
                HttpStatusCode.NotFound,
                message: "No unviewed salaries.");
        }

        var mapped = unviewed.Select(h => new GetSalaryAnomalyDto
        {
            Id = h.Id,
            ExpectedAmount = h.ExpectedAmount,
            ActualAmount = h.ActualAmount,
            DeviationPercent = h.DeviationPercent,
            EmployeeId = h.EmployeeId,
            EmployeeName = $"{h.Employee.FirstName} {h.Employee.LastName}",
            IsViewed = h.IsReviewed,
            Month = h.Month,
            ReviewComment = h.ReviewComment
        }).ToList();

        return new Response<List<GetSalaryAnomalyDto>>(
            HttpStatusCode.OK,
            message: "Salary anomalies retrieved successfully.",
            mapped);
    }

    public async Task<Response<GetSalaryAnomalyDto>> MarkAsViewedAsync(int id)
    {
        var anomaly = await _repository.GetByIdAsync(id);
        if (anomaly is null)
        {
            return new Response<GetSalaryAnomalyDto>(
                HttpStatusCode.NotFound,
                $"Anomaly with ID {id} is not found.");
        }

        if (anomaly.IsReviewed )
        {
            return new Response<GetSalaryAnomalyDto>(
                HttpStatusCode.BadRequest,
                "Salary anomaly is already reviewed");
        }
        anomaly.IsReviewed = true;
        var updated = await _repository.UpdateAsync(anomaly);

        if (!updated)
        {
            return new Response<GetSalaryAnomalyDto>(
                HttpStatusCode.InternalServerError,
                "Failed to mark anomaly as viewed, try again!");
        }
        var dto = new GetSalaryAnomalyDto
        {
            Id = anomaly.Id,
            EmployeeId = anomaly.EmployeeId,
            EmployeeName = $"{anomaly.Employee.FirstName} {anomaly.Employee.LastName}",
            ExpectedAmount = anomaly.ExpectedAmount,
            ActualAmount = anomaly.ActualAmount,
            DeviationPercent = anomaly.DeviationPercent,
            IsViewed = anomaly.IsReviewed,
            Month = anomaly.Month,
            ReviewComment = anomaly.ReviewComment
        };

        return new Response<GetSalaryAnomalyDto>(
            HttpStatusCode.OK,
            "Salary anomaly is marked as viewed successfully.",
            dto);

    }

    public async Task<Response<GetSalaryAnomalyDto>> AddReviewCommentAsync(int id,string reviewComment)
    {
        var anomaly = await _repository.GetByIdAsync(id);
        if (anomaly is null)
        {
            return new Response<GetSalaryAnomalyDto>(
                HttpStatusCode.NotFound,
                "Anomaly is not found.");
        }

        var dto = new GetSalaryAnomalyDto
        {
            Id = anomaly.Id,
            EmployeeId = anomaly.EmployeeId,
            EmployeeName = $"{anomaly.Employee.FirstName} {anomaly.Employee.LastName}",
            ExpectedAmount = anomaly.ExpectedAmount,
            ActualAmount = anomaly.ActualAmount,
            DeviationPercent = anomaly.DeviationPercent,
            IsViewed = anomaly.IsReviewed,
            Month = anomaly.Month,
            ReviewComment = anomaly.ReviewComment

        };
        if (anomaly.IsReviewed ==false)
        {
            return new Response<GetSalaryAnomalyDto>(
                HttpStatusCode.BadRequest,
                "Unreviewed anomaly cannot be commented!");
        }
        
        anomaly.ReviewComment = reviewComment;
        await _repository.UpdateAsync(anomaly);
        return new Response<GetSalaryAnomalyDto>(
            HttpStatusCode.Accepted,
            "Comment is added successfully",
            dto);
        
        
        
    }

    public async Task<Response<bool>> DeleteAsync(int id)
    {
        var isDeleted = await _repository.DeleteAnomalyAsync(id);
        if (isDeleted == false)
        {
            return new Response<bool>(
                HttpStatusCode.BadRequest,
                "Failed to delete salary anomaly.");
        }

        return new Response<bool>(
            HttpStatusCode.OK,
            "Salary anomaly is deleted!");

    }

    public async Task<Response<List<GetSalaryAnomalyDto>>> GetAnomalyByEmployeeId(int id)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
        if (employee is null)
        {
            return new Response<List<GetSalaryAnomalyDto>>(
                HttpStatusCode.NotFound,
                $"Employee with ID:{id} is not found.");
        }

        var anomalies = await _repository.GetByEmployeeIdAsync(id);
        if (!anomalies.Any())
        {
            return new Response<List<GetSalaryAnomalyDto>>(
                HttpStatusCode.NotFound,
                $"No anomalies were found.");
        }

        var mapped = anomalies.Select(a => new GetSalaryAnomalyDto
        {
            Id = a.Id,
            ActualAmount = a.ActualAmount,
            ExpectedAmount = a.ExpectedAmount,
            DeviationPercent = a.DeviationPercent,
            EmployeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
            IsViewed = a.IsReviewed,
            Month = a.Month,
            ReviewComment = a.ReviewComment
        }).ToList();
        return new Response<List<GetSalaryAnomalyDto>>(
            HttpStatusCode.OK,
            "Records retrieved successfully",
            mapped
            );
    }
 //for graphs 
    public async Task<PaginatedResponse<SalaryAnomalyListDto>> GetSalaryAnomaliesForListAsync()
    {
        var anomalies = await _repository.GetAllAsync();
        if (!anomalies.Any())
        {
            return new PaginatedResponse<SalaryAnomalyListDto>(
                new List<SalaryAnomalyListDto>(),
                1,
                1,
                0
            )
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = "No salary anomalies are found"
            };
        }
        
        var mapped = anomalies.Select(h => new SalaryAnomalyListDto
        {
          FullName =$"{h.Employee.FirstName} {h.Employee.LastName}",
          Month = h.Month,
          Deviation = h.DeviationPercent,
          IsViewed = h.IsReviewed
        }).ToList();
        
        const int pageSize = 5; 
        const int pageNumber = 1;
        var totalRecords = mapped.Count;
        
        var pagedData = mapped
            .Take(pageSize)
            .ToList();
        
        return new PaginatedResponse<SalaryAnomalyListDto>(
            pagedData,
            pageNumber,
            pageSize,
            totalRecords
        )
        {
            StatusCode = (int)HttpStatusCode.OK,
            Message = "Salary anomalies retrieved successfully."
        };
    }
}
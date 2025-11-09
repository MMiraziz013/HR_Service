using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.PayrollRecord;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.SalaryAnomaly;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Services.SalaryAnomaly;

public class SalaryAnomalyService : ISalaryAnomalyService
{
    private readonly ISalaryAnomalyRepository _repository;
    private readonly ISalaryHistoryRepository _salaryRepository;
    private readonly IPayrollRecordRepository _payrollRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<SalaryAnomalyService> _logger;
    private readonly ICacheService _cacheService;

    public SalaryAnomalyService(ISalaryAnomalyRepository repository, 
        ISalaryHistoryRepository salaryRepository, 
        IPayrollRecordRepository payrollRepository,
        IEmployeeRepository employeeRepository,
        ILogger<SalaryAnomalyService> logger,
        ICacheService cacheService)
    {
        _repository = repository;
        _salaryRepository = salaryRepository;
        _payrollRepository = payrollRepository;
        _employeeRepository = employeeRepository;
        _logger = logger;
        _cacheService = cacheService;
    }
    
    public async Task<Response<int>> GenerateAnomaliesAsync()
    {
        try
        {
            const float deviationThreshold = 10f;
            
            var payrolls = await _payrollRepository.GetLatestPayrollAsync();

            if (!payrolls.Any())
            {
                return new Response<int>(HttpStatusCode.OK, "No payroll data found.", 0);
            }
            
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

                    // Note: Use 'double' for Math.Abs to match the explicit float conversion
                    var deviationPercent = (float)(((actualNet - avgGross) / avgGross) * 100m);

                    if (Math.Abs((double)deviationPercent) > deviationThreshold)
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
            
            if (anomaliesCreated > 0)
            {
                await InvalidateAnomalyListCaches();
            }

            return new Response<int>(
                HttpStatusCode.OK,
                $"{anomaliesCreated} salary anomalies created successfully.",
                anomaliesCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while generating salary anomalies.");
            return new Response<int>(HttpStatusCode.InternalServerError, "An unexpected error occurred while generating anomalies.", 0);
        }
    }

    public async Task<PaginatedResponse<GetSalaryAnomalyDto>> GetAllAsync()
    {
        const string cacheKey = "salary_anomalies_all";
        
        try
        {
            var cached = await _cacheService.GetAsync<PaginatedResponse<GetSalaryAnomalyDto>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
            var anomalies = await _repository.GetAllAsync();
            if (!anomalies.Any())
            {
                return new PaginatedResponse<GetSalaryAnomalyDto>(new List<GetSalaryAnomalyDto>(), 1, 1, 0)
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

            var response = new PaginatedResponse<GetSalaryAnomalyDto>(
                mapped,
                pageNumber: 1,
                pageSize: mapped.Count,
                totalRecords: mapped.Count)
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Salary anomalies retrieved successfully."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving all salary anomalies.");
            return new PaginatedResponse<GetSalaryAnomalyDto>(new List<GetSalaryAnomalyDto>(), 1, 1, 0)
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred while retrieving salary anomalies."
            };
        }
    }
    public async Task<Response<List<GetSalaryAnomalyDto>>> GetUnviewedAsync()
    {
        const string cacheKey = "salary_anomalies_unviewed";
        
        try
        {
            var cached = await _cacheService.GetAsync<Response<List<GetSalaryAnomalyDto>>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
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

            var response = new Response<List<GetSalaryAnomalyDto>>(
                HttpStatusCode.OK,
                message: "Salary anomalies retrieved successfully.",
                mapped);

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving unviewed salary anomalies.");
            return new Response<List<GetSalaryAnomalyDto>>(HttpStatusCode.InternalServerError, 
                "An unexpected error occurred while retrieving unviewed salary anomalies.");
        }
    }

    public async Task<Response<GetSalaryAnomalyDto>> MarkAsViewedAsync(int id)
    {
        try
        {
            var anomaly = await _repository.GetByIdAsync(id);
            if (anomaly is null)
            {
                return new Response<GetSalaryAnomalyDto>(HttpStatusCode.NotFound, $"Anomaly with ID {id} is not found.");
            }

            if (anomaly.IsReviewed)
            {
                return new Response<GetSalaryAnomalyDto>(HttpStatusCode.BadRequest, "Salary anomaly is already reviewed");
            }

            anomaly.IsReviewed = true;
            var updated = await _repository.UpdateAsync(anomaly);

            if (!updated)
            {
                return new Response<GetSalaryAnomalyDto>(HttpStatusCode.InternalServerError, "Failed to mark anomaly as viewed, try again!");
            }
            
            await InvalidateAnomalyListCaches();
            await _cacheService.RemoveAsync($"salary_anomaly_{id}");

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while marking salary anomaly ID {id} as viewed.", id);
            return new Response<GetSalaryAnomalyDto>(HttpStatusCode.InternalServerError, "An unexpected error occurred while marking anomaly as viewed.");
        }
    }

    public async Task<Response<GetSalaryAnomalyDto>> AddReviewCommentAsync(int id, string reviewComment)
    {
        try
        {
            var anomaly = await _repository.GetByIdAsync(id);
            if (anomaly is null)
            {
                return new Response<GetSalaryAnomalyDto>(HttpStatusCode.NotFound, "Anomaly is not found.");
            }
            
            if (anomaly.IsReviewed == false)
            {
                return new Response<GetSalaryAnomalyDto>(HttpStatusCode.BadRequest, "Unreviewed anomaly cannot be commented!");
            }
            
            anomaly.ReviewComment = reviewComment;
            var updated = await _repository.UpdateAsync(anomaly);
            
            if (!updated)
            {
                return new Response<GetSalaryAnomalyDto>(HttpStatusCode.InternalServerError, "Failed to add comment, try again!");
            }

            await InvalidateAnomalyListCaches();
            await _cacheService.RemoveAsync($"salary_anomaly_{id}");

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
                HttpStatusCode.Accepted,
                "Comment is added successfully",
                dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding review comment to salary anomaly ID {id}.", id);
            return new Response<GetSalaryAnomalyDto>(HttpStatusCode.InternalServerError, "An unexpected error occurred while adding review comment.");
        }
    }

    public async Task<Response<bool>> DeleteAsync(int id)
    {
        try
        {
            var isDeleted = await _repository.DeleteAnomalyAsync(id);
            if (isDeleted == false)
            {
                return new Response<bool>(HttpStatusCode.NotFound, "Failed to delete salary anomaly.");
            }

            await InvalidateAnomalyListCaches();
            await _cacheService.RemoveAsync($"salary_anomaly_{id}");

            return new Response<bool>(HttpStatusCode.OK, "Salary anomaly is deleted!", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting salary anomaly ID {id}.", id);
            return new Response<bool>(HttpStatusCode.InternalServerError, "An unexpected error occurred while deleting salary anomaly.");
        }
    }
    
    public async Task<Response<List<GetSalaryAnomalyDto>>> GetAnomalyByEmployeeId(int id)
    {
        var cacheKey = $"salary_anomalies_employee_{id}";
        
        try
        {
            var cached = await _cacheService.GetAsync<Response<List<GetSalaryAnomalyDto>>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee is null)
            {
                return new Response<List<GetSalaryAnomalyDto>>(HttpStatusCode.NotFound, $"Employee with ID:{id} is not found.");
            }

            var anomalies = await _repository.GetByEmployeeIdAsync(id);
            if (!anomalies.Any())
            {
                return new Response<List<GetSalaryAnomalyDto>>(HttpStatusCode.NotFound, $"No anomalies were found for employee {id}.");
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

            var response = new Response<List<GetSalaryAnomalyDto>>(
                HttpStatusCode.OK,
                "Records retrieved successfully",
                mapped);

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving salary anomalies for employee ID {id}.", id);
            return new Response<List<GetSalaryAnomalyDto>>(HttpStatusCode.InternalServerError, "An unexpected error occurred while retrieving employee anomalies.");
        }
    }
    
     /// <summary>
     /// Retrieves the data for the salary anomalies widget on the dashboard.
     /// </summary> 
    public async Task<PaginatedResponse<SalaryAnomalyListDto>> GetSalaryAnomaliesForListAsync()
    {
        const string cacheKey = "salary_anomalies_list_for_graphs";
        
        try
        {
            var cached = await _cacheService.GetAsync<PaginatedResponse<SalaryAnomalyListDto>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
            
            var anomalies = await _repository.GetAllAsync();
            if (!anomalies.Any())
            {
                return new PaginatedResponse<SalaryAnomalyListDto>(new List<SalaryAnomalyListDto>(), 1, 1, 0)
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
            
            var response = new PaginatedResponse<SalaryAnomalyListDto>(
                pagedData,
                pageNumber,
                pageSize,
                totalRecords
            )
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Salary anomalies retrieved successfully."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving salary anomalies for list view.");
            return new PaginatedResponse<SalaryAnomalyListDto>(new List<SalaryAnomalyListDto>(), 1, 1, 0)
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred while retrieving salary anomalies."
            };
        }
    }
    
    private async Task InvalidateAnomalyListCaches()
    {
        await _cacheService.RemoveAsync("salary_anomalies_all");
        await _cacheService.RemoveAsync("salary_anomalies_unviewed");
        await _cacheService.RemoveAsync("salary_anomalies_list_for_graphs");
    }
}
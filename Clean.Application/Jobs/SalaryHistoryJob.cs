using Clean.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Clean.Application.Jobs;


[DisallowConcurrentExecution]
public class SalaryHistoryJob : IJob
{
    private readonly ISalaryHistoryService _salaryService;
    private readonly ILogger<SalaryHistoryJob> _logger;

    public SalaryHistoryJob(ISalaryHistoryService salaryService, ILogger<SalaryHistoryJob> logger)
    {
        _salaryService = salaryService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("SalaryHistoryJob started at: {time}", DateTime.UtcNow);

        try
        {
            await _salaryService.GenerateMonthlySalaryHistoryAsync();
            _logger.LogInformation("SalaryHistoryJob completed successfully at: {time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SalaryHistoryJob failed at: {time}", DateTime.UtcNow);
        }
    }
}

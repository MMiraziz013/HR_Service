using Clean.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Clean.Application.Jobs;

public class SalaryAnomalyJob : IJob
{
    private readonly ISalaryAnomalyService _salaryAnomalyService;
    private ILogger<SalaryAnomalyJob> _logger;
    public SalaryAnomalyJob(ISalaryAnomalyService salaryAnomalyService,ILogger<SalaryAnomalyJob> logger)
    {
        _salaryAnomalyService = salaryAnomalyService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("SalaryAnomalyJob started at: {time}", DateTime.UtcNow);

        try
        {
            await _salaryAnomalyService.GenerateAnomaliesAsync();
            _logger.LogInformation("SalaryAnomalyJob completed successfully at: {time}", DateTime.UtcNow);

        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "SalaryAnomalyJob failed at: {time}", DateTime.UtcNow);

        }
    }
}

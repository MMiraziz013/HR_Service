using Clean.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Clean.Application.Jobs;

[DisallowConcurrentExecution]
public class VacationBalanceJob : IJob
{
    private readonly IVacationBalanceService _vacationBalanceService;
    private readonly ILogger<VacationBalanceJob> _logger;

    public VacationBalanceJob(IVacationBalanceService vacationBalanceService, ILogger<VacationBalanceJob> logger)
    {
        _vacationBalanceService = vacationBalanceService;
        _logger = logger;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("VacationBalanceJob started at: {time}", DateTime.UtcNow);

        try
        {
            await _vacationBalanceService.AutoUpdateVacationBalancesAsync();
            await _vacationBalanceService.AutoUpdateVacationStatusesAsync();

            _logger.LogInformation("VacationBalanceJob completed successfully at: {time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VacationBalanceJob failed at: {time}", DateTime.UtcNow);
        }
    }
}
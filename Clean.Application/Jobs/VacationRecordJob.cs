using Clean.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Clean.Application.Jobs;

[DisallowConcurrentExecution]
public class VacationRecordJob : IJob
{
    private readonly IVacationRecordService _vacationRecordService;
    private readonly ILogger<VacationRecordJob> _logger;

    public VacationRecordJob(IVacationRecordService vacationRecordService, ILogger<VacationRecordJob> logger)
    {
        _vacationRecordService = vacationRecordService;
        _logger = logger;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("VacationRecordJob started at: {time}", DateTime.UtcNow);

        try
        {
            await _vacationRecordService.AutoUpdateVacationStatusesAsync();

            _logger.LogInformation("VacationRecordJob completed successfully at: {time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VacationRecordJob failed at: {time}", DateTime.UtcNow);
        }
    }
}
using Clean.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Clean.Application.Jobs;

public class PayrollRecordJob : IJob
{

    private readonly IPayrollRecordService _payrollRecordService;
    private readonly ILogger<PayrollRecordJob> _logger;

    public PayrollRecordJob(
        IPayrollRecordService payrollRecordService,
        ILogger<PayrollRecordJob> logger)
    {
        _payrollRecordService = payrollRecordService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Running GeneratePayrollJob at {Time}", DateTime.UtcNow);
            
            await _payrollRecordService.GenerateMonthlyPayrollRecordsAsync();

            _logger.LogInformation("GeneratePayrollJob completed successfully at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while running GeneratePayrollJob at {Time}", DateTime.UtcNow);
        }
    }
}
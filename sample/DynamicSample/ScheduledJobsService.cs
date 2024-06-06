using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NCronJob;

namespace DynamicSample;

public class ScheduledJobsService : BackgroundService
{
    private ScheduledJobsConfig config = new();
    private readonly ILogger<ScheduledJobsService> logger;
    private readonly IOptionsMonitor<ScheduledJobsConfig> configMonitor;
    private readonly IRuntimeJobRegistry runtimeJobRegistry;
    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP002:Dispose member", Justification = "Is handled in Dispose")]
    private IDisposable? configMonitorChangeListener = null;

    public ScheduledJobsService(ILogger<ScheduledJobsService> logger, IOptionsMonitor<ScheduledJobsConfig> configMonitor, IRuntimeJobRegistry runtimeJobRegistry)
    {
        this.logger = logger;
        this.configMonitor = configMonitor ?? throw new ArgumentNullException(nameof(configMonitor));
        this.runtimeJobRegistry = runtimeJobRegistry;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        config = configMonitor.CurrentValue;

        await base.StartAsync(cancellationToken);

        // Re-execute the service when config changes.
        configMonitorChangeListener?.Dispose();
        configMonitorChangeListener = configMonitor.OnChange(async x =>
        {
            logger.LogInformation("ScheduledJobsConfig changed");
            config = x;
            await ExecuteAsync(cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Configure scheduled Jobs based on provided Config.
    /// </summary>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Configure RuntimeJobs based on config
        foreach (var registeredJob in runtimeJobRegistry.GetJobs())
        {
            var jobConfig = config.GetConfigForJob(registeredJob.JobName);

            logger.LogInformation("Updating schedule of Job '{JobName}' to {Cron}", registeredJob.JobName, jobConfig.Cron);
            runtimeJobRegistry.UpdateSchedule(registeredJob.JobName, jobConfig.Cron, jobConfig.TimeZone);

            // NOTE: The below does not work, because the registeredJob.CronExpression is a parsed Cron from the library Cronos, and is therefore not comparable to the input Cron.
            /*
            // Update runtime if config has changed.
            if (registeredJob.CronExpression != jobConfig.Cron || registeredJob.TimeZone != jobConfig.TimeZone)
            {
                logger.LogInformation("Job '{JobName}' changed Scheduling config. Old/New Cron: {OldCron}/{NewCon}",
                    registeredJob.JobName, registeredJob.CronExpression, jobConfig.Cron);

                runtimeJobRegistry.UpdateSchedule(registeredJob.JobName, jobConfig.Cron, jobConfig.TimeZone);
            }
            */
        }

        return Task.CompletedTask;
    }

    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "")]
    public override void Dispose()
    {
        configMonitorChangeListener?.Dispose();
        base.Dispose();
    }
}

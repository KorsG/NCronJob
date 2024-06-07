using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCronJob;

namespace DynamicSample;

internal static class ScheduledJobsConfigurationExtensions
{
    private const string CronNever = "0 0 31 2 *"; // 31st of february which is a valid cron but a date which is never reached.
    private static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");

    public static NCronJobOptionBuilder AddConfigEnabledScheduledJobs(
        this NCronJobOptionBuilder builder,
        IServiceCollection services,
        ScheduledJobsConfig scheduleConfig)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(scheduleConfig);

        // Add Jobs.
        builder.AddJob<PrintHelloWorldJob>(scheduleConfig, "PrintHelloWorld_1", p =>
            p.WithParameter(1));

        builder.AddJob<PrintHelloWorldJob>(scheduleConfig, "PrintHelloWorld_2", p =>
            p.WithParameter(2));

        builder.AddJob<PrintHelloWorldJob>(scheduleConfig, "PrintHelloWorld_3", p =>
            p.WithParameter(3))
            // Register a handler that gets executed when the job is done
            .AddNotificationHandler<HelloWorldJobHandler>();

        builder.AddJob<PrintHelloWorldJob>(scheduleConfig, "PrintHelloWorld_OnStartup", p =>
            // NOTE: Parameters not supported for startup jobs, so this isn't used.
            p.WithParameter("On startup"));

        // Adds config change monitoring service.
        services.AddHostedService<ScheduledJobsService>();

        return builder;
    }

    private static INotificationStage<T> AddJob<T>(this NCronJobOptionBuilder builder, ScheduledJobsConfig scheduleConfig, string jobName, Action<ParameterBuilder>? parameters = null) where T : class, IJob
    {
        var jobConfig = scheduleConfig.GetConfigForJob(jobName);

        IStartupStage<T> startupStageBuilder;

        var onlyOnStartup = jobConfig.IsStartupJob;
        if (onlyOnStartup)
        {
            // NOTE: Startup jobs does not allow non-nullable cron expressions, but adding null expression via WithCronExpression throws an exception, so cant configure e.g. parameters.
            startupStageBuilder = builder.AddJob<T>();
            return startupStageBuilder.RunAtStartup();
        }
        else
        {
            startupStageBuilder = builder.AddJob<T>(x =>
            {
                var paramBuilder = x.WithCronExpression(jobConfig.Cron, timeZoneInfo: jobConfig.TimeZone).WithName(jobName);
                parameters?.Invoke(paramBuilder);
            });

            return startupStageBuilder;
        }
    }

    internal static (bool Enabled, string Cron, TimeZoneInfo TimeZone, bool IsStartupJob) GetConfigForJob(this ScheduledJobsConfig config, string jobName)
    {
        var jobConfig = config?.Jobs?.Find(x => x.Name == jobName);

        if (jobConfig != null)
        {
            var cronExpression = jobConfig.Enabled && !string.IsNullOrWhiteSpace(jobConfig.Cron)
                ? jobConfig.Cron.Trim()
                : CronNever;

            var timeZone = DefaultTimeZone;
            if (!string.IsNullOrWhiteSpace(jobConfig.TimeZone))
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(jobConfig.TimeZone.Trim());
            }

            return (jobConfig.Enabled, cronExpression, timeZone, jobConfig.OnlyOnStartup);
        }

        return (false, CronNever, DefaultTimeZone, false);
    }
}

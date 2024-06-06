using NCronJob;
using DynamicSample;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

var scheduledJobsConfigSection = builder.Configuration.GetSection("ScheduledJobs");
builder.Services.Configure<ScheduledJobsConfig>(scheduledJobsConfigSection);

// Add NCronJob to the container.
var scheduledJobsConfig = scheduledJobsConfigSection.Get<ScheduledJobsConfig>() ?? new ScheduledJobsConfig();
builder.Services.AddNCronJob(n =>
    n.AddConfigEnabledScheduledJobs(builder.Services, scheduledJobsConfig)
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/trigger-instant", (IInstantJobRegistry instantJobRegistry) =>
{
    instantJobRegistry.RunInstantJob<PrintHelloWorldJob>("Hello from instant job!");
})
    .WithName("TriggerInstantJob")
    .WithOpenApi();

app.MapPost("/trigger-instant-concurrent", (IInstantJobRegistry instantJobRegistry) =>
{
    instantJobRegistry.RunInstantJob<ConcurrentTaskExecutorJob>();
})
    .WithSummary("Triggers a job that can run concurrently with other instances.")
    .WithDescription(
        """
        This endpoint triggers an instance of 'TestCancellationJob' that is designed
        to run concurrently with other instances of the same job. Each instance operates
        independently, allowing parallel processing without mutual interference.
        """)
    .WithName("TriggerConcurrentJob")
    .WithOpenApi();

await app.RunAsync();

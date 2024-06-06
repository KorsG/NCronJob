using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DynamicSample;

public class ScheduledJobsConfig
{
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "<Pending>")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
    public List<ScheduledJobConfig> Jobs { get; set; } = [];
}

public class ScheduledJobConfig
{
    public string Name { get; set; } = "";

    public bool Enabled { get; set; }

    public string Cron { get; set; } = "";

    public string TimeZone { get; set; } = "";

    public bool OnlyOnStartup { get; set; }

}

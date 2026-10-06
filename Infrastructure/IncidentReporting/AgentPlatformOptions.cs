namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public sealed class AgentPlatformOptions
{
    public const string SectionName = "AgentPlatform";

    public bool Enabled { get; set; }
    public bool AutoReportIncidents { get; set; }
    public string ControlPlaneUrl { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = "ROOM";
    public string Environment { get; set; } = "Production";
    public int RequestTimeoutSeconds { get; set; } = 10;
    public int QueueCapacity { get; set; } = 100;
    public int ThrottleWindowSeconds { get; set; } = 10;
    public string? DeploymentVersion { get; set; }
    public string? GitCommit { get; set; }

    public bool ReportingEnabled => Enabled && AutoReportIncidents && !string.IsNullOrWhiteSpace(ControlPlaneUrl);
}

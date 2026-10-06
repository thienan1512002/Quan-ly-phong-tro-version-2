namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public sealed record IncidentPayload
{
    public string ProjectCode { get; init; } = string.Empty;
    public string Source { get; init; } = "Application";
    public string Environment { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ErrorType { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string? StackTrace { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string? ExpectedBehavior { get; init; }
    public string? ActualBehavior { get; init; }
    public string? DeploymentVersion { get; init; }
    public string? GitCommit { get; init; }
}

namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public sealed class DefaultIncidentReportFilter : IIncidentReportFilter
{
    // TaskCanceledException also derives from OperationCanceledException.
    // Current business validation uses ModelState/results, not exception types.
    public bool ShouldReport(Exception exception, HttpContext context) =>
        exception is not OperationCanceledException;
}

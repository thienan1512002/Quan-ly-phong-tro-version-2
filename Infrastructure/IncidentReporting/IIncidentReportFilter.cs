namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public interface IIncidentReportFilter
{
    bool ShouldReport(Exception exception, HttpContext context);
}

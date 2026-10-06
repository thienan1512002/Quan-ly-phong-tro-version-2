namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public interface IIncidentReporter
{
    Task<bool> ReportAsync(IncidentPayload incident, CancellationToken cancellationToken);
}

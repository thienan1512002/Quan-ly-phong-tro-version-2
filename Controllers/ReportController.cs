using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using ClosedXML.Excel;

namespace QuanLyPhongTro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        // GET /Report
        public IActionResult Index()
        {
            return View();
        }

        // ─────────────────────────────────────────────────────────────────
        // BÁO CÁO THUÊ NHÀ
        // ─────────────────────────────────────────────────────────────────

        // GET /Report/RentalReport
        public async Task<IActionResult> RentalReport(int? year, int? month, bool? active)
        {
            int selYear = year ?? DateTime.Today.Year;
            int selMonth = month ?? 0; // 0 = tất cả tháng

            var query = _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.User)
                .AsQueryable();

            // Lọc theo trạng thái
            if (active == true)
                query = query.Where(c => c.IsActive);
            else if (active == false)
                query = query.Where(c => !c.IsActive);

            // Lọc theo năm (dựa vào StartDate)
            query = query.Where(c => c.StartDate.Year == selYear
                                  || c.EndDate.Year == selYear
                                  || (c.StartDate.Year < selYear && c.EndDate.Year > selYear));

            // Lọc theo tháng nếu chọn
            if (selMonth > 0)
                query = query.Where(c => c.StartDate.Month == selMonth
                                      || (c.StartDate.Year == selYear && c.StartDate.Month <= selMonth
                                          && (c.EndDate.Year > selYear || c.EndDate.Month >= selMonth)));

            var contracts = await query.OrderBy(c => c.StartDate).ToListAsync();

            ViewBag.Year = selYear;
            ViewBag.Month = selMonth;
            ViewBag.Active = active;
            ViewBag.Years = Enumerable.Range(DateTime.Today.Year - 4, 6).Reverse().ToList();

            // Tổng hợp
            ViewBag.TotalContracts = contracts.Count;
            ViewBag.ActiveCount = contracts.Count(c => c.IsActive);
            ViewBag.TotalDeposit = contracts.Sum(c => c.Deposit ?? 0);
            ViewBag.TotalRent = contracts.Sum(c => c.MonthlyRent);

            return View(contracts);
        }

        // GET /Report/ExportRental
        public async Task<IActionResult> ExportRental(int? year, int? month, bool? active)
        {
            int selYear = year ?? DateTime.Today.Year;
            int selMonth = month ?? 0;

            var query = _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.User)
                .AsQueryable();

            if (active == true) query = query.Where(c => c.IsActive);
            if (active == false) query = query.Where(c => !c.IsActive);

            query = query.Where(c => c.StartDate.Year == selYear
                                  || c.EndDate.Year == selYear
                                  || (c.StartDate.Year < selYear && c.EndDate.Year > selYear));

            if (selMonth > 0)
                query = query.Where(c => c.StartDate.Month == selMonth
                                      || (c.StartDate.Year == selYear && c.StartDate.Month <= selMonth
                                          && (c.EndDate.Year > selYear || c.EndDate.Month >= selMonth)));

            var contracts = await query.OrderBy(c => c.StartDate).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Báo cáo thuê nhà");

            // Tiêu đề
            string title = selMonth > 0
                ? $"BÁO CÁO THUÊ NHÀ NĂM {selYear} - THÁNG {selMonth}"
                : $"BÁO CÁO THUÊ NHÀ NĂM {selYear}";
            ws.Cell(1, 1).Value = title;
            ws.Range(1, 1, 1, 10).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(27, 79, 114);
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(1).Height = 28;

            ws.Cell(2, 1).Value = $"Xuất ngày: {DateTime.Today:dd/MM/yyyy}";
            ws.Range(2, 1, 2, 10).Merge();
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 1).Style.Font.Italic = true;

            // Headers (row 4)
            string[] headers = { "STT", "Tên phòng", "Địa chỉ", "Người thuê", "SĐT",
                                  "Ngày bắt đầu", "Ngày kết thúc", "Giá thuê (VNĐ)", "Tiền cọc (VNĐ)", "Trạng thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                var hCell = ws.Cell(4, i + 1);
                hCell.Value = headers[i];
                hCell.Style.Font.Bold = true;
                hCell.Style.Font.FontColor = XLColor.White;
                hCell.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 134, 193);
                hCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                hCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws.Row(4).Height = 22;

            // Data rows
            int row = 5, stt = 1;
            foreach (var c in contracts)
            {
                var bg = stt % 2 == 0
                    ? XLColor.FromArgb(235, 245, 251)
                    : XLColor.White;

                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = c.Room?.Name ?? "";
                ws.Cell(row, 3).Value = c.Room?.Address ?? "";
                ws.Cell(row, 4).Value = c.User?.FullName ?? "";
                ws.Cell(row, 5).Value = c.User?.PhoneNumber ?? "";
                ws.Cell(row, 6).Value = c.StartDate;
                ws.Cell(row, 7).Value = c.EndDate;
                ws.Cell(row, 8).Value = c.MonthlyRent;
                ws.Cell(row, 9).Value = c.Deposit ?? 0;
                ws.Cell(row, 10).Value = c.IsActive ? "Hiệu lực" : "Đã kết thúc";

                ws.Cell(row, 6).Style.NumberFormat.Format = "dd/mm/yyyy";
                ws.Cell(row, 7).Style.NumberFormat.Format = "dd/mm/yyyy";
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0";

                for (int col = 1; col <= 10; col++)
                {
                    ws.Cell(row, col).Style.Fill.BackgroundColor = bg;
                    ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Cell(row, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row++;
            }

            // Tổng cộng
            if (contracts.Any())
            {
                ws.Cell(row, 2).Value = "TỔNG CỘNG";
                ws.Cell(row, 8).Value = contracts.Sum(c => c.MonthlyRent);
                ws.Cell(row, 9).Value = contracts.Sum(c => c.Deposit ?? 0);
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
                for (int col = 1; col <= 10; col++)
                {
                    ws.Cell(row, col).Style.Font.Bold = true;
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromArgb(213, 216, 220);
                    ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            ws.SheetView.FreezeRows(4);
            ws.Columns().AdjustToContents();

            using var ms = new System.IO.MemoryStream();
            wb.SaveAs(ms);
            string fileName = $"BaoCaoThueNha_{selYear}{(selMonth > 0 ? "_T" + selMonth : "")}.xlsx";
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ─────────────────────────────────────────────────────────────────
        // BÁO CÁO DOANH THU
        // ─────────────────────────────────────────────────────────────────

        // GET /Report/RevenueReport
        public async Task<IActionResult> RevenueReport(int? year)
        {
            int selYear = year ?? DateTime.Today.Year;
            ViewBag.Years = Enumerable.Range(DateTime.Today.Year - 4, 6).Reverse().ToList();
            ViewBag.Year = selYear;

            // Lấy tất cả hợp đồng active (hoặc đã kết thúc trong năm)
            var contracts = await _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.User)
                .Where(c => c.StartDate.Year <= selYear && c.EndDate.Year >= selYear)
                .ToListAsync();

            // Doanh thu từng tháng = tổng MonthlyRent của các HĐ hoạt động trong tháng đó
            var monthly = new decimal[13]; // index 1-12
            for (int m = 1; m <= 12; m++)
            {
                var firstDay = new DateTime(selYear, m, 1);
                var lastDay = firstDay.AddMonths(1).AddDays(-1);
                monthly[m] = contracts
                    .Where(c => c.StartDate <= lastDay && c.EndDate >= firstDay)
                    .Sum(c => c.MonthlyRent);
            }

            ViewBag.Monthly = monthly;
            ViewBag.TotalRevenue = monthly.Sum();
            ViewBag.AvgMonthly = monthly.Where(x => x > 0).DefaultIfEmpty(0).Average();
            ViewBag.BestMonth = Array.IndexOf(monthly, monthly.Skip(1).Max());
            ViewBag.ActiveContracts = contracts.Count(c => c.IsActive);
            ViewBag.TotalContracts = contracts.Count;

            // Top phòng doanh thu cao nhất
            var topRooms = contracts
                .GroupBy(c => new { c.RoomId, Name = c.Room?.Name ?? "?" })
                .Select(g => new
                {
                    g.Key.Name,
                    Revenue = g.Sum(c =>
                    {
                        // Đếm tháng trong selYear mà HĐ này active
                        int months = 0;
                        for (int m = 1; m <= 12; m++)
                        {
                            var fd = new DateTime(selYear, m, 1);
                            var ld = fd.AddMonths(1).AddDays(-1);
                            if (c.StartDate <= ld && c.EndDate >= fd) months++;
                        }
                        return c.MonthlyRent * months;
                    }),
                    Months = g.Sum(c =>
                    {
                        int months = 0;
                        for (int m = 1; m <= 12; m++)
                        {
                            var fd = new DateTime(selYear, m, 1);
                            var ld = fd.AddMonths(1).AddDays(-1);
                            if (c.StartDate <= ld && c.EndDate >= fd) months++;
                        }
                        return months;
                    })
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            ViewBag.TopRooms = topRooms;

            return View(contracts);
        }

        // GET /Report/ExportRevenue
        public async Task<IActionResult> ExportRevenue(int? year)
        {
            int selYear = year ?? DateTime.Today.Year;

            var contracts = await _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.User)
                .Where(c => c.StartDate.Year <= selYear && c.EndDate.Year >= selYear)
                .OrderBy(c => c.StartDate)
                .ToListAsync();

            using var wb = new XLWorkbook();

            // ── Sheet 1: Doanh thu theo tháng ──
            var ws1 = wb.Worksheets.Add("Doanh thu theo tháng");

            ws1.Cell(1, 1).Value = $"BÁO CÁO DOANH THU NĂM {selYear}";
            ws1.Range(1, 1, 1, 4).Merge();
            ws1.Cell(1, 1).Style.Font.Bold = true;
            ws1.Cell(1, 1).Style.Font.FontSize = 14;
            ws1.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws1.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(27, 79, 114);
            ws1.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws1.Row(1).Height = 28;

            ws1.Cell(2, 1).Value = $"Xuất ngày: {DateTime.Today:dd/MM/yyyy}";
            ws1.Range(2, 1, 2, 4).Merge();
            ws1.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws1.Cell(2, 1).Style.Font.Italic = true;

            string[] mHeaders = { "Tháng", "Số hợp đồng active", "Doanh thu (VNĐ)", "Ghi chú" };
            for (int i = 0; i < mHeaders.Length; i++)
            {
                var hCell = ws1.Cell(4, i + 1);
                hCell.Value = mHeaders[i];
                hCell.Style.Font.Bold = true;
                hCell.Style.Font.FontColor = XLColor.White;
                hCell.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 134, 193);
                hCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                hCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws1.Row(4).Height = 22;

            // Tính doanh thu từng tháng
            var monthRevs = new decimal[13];
            var monthCounts = new int[13];
            decimal totalRev = 0;
            for (int m = 1; m <= 12; m++)
            {
                var fd = new DateTime(selYear, m, 1);
                var ld = fd.AddMonths(1).AddDays(-1);
                var list = contracts.Where(c => c.StartDate <= ld && c.EndDate >= fd).ToList();
                monthRevs[m] = list.Sum(c => c.MonthlyRent);
                monthCounts[m] = list.Count;
                totalRev += monthRevs[m];
            }
            int bestMonth = 0; decimal maxRev = 0;
            for (int m = 1; m <= 12; m++)
                if (monthRevs[m] > maxRev) { maxRev = monthRevs[m]; bestMonth = m; }

            for (int m = 1; m <= 12; m++)
            {
                int mRow = m + 4;
                var bg = m == bestMonth && bestMonth > 0
                    ? XLColor.FromArgb(254, 249, 231)
                    : (m % 2 == 0 ? XLColor.FromArgb(235, 245, 251) : XLColor.White);

                ws1.Cell(mRow, 1).Value = $"Tháng {m}/{selYear}";
                ws1.Cell(mRow, 2).Value = monthCounts[m];
                ws1.Cell(mRow, 3).Value = monthRevs[m];
                ws1.Cell(mRow, 3).Style.NumberFormat.Format = "#,##0";
                ws1.Cell(mRow, 4).Value = m == bestMonth && bestMonth > 0 ? "★ Cao nhất" : "";
                for (int col = 1; col <= 4; col++)
                {
                    ws1.Cell(mRow, col).Style.Fill.BackgroundColor = bg;
                    ws1.Cell(mRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                ws1.Cell(mRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int totRow = 17;
            ws1.Cell(totRow, 1).Value = "TỔNG CỘNG";
            ws1.Cell(totRow, 3).Value = totalRev;
            ws1.Cell(totRow, 3).Style.NumberFormat.Format = "#,##0";
            for (int col = 1; col <= 4; col++)
            {
                ws1.Cell(totRow, col).Style.Font.Bold = true;
                ws1.Cell(totRow, col).Style.Fill.BackgroundColor = XLColor.FromArgb(213, 216, 220);
                ws1.Cell(totRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws1.SheetView.FreezeRows(4);
            ws1.Columns().AdjustToContents();

            // ── Sheet 2: Chi tiết hợp đồng ──
            var ws2 = wb.Worksheets.Add("Chi tiết hợp đồng");

            ws2.Cell(1, 1).Value = $"CHI TIẾT HỢP ĐỒNG NĂM {selYear}";
            ws2.Range(1, 1, 1, 9).Merge();
            ws2.Cell(1, 1).Style.Font.Bold = true;
            ws2.Cell(1, 1).Style.Font.FontSize = 14;
            ws2.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws2.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(27, 79, 114);
            ws2.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws2.Row(1).Height = 28;

            ws2.Cell(2, 1).Value = $"Xuất ngày: {DateTime.Today:dd/MM/yyyy}";
            ws2.Range(2, 1, 2, 9).Merge();
            ws2.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws2.Cell(2, 1).Style.Font.Italic = true;

            string[] dHeaders = { "STT", "Tên phòng", "Người thuê", "Ngày BĐ", "Ngày KT",
                                   "Giá thuê/tháng (VNĐ)", "Số tháng active", "Tổng doanh thu (VNĐ)", "Trạng thái" };
            for (int i = 0; i < dHeaders.Length; i++)
            {
                var hCell = ws2.Cell(4, i + 1);
                hCell.Value = dHeaders[i];
                hCell.Style.Font.Bold = true;
                hCell.Style.Font.FontColor = XLColor.White;
                hCell.Style.Fill.BackgroundColor = XLColor.FromArgb(39, 174, 96);
                hCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                hCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws2.Row(4).Height = 22;

            int dRow = 5; int dstt = 1; decimal grandTotal = 0;
            foreach (var c in contracts)
            {
                int months = 0;
                for (int m = 1; m <= 12; m++)
                {
                    var fd = new DateTime(selYear, m, 1);
                    var ld = fd.AddMonths(1).AddDays(-1);
                    if (c.StartDate <= ld && c.EndDate >= fd) months++;
                }
                decimal contrib = c.MonthlyRent * months;
                grandTotal += contrib;

                var bg = dstt % 2 == 0
                    ? XLColor.FromArgb(234, 250, 241)
                    : XLColor.White;

                ws2.Cell(dRow, 1).Value = dstt++;
                ws2.Cell(dRow, 2).Value = c.Room?.Name ?? "";
                ws2.Cell(dRow, 3).Value = c.User?.FullName ?? "";
                ws2.Cell(dRow, 4).Value = c.StartDate;
                ws2.Cell(dRow, 5).Value = c.EndDate;
                ws2.Cell(dRow, 6).Value = c.MonthlyRent;
                ws2.Cell(dRow, 7).Value = months;
                ws2.Cell(dRow, 8).Value = contrib;
                ws2.Cell(dRow, 9).Value = c.IsActive ? "Hiệu lực" : "Đã kết thúc";

                ws2.Cell(dRow, 4).Style.NumberFormat.Format = "dd/mm/yyyy";
                ws2.Cell(dRow, 5).Style.NumberFormat.Format = "dd/mm/yyyy";
                ws2.Cell(dRow, 6).Style.NumberFormat.Format = "#,##0";
                ws2.Cell(dRow, 8).Style.NumberFormat.Format = "#,##0";

                for (int col = 1; col <= 9; col++)
                {
                    ws2.Cell(dRow, col).Style.Fill.BackgroundColor = bg;
                    ws2.Cell(dRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws2.Cell(dRow, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                ws2.Cell(dRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws2.Cell(dRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                dRow++;
            }

            if (contracts.Any())
            {
                ws2.Cell(dRow, 2).Value = "TỔNG CỘNG";
                ws2.Cell(dRow, 8).Value = grandTotal;
                ws2.Cell(dRow, 8).Style.NumberFormat.Format = "#,##0";
                for (int col = 1; col <= 9; col++)
                {
                    ws2.Cell(dRow, col).Style.Font.Bold = true;
                    ws2.Cell(dRow, col).Style.Fill.BackgroundColor = XLColor.FromArgb(213, 216, 220);
                    ws2.Cell(dRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            ws2.SheetView.FreezeRows(4);
            ws2.Columns().AdjustToContents();

            using var ms = new System.IO.MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoDoanhThu_{selYear}.xlsx");
        }

        // ─────────────────────────────────────────────────────────────────
        private static string CsvEsc(string? val)
        {
            if (val == null) return "";
            if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            return val;
        }
    }
}

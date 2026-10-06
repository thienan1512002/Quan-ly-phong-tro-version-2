(() => {
    const payload = document.getElementById('dashboard-chart-data');
    if (!payload) return;
    const data = JSON.parse(payload.textContent);
    const colors = { green: '#498166', mint: '#b2d5bd', gold: '#ddbb74', rose: '#c8918c', gray: '#bac6be' };
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const font = { family: '"Segoe UI", system-ui, sans-serif', size: 10 };

    function draw(id, type, labels, values, palette, extra = {}) {
        const canvas = document.getElementById(id);
        if (!canvas) return;
        if (!window.Chart) {
            canvas.hidden = true;
            const notice = document.createElement('p');
            notice.className = 'chart-unavailable';
            notice.textContent = 'Không tải được biểu đồ. Số liệu vẫn hiển thị bên dưới.';
            canvas.parentElement.appendChild(notice);
            return;
        }
        new Chart(canvas, {
            type,
            data: { labels, datasets: [{
                data: values, backgroundColor: palette,
                borderWidth: type === 'doughnut' ? 3 : 0,
                borderColor: '#fff', borderRadius: type === 'doughnut' ? 3 : 5,
                maxBarThickness: 25, hoverOffset: type === 'doughnut' ? 3 : 0,
            }] },
            options: {
                responsive: true, maintainAspectRatio: false,
                animation: reducedMotion ? false : { duration: 450 },
                cutout: '76%',
                plugins: {
                    legend: { display: false },
                    tooltip: { backgroundColor: '#254b39', padding: 10, cornerRadius: 7, bodyFont: font, titleFont: font },
                },
                ...extra,
            },
        });
    }

    const numberTicks = { color: '#96a398', font, precision: 0 };
    const categoryTicks = { color: '#829184', font };
    const grid = { color: '#eef2eb', drawTicks: false };
    const border = { display: false };
    draw('roomStatusChart', 'doughnut', ['Đã cho thuê', 'Còn trống', 'Chờ duyệt'],
        data.rooms, [colors.green, colors.mint, colors.gold]);
    draw('requestStatusChart', 'bar', ['Chờ duyệt', 'Đã duyệt', 'Từ chối', 'Đã hủy'],
        data.requests, [colors.gold, colors.green, colors.rose, colors.gray], {
            indexAxis: 'y',
            scales: { x: { beginAtZero: true, ticks: numberTicks, grid, border }, y: { ticks: categoryTicks, grid: { display: false }, border } },
        });
    draw('expiringChart', 'bar', ['≤ 30 ngày', '31–60 ngày', '61–90 ngày'],
        data.expiring, [colors.rose, colors.gold, colors.green], {
            scales: { y: { beginAtZero: true, ticks: numberTicks, grid, border }, x: { ticks: categoryTicks, grid: { display: false }, border } },
        });
    draw('contractProgressChart', 'doughnut', ['Đã qua', 'Còn lại'],
        data.contractDays, [colors.gray, colors.green], { cutout: '72%' });
    draw('myRequestsChart', 'doughnut', ['Chờ duyệt', 'Đã duyệt', 'Từ chối', 'Đã hủy'],
        data.myRequests, [colors.gold, colors.green, colors.rose, colors.gray], { cutout: '72%' });
})();

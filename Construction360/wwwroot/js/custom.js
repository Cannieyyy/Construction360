// Toggle password visibility
document.querySelectorAll('.eye-toggle').forEach(btn => {
    btn.addEventListener('click', function () {
        const input = this.previousElementSibling || this.parentElement.querySelector('input');
        if (input && input.type) {
            input.type = input.type === 'password' ? 'text' : 'password';
            this.textContent = input.type === 'password' ? '👁' : '🙈';
        }
    });
});

// Role selector
document.querySelectorAll('.role-option').forEach(opt => {
    opt.addEventListener('click', function () {
        document.querySelectorAll('.role-option').forEach(o => {
            o.classList.remove('selected');
            o.querySelector('.radio-dot')?.classList.remove('filled');
        });
        this.classList.add('selected');
        this.querySelector('.radio-dot')?.classList.add('filled');
        const roleInput = document.getElementById('roleInput');
        if (roleInput) roleInput.value = this.dataset.role;
    });
});

// Sidebar collapse
const collapseBtn = document.querySelector('.collapse-btn');
const sidebar = document.querySelector('.sidebar');
if (collapseBtn && sidebar) {
    collapseBtn.addEventListener('click', () => sidebar.classList.toggle('collapsed'));
}

// Modal open/close
window.openModal = id => document.getElementById(id)?.classList.add('open');
window.closeModal = id => document.getElementById(id)?.classList.remove('open');
document.querySelectorAll('.modal-overlay').forEach(m => {
    m.addEventListener('click', function (e) {
        if (e.target === this) this.classList.remove('open');
    });
});

// Draw chart bars (pure CSS/SVG substitute with canvas)
function drawBarChart(canvasId, data, colors) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const W = canvas.width, H = canvas.height;
    const pad = { top: 20, right: 20, bottom: 40, left: 36 };
    const chartW = W - pad.left - pad.right;
    const chartH = H - pad.top - pad.bottom;
    const groups = data.length;
    const barW = (chartW / groups) * 0.6;
    const subW = barW / colors.length;
    const maxVal = Math.max(...data.flatMap(d => d.values));

    ctx.clearRect(0, 0, W, H);
    ctx.font = '11px Sora, sans-serif';
    ctx.fillStyle = '#9ca3af';

    // Grid lines
    [0, 15, 30, 45, 60].forEach(v => {
        const y = pad.top + chartH - (v / maxVal) * chartH;
        ctx.strokeStyle = '#f3f4f6';
        ctx.lineWidth = 1;
        ctx.beginPath(); ctx.moveTo(pad.left, y); ctx.lineTo(W - pad.right, y); ctx.stroke();
        ctx.fillText(v, 4, y + 4);
    });

    data.forEach((group, gi) => {
        const groupX = pad.left + gi * (chartW / groups) + (chartW / groups - barW) / 2;
        group.values.forEach((val, vi) => {
            const barH = (val / maxVal) * chartH;
            const x = groupX + vi * subW;
            const y = pad.top + chartH - barH;
            ctx.fillStyle = colors[vi];
            ctx.beginPath();
            ctx.roundRect(x, y, subW - 2, barH, [3, 3, 0, 0]);
            ctx.fill();
        });
        ctx.fillStyle = '#9ca3af';
        ctx.fillText(group.label, groupX + barW / 2 - 10, H - 8);
    });
}

function drawLineChart(canvasId, datasets) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const W = canvas.width, H = canvas.height;
    const pad = { top: 20, right: 20, bottom: 40, left: 36 };
    const chartW = W - pad.left - pad.right;
    const chartH = H - pad.top - pad.bottom;
    const pts = datasets[0].values.length;
    const allVals = datasets.flatMap(d => d.values);
    const maxVal = Math.max(...allVals) + 20;

    ctx.clearRect(0, 0, W, H);
    ctx.font = '11px Sora, sans-serif';
    ctx.fillStyle = '#9ca3af';

    [0, 30, 60, 90, 120].forEach(v => {
        const y = pad.top + chartH - (v / maxVal) * chartH;
        ctx.strokeStyle = '#f3f4f6'; ctx.lineWidth = 1;
        ctx.beginPath(); ctx.moveTo(pad.left, y); ctx.lineTo(W - pad.right, y); ctx.stroke();
        ctx.fillText(v, 4, y + 4);
    });

    datasets.forEach(ds => {
        ctx.strokeStyle = ds.color;
        ctx.lineWidth = ds.dashed ? 1 : 2;
        ctx.setLineDash(ds.dashed ? [6, 4] : []);
        ctx.beginPath();
        ds.values.forEach((v, i) => {
            const x = pad.left + i * (chartW / (pts - 1));
            const y = pad.top + chartH - (v / maxVal) * chartH;
            i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
        });
        ctx.stroke();
        if (!ds.dashed) {
            ds.values.forEach((v, i) => {
                const x = pad.left + i * (chartW / (pts - 1));
                const y = pad.top + chartH - (v / maxVal) * chartH;
                ctx.beginPath(); ctx.arc(x, y, 4, 0, Math.PI * 2);
                ctx.fillStyle = ds.color; ctx.fill();
            });
        }
        ctx.setLineDash([]);
    });

    datasets[0].labels?.forEach((lbl, i) => {
        const x = pad.left + i * (chartW / (pts - 1));
        ctx.fillStyle = '#9ca3af'; ctx.fillText(lbl, x - 16, H - 8);
    });
}

function drawDonut(canvasId, data) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const W = canvas.width, H = canvas.height;
    const cx = W / 2, cy = H / 2, r = Math.min(W, H) / 2 - 20, inner = r * 0.55;
    const total = data.reduce((a, b) => a + b.value, 0);
    let angle = -Math.PI / 2;
    ctx.clearRect(0, 0, W, H);
    data.forEach(seg => {
        const sweep = (seg.value / total) * 2 * Math.PI;
        ctx.beginPath();
        ctx.moveTo(cx, cy);
        ctx.arc(cx, cy, r, angle, angle + sweep);
        ctx.closePath();
        ctx.fillStyle = seg.color;
        ctx.fill();
        angle += sweep;
    });
    ctx.beginPath();
    ctx.arc(cx, cy, inner, 0, Math.PI * 2);
    ctx.fillStyle = 'white';
    ctx.fill();
}
let priceChart = null;
let currentCrypto = 'bitcoin';
let currentDays = 7;

const cryptoIcons = {
    bitcoin: 'currency_bitcoin',
    ethereum: 'eco',
    cardano: 'token',
    dogecoin: 'pets',
    solana: 'circle',
    ripple: 'water_drop'
};

const cryptoNames = {
    bitcoin: 'Bitcoin',
    ethereum: 'Ethereum',
    cardano: 'Cardano',
    dogecoin: 'Dogecoin',
    solana: 'Solana',
    ripple: 'Ripple'
};

const cryptoSymbols = {
    bitcoin: 'BTC',
    ethereum: 'ETH',
    cardano: 'ADA',
    dogecoin: 'DOGE',
    solana: 'SOL',
    ripple: 'XRP'
};

function formatPrice(price) {
    if (price >= 1) return '$' + price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return '$' + price.toFixed(6);
}

function formatPriceShort(price) {
    if (price >= 1000) return '$' + (price / 1000).toFixed(1) + 'K';
    return '$' + price.toFixed(2);
}

function formatEur(price) {
    return '\u20AC' + price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatPercent(pct) {
    return (pct >= 0 ? '+' : '') + pct.toFixed(2) + '%';
}

function timeAgo(dateStr) {
    const now = new Date();
    const then = new Date(dateStr);
    const diffMs = now - then;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return diffMins + 'm ago';
    if (diffHours < 24) return diffHours + 'h ago';
    return diffDays + 'd ago';
}

function showToast(message, isError) {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = 'error-toast';
    toast.textContent = message;
    if (!isError) toast.style.background = '#007139';
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
}

function showSkeletonRows() {
    const tbody = document.getElementById('price-table-body');
    tbody.innerHTML = '';
    for (let i = 0; i < 6; i++) {
        tbody.innerHTML += `
            <tr class="skeleton-row">
                <td class="px-lg py-md"><div class="flex items-center gap-md">
                    <div class="skeleton" style="width:40px;height:40px;border-radius:50%"></div>
                    <div><div class="skeleton" style="width:80px;margin-bottom:6px"></div><div class="skeleton" style="width:40px"></div></div>
                </div></td>
                <td class="px-lg py-md text-right"><div class="skeleton" style="width:90px;float:right"></div></td>
                <td class="px-lg py-md text-right"><div class="skeleton" style="width:90px;float:right"></div></td>
                <td class="px-lg py-md text-right"><div class="skeleton" style="width:70px;float:right"></div></td>
            </tr>`;
    }
}

function showSkeletonAlerts() {
    const list = document.getElementById('alerts-list');
    list.innerHTML = '';
    for (let i = 0; i < 3; i++) {
        list.innerHTML += `
            <div class="bg-surface-container-highest/30 border border-outline-variant/10 rounded-lg p-md">
                <div class="skeleton" style="width:60%;margin-bottom:8px"></div>
                <div class="skeleton" style="width:40%"></div>
            </div>`;
    }
}

function showAlertsEmpty() {
    document.getElementById('alerts-list').innerHTML = `
        <div class="flex items-center justify-center h-32 text-on-surface-variant text-body-md">
            No alerts configured
        </div>`;
}

let priceFetchFailures = 0;

async function fetchPrices() {
    try {
        const res = await api('/api/crypto/list');
        if (!res.ok) throw new Error('HTTP ' + res.status);
        const data = await res.json();

        if (!data || data.length === 0) {
            if (priceFetchFailures > 1) showToast('No price data available. Check CoinGecko API key or network.', true);
            priceFetchFailures++;
            showSkeletonRows();
            return;
        }

        priceFetchFailures = 0;

        const tbody = document.getElementById('price-table-body');
        tbody.innerHTML = '';

        data.forEach((crypto, i) => {
            const icon = cryptoIcons[crypto.id] || 'currency_bitcoin';
            const changeClass = crypto.change24hPercent >= 0 ? 'text-primary-fixed-dim' : 'text-error';
            const arrow = crypto.change24hPercent >= 0 ? 'arrow_upward' : 'arrow_downward';

            const row = document.createElement('tr');
            row.className = 'hover:bg-surface-container-highest/30 transition-colors group cursor-pointer';
            row.style.animationDelay = (i * 50) + 'ms';
            row.onclick = () => { window.location.href = 'crypto-detail.html?id=' + crypto.id; };
            row.innerHTML = `
                <td class="px-lg py-md">
                    <div class="flex items-center gap-md">
                        <div class="w-10 h-10 rounded-full bg-surface-container-high flex items-center justify-center border border-outline-variant/20">
                            <span class="material-symbols-outlined text-primary-fixed" style="font-variation-settings:'FILL'1">${icon}</span>
                        </div>
                        <div>
                            <div class="font-bold text-body-md font-body-md">${crypto.name}</div>
                            <div class="text-on-surface-variant text-label-md font-label-md">${crypto.symbol}</div>
                        </div>
                    </div>
                </td>
                <td class="px-lg py-md text-right font-mono-data text-mono-data price-update" data-field="usd">${formatPrice(crypto.priceUsd)}</td>
                <td class="px-lg py-md text-right font-mono-data text-mono-data price-update" data-field="eur">${formatEur(crypto.priceEur)}</td>
                <td class="px-lg py-md text-right">
                    <span class="${changeClass} flex items-center justify-end gap-xs font-mono-data text-mono-data">
                        <span class="material-symbols-outlined text-sm">${arrow}</span> ${formatPercent(crypto.change24hPercent)}
                    </span>
                </td>`;
            tbody.appendChild(row);

            if (crypto.id === 'bitcoin' && document.getElementById('ticker-btc')) {
                document.getElementById('ticker-btc').textContent = formatPrice(crypto.priceUsd);
            }
            if (crypto.id === 'ethereum' && document.getElementById('ticker-eth')) {
                document.getElementById('ticker-eth').textContent = formatPrice(crypto.priceUsd);
            }
        });
    } catch (err) {
        console.error('fetchPrices failed:', err);
        if (priceFetchFailures > 1) showToast('Failed to load prices. Retrying...', true);
        priceFetchFailures++;
    }
}

async function fetchChartData(cryptoId, days) {
    try {
        const res = await api('/api/crypto/' + cryptoId + '/chart?days=' + days);
        if (!res.ok) throw new Error('HTTP ' + res.status);
        const data = await res.json();
        return data;
    } catch (err) {
        console.error('fetchChartData failed:', err);
        return [];
    }
}

async function updateChart() {
    const data = await fetchChartData(currentCrypto, currentDays);

    const canvas = document.getElementById('price-chart-canvas');
    const placeholder = document.getElementById('chart-placeholder');

    if (!data || data.length === 0) {
        placeholder.classList.remove('hidden');
        if (canvas) canvas.style.display = 'none';
        document.getElementById('chart-current-price').textContent = '---';
        document.getElementById('stat-low').textContent = '---';
        document.getElementById('stat-high').textContent = '---';
        document.getElementById('stat-range').textContent = '---';
        return;
    }

    placeholder.classList.add('hidden');
    canvas.style.display = 'block';

    const timestamps = data.map(d => {
        const dObj = new Date(d.timestamp);
        return dObj.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    });
    const prices = data.map(d => d.price_usd);

    const lastPrice = prices[prices.length - 1];
    const firstPrice = prices[0];
    const changePercent = firstPrice > 0 ? ((lastPrice - firstPrice) / firstPrice * 100) : 0;

    document.getElementById('chart-current-price').textContent = formatPrice(lastPrice);

    const statsLow = Math.min(...prices);
    const statsHigh = Math.max(...prices);
    const statsRange = ((statsHigh - statsLow) / statsLow * 100).toFixed(2);

    document.getElementById('stat-low').textContent = formatPriceShort(statsLow);
    document.getElementById('stat-high').textContent = formatPriceShort(statsHigh);
    document.getElementById('stat-range').textContent = (changePercent >= 0 ? '+' : '') + statsRange + '%';

    if (priceChart) {
        priceChart.destroy();
        priceChart = null;
    }

    const ctx = canvas.getContext('2d');

    const gradient = ctx.createLinearGradient(0, 0, 0, 280);
    gradient.addColorStop(0, 'rgba(0, 255, 136, 0.25)');
    gradient.addColorStop(1, 'rgba(0, 255, 136, 0)');

    const lineColor = changePercent >= 0 ? '#00ff88' : '#ff4757';
    const fillGradient = ctx.createLinearGradient(0, 0, 0, 280);
    fillGradient.addColorStop(0, changePercent >= 0 ? 'rgba(0, 255, 136, 0.25)' : 'rgba(255, 71, 87, 0.2)');
    fillGradient.addColorStop(1, 'rgba(0, 255, 136, 0)');

    priceChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: timestamps,
            datasets: [{
                label: 'Price USD',
                data: prices,
                borderColor: lineColor,
                backgroundColor: fillGradient,
                fill: true,
                tension: 0.4,
                borderWidth: 2.5,
                pointRadius: (ctx) => ctx.dataIndex === prices.length - 1 ? 4 : 0,
                pointBackgroundColor: lineColor,
                pointBorderColor: lineColor,
                pointHoverRadius: 6,
                pointHoverBackgroundColor: lineColor
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 800 },
            interaction: {
                intersect: false,
                mode: 'index'
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: '#1a1f2f',
                    titleColor: '#dee1f7',
                    bodyColor: '#00ff88',
                    borderColor: '#2a2d3e',
                    borderWidth: 1,
                    padding: 12,
                    callbacks: {
                        label: (ctx) => formatPrice(ctx.raw)
                    }
                }
            },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: { color: '#8b8fa3', maxTicksLimit: 6, font: { size: 11 } }
                },
                y: {
                    grid: { color: 'rgba(42, 45, 62, 0.3)' },
                    ticks: {
                        color: '#8b8fa3',
                        font: { size: 11 },
                        callback: (v) => formatPriceShort(v)
                    }
                }
            }
        }
    });
}

async function fetchAlerts() {
    try {
        const res = await api('/api/alerts');
        if (!res.ok) throw new Error('HTTP ' + res.status);
        const alerts = await res.json();

        const list = document.getElementById('alerts-list');
        const countTitle = document.getElementById('alerts-count-title');

        if (!alerts || alerts.length === 0) {
            showAlertsEmpty();
            countTitle.textContent = 'Active Alerts (0)';
            return;
        }

        const activeCount = alerts.filter(a => !a.isTriggered).length;
        countTitle.textContent = 'Active Alerts (' + activeCount + ')';

        list.innerHTML = '';

        alerts.forEach(alert => {
            const icon = cryptoIcons[alert.cryptoId] || 'currency_bitcoin';
            const name = cryptoNames[alert.cryptoId] || alert.cryptoId;
            const symbol = cryptoSymbols[alert.cryptoId] || alert.cryptoId.toUpperCase();
            const conditionText = alert.condition === 'above' ? 'Above' : 'Below';
            const badgeClass = alert.isTriggered ? 'alert-badge-triggered' : 'alert-badge-active';
            const badgeText = alert.isTriggered ? 'Triggered' : 'Active';
            const triggeredInfo = alert.triggeredAt
                ? '<div class="text-label-md font-label-md text-on-surface-variant">Triggered ' + timeAgo(alert.triggeredAt) + '</div>'
                : '';

            const div = document.createElement('div');
            div.className = 'bg-surface-container-highest/30 border border-outline-variant/10 rounded-lg p-md flex items-center justify-between group';
            div.innerHTML = `
                <div class="flex items-center gap-md">
                    <div class="w-8 h-8 rounded-full bg-primary-container/10 flex items-center justify-center">
                        <span class="material-symbols-outlined text-primary-fixed-dim text-sm" style="font-variation-settings:'FILL'1">notifications_active</span>
                    </div>
                    <div>
                        <div class="text-body-md font-body-md font-bold">${symbol} ${conditionText} $${alert.thresholdUsd.toLocaleString()}</div>
                        <div class="text-label-md font-label-md text-on-surface-variant">
                            Created ${timeAgo(alert.createdAt)} &middot; ${badgeText}
                            ${triggeredInfo}
                        </div>
                    </div>
                </div>
                <div class="flex items-center gap-sm">
                    <span class="${badgeClass}">${badgeText}</span>
                    <button class="opacity-0 group-hover:opacity-100 transition-opacity p-2 hover:bg-error-container/20 rounded-full text-error" onclick="onDeleteAlert(${alert.id})">
                        <span class="material-symbols-outlined text-sm">delete</span>
                    </button>
                </div>`;
            list.appendChild(div);
        });
    } catch (err) {
        console.error('fetchAlerts failed:', err);
    }
}

async function onCreateAlert() {
    const cryptoId = document.getElementById('alert-crypto').value;
    const condition = document.getElementById('alert-condition').value;
    const thresholdVal = document.getElementById('alert-threshold').value;
    const errorEl = document.getElementById('alert-form-error');

    errorEl.classList.add('hidden');

    if (!thresholdVal || thresholdVal <= 0) {
        errorEl.textContent = 'Please enter a valid price threshold';
        errorEl.classList.remove('hidden');
        return;
    }

    const thresholdUsd = parseFloat(thresholdVal);

    try {
        const res = await api('/api/alerts', {
            method: 'POST',
            body: { cryptoId, condition, thresholdUsd }
        });

        if (!res.ok) {
            const errData = await res.json();
            errorEl.textContent = errData.error || 'Failed to create alert';
            errorEl.classList.remove('hidden');
            return;
        }

        document.getElementById('alert-threshold').value = '';
        showToast('Alert created successfully', false);
        fetchAlerts();
    } catch (err) {
        console.error('createAlert failed:', err);
        errorEl.textContent = 'Network error. Please try again.';
        errorEl.classList.remove('hidden');
    }
}

async function onDeleteAlert(id) {
    try {
        const res = await api('/api/alerts/' + id, { method: 'DELETE' });
        if (res.ok) {
            showToast('Alert deleted', false);
            fetchAlerts();
        }
    } catch (err) {
        console.error('deleteAlert failed:', err);
        showToast('Failed to delete alert', true);
    }
}

async function onClearAllAlerts() {
    const res = await api('/api/alerts');
    const alerts = await res.json();
    for (const alert of alerts) {
        await api('/api/alerts/' + alert.id, { method: 'DELETE' });
    }
    fetchAlerts();
}

async function fetchHealth() {
    try {
        const res = await fetch(API_BASE + '/api/health');
        if (!res.ok) throw new Error('HTTP ' + res.status);
        const data = await res.json();

        const statusDot = document.getElementById('status-dot');
        const statusText = document.getElementById('status-text');
        const dbDot = document.getElementById('db-status-dot');
        const dbText = document.getElementById('db-status-text');
        const cacheAge = document.getElementById('cache-age-text');

        const isDbOk = data.db === 'connected';

        statusDot.className = isDbOk ? 'status-dot-connected' : 'status-dot-error';
        statusText.textContent = isDbOk ? 'System OK' : 'DB Error';
        statusText.className = isDbOk
            ? 'text-body-sm font-body-sm text-primary-fixed-dim'
            : 'text-body-sm font-body-sm text-error';

        dbDot.className = isDbOk ? 'status-dot-connected' : 'status-dot-error';
        dbText.textContent = isDbOk ? 'System OK' : 'DB Error';
        dbText.className = isDbOk
            ? 'text-body-sm font-body-sm text-primary-fixed-dim'
            : 'text-body-sm font-body-sm text-error';

        if (data.cache_age_seconds >= 0) {
            if (data.cache_age_seconds < 60) {
                cacheAge.textContent = 'Prices updated ' + data.cache_age_seconds + 's ago';
            } else {
                cacheAge.textContent = 'Prices updated ' + Math.floor(data.cache_age_seconds / 60) + 'm ago';
            }
        } else {
            cacheAge.textContent = 'No price data yet';
        }

        document.getElementById('cache-age').textContent = cacheAge.textContent;
    } catch (err) {
        const statusDot = document.getElementById('status-dot');
        const statusText = document.getElementById('status-text');
        statusDot.className = 'status-dot-error';
        statusText.textContent = 'Connection Error';
        statusText.className = 'text-body-sm font-body-sm text-error';
    }
}

function onCryptoChange() {
    currentCrypto = document.getElementById('chart-crypto-select').value;
    updateChart();
}

function onPeriodChange(days, btn) {
    currentDays = days;
    document.querySelectorAll('#period-buttons button').forEach(b => {
        b.className = 'px-3 py-1 text-label-md font-label-md text-on-surface-variant hover:text-on-surface';
    });
    btn.className = 'period-btn px-3 py-1 text-label-md font-label-md bg-surface-container-highest text-primary-fixed-dim rounded-md active';
    updateChart();
}

function scrollToSection(id) {
    if (id === 'dashboard') {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    } else if (id === 'alerts') {
        document.getElementById('alerts-section').scrollIntoView({ behavior: 'smooth' });
    }
}

function init() {
    showSkeletonRows();
    showSkeletonAlerts();
    fetchPrices();
    fetchHealth();
    updateChart();
    fetchAlerts();
    updateUserMenu();

    setInterval(() => {
        fetchPrices();
        fetchHealth();
    }, 30000);

    setInterval(fetchAlerts, 60000);
}

function updateUserMenu() {
    const user = getCurrentUser();
    if (!user) return;
    document.getElementById('user-name').textContent = user.name || user.email;
    document.getElementById('user-role').textContent = user.role.toUpperCase();

    var navUpgrade = document.getElementById('navbar-upgrade-btn');
    var sidebarUpgrade = document.getElementById('sidebar-upgrade-btn');

    if (user.role === 'user') {
        if (navUpgrade) navUpgrade.classList.remove('hidden');
        if (sidebarUpgrade) sidebarUpgrade.classList.remove('hidden');
    } else {
        if (navUpgrade) navUpgrade.classList.add('hidden');
        if (sidebarUpgrade) sidebarUpgrade.classList.add('hidden');
    }
}

document.addEventListener('DOMContentLoaded', init);

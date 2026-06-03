(() => {
  const ALL = '__all__';
  const BRAND_COLOR = '#0068C3';
  const fmtCurrency = (v) => new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(v || 0);
  const fmtPercent = (v) => (v || 0).toFixed(1) + ' %';
  const fmtDate = (d) => (d ? new Date(d).toLocaleDateString('pt-PT') : '—');
  const fmtDailyLabel = (d) => {
    if (!d) {
      return '—';
    }

    const date = new Date(d);
    return date.toLocaleDateString('pt-PT', { day: '2-digit', month: '2-digit' });
  };

  let payload = null;

  function normalizeKey(key) {
    return !key || key === ALL ? ALL : key;
  }

  function getCampaignKey() {
    return normalizeKey(new URLSearchParams(window.location.search).get('campaign'));
  }

  function setCampaignKey(key) {
    const effectiveKey = normalizeKey(key);
    const url = new URL(window.location.href);
    if (effectiveKey === ALL) {
      url.searchParams.delete('campaign');
    } else {
      url.searchParams.set('campaign', effectiveKey);
    }

    window.history.replaceState({}, '', url.pathname + url.search);
    applyFilter(effectiveKey);
  }

  function getDetail(key) {
    const effectiveKey = normalizeKey(key);
    if (effectiveKey === ALL) {
      return payload?.all || null;
    }

    return (payload?.campaigns || []).find((c) => c.campaignKey === effectiveKey) || null;
  }

  function updateNavLinks(key) {
    const effectiveKey = normalizeKey(key);
    document.querySelectorAll('.nav a').forEach((a) => {
      const url = new URL(a.href, window.location.origin);
      if (effectiveKey === ALL) {
        url.searchParams.delete('campaign');
      } else {
        url.searchParams.set('campaign', effectiveKey);
      }

      a.href = url.pathname + url.search;
    });
  }

  function escapeHtml(text) {
    return String(text ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function getReportBasePath() {
    let path = window.location.pathname;
    if (!path.endsWith('/')) {
      path = path.replace(/\/[^/]*$/, '/');
    }

    if (!path.endsWith('/')) {
      path += '/';
    }

    return path;
  }

  async function loadPayload() {
    const inline = document.getElementById('reportFilterData');
    if (inline?.textContent?.trim()) {
      try {
        return JSON.parse(inline.textContent);
      } catch {
        // Fall back to fetching report-data.json.
      }
    }

    const response = await fetch(getReportBasePath() + 'report-data.json');
    if (!response.ok) {
      throw new Error('report-data.json not found');
    }

    return response.json();
  }

  function getChart(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || typeof Chart === 'undefined') {
      return null;
    }

    return Chart.getChart(canvas);
  }

  function updateSingleDatasetChart(canvasId, labels, values) {
    const chart = getChart(canvasId);
    if (!chart) {
      return;
    }

    chart.data.labels = labels;
    chart.data.datasets[0].data = values;
    chart.update();
  }

  function updateLineChart(canvasId, labels, values, label) {
    const chart = getChart(canvasId);
    if (!chart) {
      return;
    }

    chart.data.labels = labels;
    chart.data.datasets[0].data = values;
    if (label) {
      chart.data.datasets[0].label = label;
    }

    chart.update();
  }

  function updateMultiDatasetBarChart(canvasId, labels, datasets) {
    const chart = getChart(canvasId);
    if (!chart) {
      return;
    }

    chart.data.labels = labels;
    datasets.forEach((dataset, index) => {
      if (chart.data.datasets[index]) {
        chart.data.datasets[index].data = dataset.data;
        if (dataset.label) {
          chart.data.datasets[index].label = dataset.label;
        }
      }
    });

    chart.update();
  }

  function renderFoodBankTable(rows) {
    const tbody = document.getElementById('foodBankTableBody');
    if (!tbody) {
      return;
    }

    tbody.innerHTML = (rows || [])
      .map(
        (r) =>
          '<tr><td>' +
          escapeHtml(r.foodBankName) +
          '</td><td>' +
          fmtCurrency(r.paidAmount) +
          '</td><td>' +
          r.paidCount +
          '</td><td>' +
          (r.productUnits || 0).toLocaleString('pt-PT') +
          '</td><td>' +
          fmtPercent(r.sharePercent) +
          '</td></tr>',
      )
      .join('');
  }

  function renderProductTable(rows) {
    const tbody = document.getElementById('productTableBody');
    if (!tbody) {
      return;
    }

    tbody.innerHTML = (rows || [])
      .map(
        (r) =>
          '<tr><td>' +
          escapeHtml(r.productName) +
          '</td><td>' +
          escapeHtml(r.unitOfMeasure || '') +
          '</td><td>' +
          (r.quantity || 0).toLocaleString('pt-PT') +
          '</td><td>' +
          fmtCurrency(r.value) +
          '</td><td>' +
          fmtPercent(r.sharePercent) +
          '</td></tr>',
      )
      .join('');
  }

  function renderPaymentTable(rows) {
    const tbody = document.getElementById('paymentTableBody');
    if (!tbody) {
      return;
    }

    tbody.innerHTML = (rows || [])
      .map(
        (r) =>
          '<tr><td>' +
          escapeHtml(r.paymentTypeLabel) +
          '</td><td>' +
          fmtCurrency(r.paidAmount) +
          '</td><td>' +
          r.paidCount +
          '</td><td>' +
          fmtCurrency(r.averagePaidAmount) +
          '</td><td>' +
          fmtCurrency(r.maxPaidAmount) +
          '</td><td>' +
          fmtPercent(r.sharePercent) +
          '</td></tr>',
      )
      .join('');
  }

  function renderCampaignTable(rows) {
    const tbody = document.getElementById('campaignTableBody');
    if (!tbody) {
      return;
    }

    tbody.innerHTML = (rows || [])
      .map(
        (r) =>
          '<tr><td>' +
          escapeHtml(r.campaignName) +
          '</td><td>' +
          fmtCurrency(r.paidAmount) +
          '</td><td>' +
          r.paidCount +
          '</td><td>' +
          r.pendingCount +
          '</td><td>' +
          fmtCurrency(r.averagePaidAmount) +
          '</td><td>' +
          fmtPercent(r.conversionPercent) +
          '</td></tr>',
      )
      .join('');
  }

  function updateKpiGrid(summary) {
    const grid = document.getElementById('kpiGrid');
    if (!grid || !summary) {
      return;
    }

    grid.innerHTML =
      '<article class="kpi"><h3>Total angariado (pago)</h3><p class="kpi-value">' +
      fmtCurrency(summary.totalPaidAmount) +
      '</p><p class="kpi-hint">Receita confirmada</p></article>' +
      '<article class="kpi"><h3>Doações pagas</h3><p class="kpi-value">' +
      summary.paidDonationCount +
      '</p><p class="kpi-hint">Ticket médio ' +
      fmtCurrency(summary.averagePaidAmount) +
      '</p></article>' +
      '<article class="kpi"><h3>Taxa de conversão</h3><p class="kpi-value">' +
      fmtPercent(summary.paymentConversionPercent) +
      '</p><p class="kpi-hint">' +
      summary.pendingDonationCount +
      ' pendentes · ' +
      summary.failedDonationCount +
      ' com erro</p></article>' +
      '<article class="kpi"><h3>Unidades de produto</h3><p class="kpi-value">' +
      (summary.totalProductUnits || 0).toLocaleString('pt-PT') +
      '</p><p class="kpi-hint">Valor catálogo ' +
      fmtCurrency(summary.totalProductValue) +
      '</p></article>' +
      '<article class="kpi"><h3>Bancos alimentares</h3><p class="kpi-value">' +
      summary.activeFoodBankCount +
      '</p><p class="kpi-hint">Com doações confirmadas</p></article>' +
      '<article class="kpi"><h3>Doações em numerário</h3><p class="kpi-value">' +
      fmtPercent(summary.cashDonationSharePercent) +
      '</p><p class="kpi-hint">Parte das doações pagas</p></article>';
  }

  function updatePaymentKpi(summary) {
    const grid = document.getElementById('paymentKpiGrid');
    if (!grid || !summary) {
      return;
    }

    grid.innerHTML =
      '<article class="kpi"><h3>Maior doação (global)</h3><p class="kpi-value">' +
      fmtCurrency(summary.maxSingleDonation) +
      '</p><p class="kpi-hint">Valor máximo entre doações pagas</p></article>';
  }

  function updateTimingKpi(temporal) {
    const grid = document.getElementById('timingKpiGrid');
    if (!grid || !temporal) {
      return;
    }

    grid.innerHTML =
      '<article class="kpi"><h3>Hora com mais doações</h3><p class="kpi-value">' +
      escapeHtml(temporal.peakHourLabel || '—') +
      '</p><p class="kpi-hint">' +
      (temporal.peakHourCount || 0).toLocaleString('pt-PT') +
      ' doações pagas</p></article>' +
      '<article class="kpi"><h3>Dia com mais doações</h3><p class="kpi-value">' +
      escapeHtml(temporal.peakDayLabel || '—') +
      '</p><p class="kpi-hint">' +
      (temporal.peakDayCount || 0).toLocaleString('pt-PT') +
      ' doações pagas</p></article>';
  }

  function campaignRowsFromPayload() {
    return (payload?.campaigns || []).map((c) => ({
      campaignName: c.campaignName,
      paidAmount: c.summary?.totalPaidAmount || 0,
      paidCount: c.summary?.paidDonationCount || 0,
      pendingCount: c.pendingCount || 0,
      averagePaidAmount: c.summary?.averagePaidAmount || 0,
      conversionPercent: c.conversionPercent || 0,
    }));
  }

  function campaignRowFromDetail(detail) {
    return {
      campaignName: detail.campaignName,
      paidAmount: detail.summary?.totalPaidAmount || 0,
      paidCount: detail.summary?.paidDonationCount || 0,
      pendingCount: detail.pendingCount || 0,
      averagePaidAmount: detail.summary?.averagePaidAmount || 0,
      conversionPercent: detail.conversionPercent || 0,
    };
  }

  function updateIndexPage(detail) {
    updateKpiGrid(detail.summary);

    const daily = detail.dailyTrend || [];
    updateLineChart(
      'dailyChart',
      daily.map((d) => fmtDailyLabel(d.date)),
      daily.map((d) => d.paidAmount || 0),
      '€ pagos',
    );

    const statuses = detail.paymentStatuses || [];
    updateSingleDatasetChart(
      'statusChart',
      statuses.map((s) => s.statusLabel),
      statuses.map((s) => s.count || 0),
    );
  }

  function updateFoodBanksPage(detail, key) {
    const intro = document.getElementById('foodBankIntro');
    if (intro) {
      intro.textContent =
        key === ALL
          ? 'Distribuição geográfica das doações confirmadas.'
          : 'Doações confirmadas na campanha ' + detail.campaignName + '.';
    }

    const rows = detail.foodBanks || [];
    const top = rows.slice(0, 15);
    renderFoodBankTable(rows);
    updateSingleDatasetChart(
      'fbAmountChart',
      top.map((r) => r.foodBankName),
      top.map((r) => r.paidAmount || 0),
    );
    updateSingleDatasetChart(
      'fbShareChart',
      top.map((r) => r.foodBankName),
      top.map((r) => r.sharePercent || 0),
    );
  }

  function updateProductsPage(detail, key) {
    const intro = document.getElementById('productIntro');
    if (intro) {
      intro.textContent =
        key === ALL
          ? 'Produtos doados (doações confirmadas).'
          : 'Produtos doados na campanha ' + detail.campaignName + '.';
    }

    const rows = detail.products || [];
    const top = rows.slice(0, 15);
    renderProductTable(rows);
    updateSingleDatasetChart(
      'productChart',
      top.map((r) => r.productName),
      top.map((r) => r.quantity || 0),
    );
  }

  function updatePaymentsPage(detail) {
    updatePaymentKpi(detail.summary);

    const payments = detail.payments || [];
    renderPaymentTable(payments);
    updateSingleDatasetChart(
      'payAmountChart',
      payments.map((p) => p.paymentTypeLabel),
      payments.map((p) => p.paidAmount || 0),
    );
    updateSingleDatasetChart(
      'payAvgChart',
      payments.map((p) => p.paymentTypeLabel),
      payments.map((p) => p.averagePaidAmount || 0),
    );
    updateSingleDatasetChart(
      'payMaxChart',
      payments.map((p) => p.paymentTypeLabel),
      payments.map((p) => p.maxPaidAmount || 0),
    );
  }

  function updateTimingPage(detail) {
    const temporal = detail.temporalAnalysis;
    if (!temporal) {
      return;
    }

    updateTimingKpi(temporal);

    const hourly = temporal.hourlyDistribution || [];
    updateLineChart(
      'hourCountChart',
      hourly.map((h) => h.hourLabel),
      hourly.map((h) => h.paidCount || 0),
      'Doações',
    );
    updateLineChart(
      'hourAmountChart',
      hourly.map((h) => h.hourLabel),
      hourly.map((h) => h.paidAmount || 0),
      '€ pagos',
    );

    const weekday = temporal.weekdayDistribution || [];
    updateLineChart(
      'weekdayCountChart',
      weekday.map((d) => d.dayLabel),
      weekday.map((d) => d.paidCount || 0),
      'Doações',
    );
    updateLineChart(
      'weekdayAmountChart',
      weekday.map((d) => d.dayLabel),
      weekday.map((d) => d.paidAmount || 0),
      '€ pagos',
    );
  }

  function updateCampaignsPage(detail, key) {
    const rows = key === ALL ? campaignRowsFromPayload() : [campaignRowFromDetail(detail)];
    renderCampaignTable(rows);
    updateSingleDatasetChart(
      'campaignChart',
      rows.map((r) => r.campaignName),
      rows.map((r) => r.paidAmount || 0),
    );
  }

  function updateHeader(detail, key) {
    const subtitle = document.getElementById('reportSubtitle');
    const meta = document.getElementById('reportMeta');
    if (subtitle) {
      subtitle.textContent = key === ALL ? 'Todas as campanhas' : detail.campaignName;
    }

    if (meta && detail.periodStart) {
      meta.textContent =
        'Período: ' +
        fmtDate(detail.periodStart) +
        ' – ' +
        fmtDate(detail.periodEnd) +
        (key === ALL ? ' · Todas as campanhas' : ' · Campanha filtrada');
    }
  }

  function applyFilter(key) {
    const effectiveKey = normalizeKey(key);
    updateNavLinks(effectiveKey);

    const detail = getDetail(effectiveKey);
    if (!detail) {
      return;
    }

    updateHeader(detail, effectiveKey);

    const page = document.body.dataset.page;
    if (page === 'index.html') {
      updateIndexPage(detail);
    } else if (page === 'food-banks.html') {
      updateFoodBanksPage(detail, effectiveKey);
    } else if (page === 'products.html') {
      updateProductsPage(detail, effectiveKey);
    } else if (page === 'payments.html') {
      updatePaymentsPage(detail);
    } else if (page === 'timing.html') {
      updateTimingPage(detail);
    } else if (page === 'campaigns.html') {
      updateCampaignsPage(detail, effectiveKey);
    }
  }

  async function init() {
    try {
      payload = await loadPayload();
    } catch {
      return;
    }

    const select = document.getElementById('campaignFilter');
    if (!select || !Array.isArray(payload?.options) || payload.options.length === 0) {
      return;
    }

    select.innerHTML = '';
    payload.options.forEach((option) => {
      const element = document.createElement('option');
      element.value = option.key;
      element.textContent = option.label;
      select.appendChild(element);
    });

    const key = getCampaignKey();
    const validKeys = payload.options.map((o) => o.key);
    select.value = validKeys.includes(key) ? key : ALL;
    select.addEventListener('change', () => setCampaignKey(select.value));
    applyFilter(select.value);
  }

  document.addEventListener('DOMContentLoaded', init);
})();

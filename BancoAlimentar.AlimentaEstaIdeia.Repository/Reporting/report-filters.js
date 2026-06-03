(() => {
  const ACTIVE = '__active__';
  const fmtCurrency = (v) => new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(v || 0);
  const fmtPercent = (v) => (v || 0).toFixed(1) + ' %';
  const fmtDate = (d) => (d ? new Date(d).toLocaleDateString('pt-PT') : '—');
  let payload = null;

  function getCampaignKey() {
    return new URLSearchParams(window.location.search).get('campaign') || ACTIVE;
  }

  function setCampaignKey(key) {
    const url = new URL(window.location.href);
    if (!key || key === ACTIVE) {
      url.searchParams.delete('campaign');
    } else {
      url.searchParams.set('campaign', key);
    }

    window.location.href = url.toString();
  }

  function findCampaign(key) {
    return (payload?.campaigns || []).find((c) => c.campaignKey === key);
  }

  function updateNavLinks(key) {
    document.querySelectorAll('.nav a').forEach((a) => {
      const url = new URL(a.href, window.location.origin);
      if (!key || key === ACTIVE) {
        url.searchParams.delete('campaign');
      } else {
        url.searchParams.set('campaign', key);
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

  function applyFilter(key) {
    updateNavLinks(key);
    if (!key || key === ACTIVE) {
      return;
    }

    const detail = findCampaign(key);
    if (!detail) {
      return;
    }

    const page = document.body.dataset.page;
    const subtitle = document.getElementById('reportSubtitle');
    const meta = document.getElementById('reportMeta');
    if (subtitle) {
      subtitle.textContent = detail.campaignName;
    }

    if (meta) {
      meta.textContent =
        'Período: ' +
        fmtDate(detail.periodStart) +
        ' – ' +
        fmtDate(detail.periodEnd) +
        ' · Campanha filtrada';
    }

    if (page === 'index.html') {
      updateKpiGrid(detail.summary);
    }

    if (page === 'food-banks.html') {
      const intro = document.getElementById('foodBankIntro');
      if (intro) {
        intro.textContent = 'Doações confirmadas na campanha ' + detail.campaignName + '.';
      }

      renderFoodBankTable(detail.foodBanks);
    }

    if (page === 'products.html') {
      const intro = document.getElementById('productIntro');
      if (intro) {
        intro.textContent = 'Produtos doados na campanha ' + detail.campaignName + '.';
      }

      renderProductTable(detail.products);
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

    select.innerHTML = payload.options
      .map((o) => '<option value="' + escapeHtml(o.key) + '">' + escapeHtml(o.label) + '</option>')
      .join('');
    const key = getCampaignKey();
    select.value = payload.options.some((o) => o.key === key) ? key : ACTIVE;
    select.addEventListener('change', () => setCampaignKey(select.value));
    applyFilter(select.value);
  }

  document.addEventListener('DOMContentLoaded', init);
})();

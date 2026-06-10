(() => {
  const ALL = '__all__';
  const NO_FREQUENCY = '__none__';
  const SUBSCRIPTION_PAGE_SIZE = 25;
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

  function getSubscriptionListFilters() {
    const params = new URLSearchParams(window.location.search);
    const status = params.get('status') || '';
    const frequency = params.get('frequency') || '';
    const page = Math.max(1, parseInt(params.get('page') || '1', 10) || 1);
    return { status, frequency, page };
  }

  function buildSubscriptionListUrl(campaignKey, filters) {
    const url = new URL('subscription-list.html', window.location.href);
    const effectiveKey = normalizeKey(campaignKey);
    if (effectiveKey !== ALL) {
      url.searchParams.set('campaign', effectiveKey);
    }

    const status = filters?.status || '';
    const frequency = filters?.frequency || '';
    const page = filters?.page || 1;
    if (status) {
      url.searchParams.set('status', status);
    }

    if (frequency) {
      url.searchParams.set('frequency', frequency);
    }

    if (page > 1) {
      url.searchParams.set('page', String(page));
    }

    return url.pathname + url.search;
  }

  function setSubscriptionListFilters(filters) {
    const url = new URL(window.location.href);
    if (filters.status) {
      url.searchParams.set('status', filters.status);
    } else {
      url.searchParams.delete('status');
    }

    if (filters.frequency) {
      url.searchParams.set('frequency', filters.frequency);
    } else {
      url.searchParams.delete('frequency');
    }

    if (filters.page > 1) {
      url.searchParams.set('page', String(filters.page));
    } else {
      url.searchParams.delete('page');
    }

    window.history.replaceState({}, '', url.pathname + url.search);
    applyFilter(getCampaignKey());
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
    const listFilters = document.body.dataset.page === 'subscription-list.html' ? getSubscriptionListFilters() : null;
    document.querySelectorAll('.nav a').forEach((a) => {
      const url = new URL(a.href, window.location.origin);
      if (effectiveKey === ALL) {
        url.searchParams.delete('campaign');
      } else {
        url.searchParams.set('campaign', effectiveKey);
      }

      url.searchParams.delete('status');
      url.searchParams.delete('frequency');
      url.searchParams.delete('page');
      if (listFilters && url.pathname.endsWith('subscription-list.html')) {
        if (listFilters.status) {
          url.searchParams.set('status', listFilters.status);
        }

        if (listFilters.frequency) {
          url.searchParams.set('frequency', listFilters.frequency);
        }

        if (listFilters.page > 1) {
          url.searchParams.set('page', String(listFilters.page));
        }
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
      '</p><p class="kpi-hint">Doação média ' +
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

  function getCampaignDonationStats(key) {
    const effectiveKey = normalizeKey(key);
    if (effectiveKey === ALL) {
      const comparison = payload?.comparison;
      return {
        labels: comparison?.campaignLabels || [],
        averages: (comparison?.campaignAverageDonations || []).map((value) => value || 0),
        maximums: (comparison?.campaignMaxDonations || []).map((value) => value || 0),
      };
    }

    const detail = getDetail(effectiveKey);
    if (!detail) {
      return { labels: [], averages: [], maximums: [] };
    }

    return {
      labels: [detail.campaignName],
      averages: [detail.summary?.averagePaidAmount || 0],
      maximums: [detail.summary?.maxSingleDonation || 0],
    };
  }

  function updateCampaignDonationStatsChart(key) {
    const stats = getCampaignDonationStats(key);
    updateSingleDatasetChart('payCampaignAvgChart', stats.labels, stats.averages);
    updateSingleDatasetChart('payCampaignMaxChart', stats.labels, stats.maximums);
  }

  function updatePaymentsPage(detail, key) {
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
    updateCampaignDonationStatsChart(key);
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

  function updateSubscriptionKpiGrid(subscriptions, campaignKey) {
    const grid = document.getElementById('subscriptionKpiGrid');
    if (!grid || !subscriptions) {
      return;
    }

    const listUrl = buildSubscriptionListUrl(campaignKey, { page: 1 });
    grid.innerHTML =
      '<article class="kpi"><h3>Total via subscrições (pago)</h3><p class="kpi-value">' +
      fmtCurrency(subscriptions.totalPaidAmount) +
      '</p><p class="kpi-hint">Apenas doações com pagamento confirmado</p></article>' +
      '<article class="kpi"><h3>Subscrições</h3><p class="kpi-value"><a href="' +
      escapeHtml(listUrl) +
      '">' +
      (subscriptions.subscriptionCount || 0).toLocaleString('pt-PT') +
      '</a></p><p class="kpi-hint">Subscrições com doações associadas</p></article>' +
      '<article class="kpi"><h3>Doações pagas</h3><p class="kpi-value">' +
      (subscriptions.paidDonationCount || 0).toLocaleString('pt-PT') +
      '</p><p class="kpi-hint">Doações de subscrição confirmadas</p></article>';
  }

  function renderSubscriptionStatusTable(rows, campaignKey) {
    const tbody = document.getElementById('subscriptionStatusTableBody');
    if (!tbody) {
      return;
    }

    tbody.innerHTML = (rows || [])
      .map((r) => {
        const listUrl = buildSubscriptionListUrl(campaignKey, {
          status: r.statusKey || '',
          page: 1,
        });
        return (
          '<tr><td>' +
          escapeHtml(r.statusLabel) +
          '</td><td><a href="' +
          escapeHtml(listUrl) +
          '">' +
          (r.count || 0).toLocaleString('pt-PT') +
          '</a></td><td>' +
          fmtPercent(r.sharePercent) +
          '</td></tr>'
        );
      })
      .join('');
  }

  function renderSubscriptionForecastPeriod(subscriptions) {
    const element = document.getElementById('subscriptionForecastPeriod');
    if (!element || !subscriptions) {
      return;
    }

    if (!subscriptions.forecastPeriodStart || !subscriptions.forecastPeriodEnd) {
      element.textContent = '';
      return;
    }

    element.textContent =
      'Previsão para subscrições ativas até ' +
      fmtDate(subscriptions.forecastPeriodEnd) +
      ' (desde ' +
      fmtDate(subscriptions.forecastPeriodStart) +
      ').';
  }

  function frequencyFilterValue(frequencyLabel) {
    if (!frequencyLabel || frequencyLabel === '(sem frequência)') {
      return NO_FREQUENCY;
    }

    return frequencyLabel;
  }

  function renderSubscriptionFrequencyTable(rows, campaignKey) {
    const tbody = document.getElementById('subscriptionFrequencyTableBody');
    if (!tbody) {
      return;
    }

    tbody.innerHTML = (rows || [])
      .map((r) => {
        const listUrl = buildSubscriptionListUrl(campaignKey, {
          frequency: frequencyFilterValue(r.frequencyLabel),
          page: 1,
        });
        return (
          '<tr><td>' +
          escapeHtml(r.frequencyLabel) +
          '</td><td><a href="' +
          escapeHtml(listUrl) +
          '">' +
          (r.subscriptionCount || 0).toLocaleString('pt-PT') +
          '</a></td><td>' +
          fmtPercent(r.subscriptionSharePercent) +
          '</td><td>' +
          fmtCurrency(r.totalPaidAmount) +
          '</td><td>' +
          fmtCurrency(r.averageDonationAmount) +
          '</td><td>' +
          fmtCurrency(r.expectedUpcomingAmount) +
          '</td></tr>'
        );
      })
      .join('');
  }

  function filterSubscriptionRows(rows, filters) {
    return (rows || []).filter((row) => {
      if (filters.status && row.statusKey !== filters.status) {
        return false;
      }

      if (filters.frequency) {
        const rowFrequency = row.frequencyKey || NO_FREQUENCY;
        if (rowFrequency !== filters.frequency) {
          return false;
        }
      }

      return true;
    });
  }

  function renderSubscriptionListTable(rows) {
    const tbody = document.getElementById('subscriptionListTableBody');
    if (!tbody) {
      return;
    }

    if (!rows.length) {
      tbody.innerHTML = '<tr><td colspan="5">Não foram encontradas subscrições com os filtros selecionados.</td></tr>';
      return;
    }

    tbody.innerHTML = rows
      .map(
        (r) =>
          '<tr><td>' +
          escapeHtml(r.statusLabel) +
          '</td><td>' +
          escapeHtml(r.frequency || '—') +
          '</td><td>' +
          fmtDate(r.created) +
          '</td><td>' +
          (r.paidDonationCount || 0).toLocaleString('pt-PT') +
          '</td><td>' +
          fmtCurrency(r.totalPaidAmount) +
          '</td></tr>',
      )
      .join('');
  }

  function renderSubscriptionListPagination(totalItems, filters, campaignKey) {
    const nav = document.getElementById('subscriptionListPagination');
    if (!nav) {
      return;
    }

    const totalPages = Math.max(1, Math.ceil(totalItems / SUBSCRIPTION_PAGE_SIZE));
    const currentPage = Math.min(filters.page, totalPages);
    const parts = [];
    const baseFilters = { status: filters.status, frequency: filters.frequency };

    if (currentPage > 1) {
      parts.push(
        '<a href="' +
          escapeHtml(buildSubscriptionListUrl(campaignKey, { ...baseFilters, page: currentPage - 1 })) +
          '" data-page="' +
          (currentPage - 1) +
          '">Anterior</a>',
      );
    } else {
      parts.push('<span class="disabled">Anterior</span>');
    }

    const windowSize = 5;
    let startPage = Math.max(1, currentPage - Math.floor(windowSize / 2));
    let endPage = Math.min(totalPages, startPage + windowSize - 1);
    startPage = Math.max(1, endPage - windowSize + 1);

    for (let page = startPage; page <= endPage; page += 1) {
      if (page === currentPage) {
        parts.push('<span class="active">' + page + '</span>');
      } else {
        parts.push(
          '<a href="' +
            escapeHtml(buildSubscriptionListUrl(campaignKey, { ...baseFilters, page })) +
            '" data-page="' +
            page +
            '">' +
            page +
            '</a>',
        );
      }
    }

    if (currentPage < totalPages) {
      parts.push(
        '<a href="' +
          escapeHtml(buildSubscriptionListUrl(campaignKey, { ...baseFilters, page: currentPage + 1 })) +
          '" data-page="' +
          (currentPage + 1) +
          '">Seguinte</a>',
      );
    } else {
      parts.push('<span class="disabled">Seguinte</span>');
    }

    const startItem = totalItems === 0 ? 0 : (currentPage - 1) * SUBSCRIPTION_PAGE_SIZE + 1;
    const endItem = Math.min(currentPage * SUBSCRIPTION_PAGE_SIZE, totalItems);
    parts.push(
      '<span class="summary">A mostrar ' +
        startItem +
        '–' +
        endItem +
        ' de ' +
        totalItems.toLocaleString('pt-PT') +
        '</span>',
    );

    nav.innerHTML = parts.join('');
    nav.querySelectorAll('a[data-page]').forEach((link) => {
      link.addEventListener('click', (event) => {
        event.preventDefault();
        const page = parseInt(link.getAttribute('data-page') || '1', 10) || 1;
        setSubscriptionListFilters({ ...filters, page });
      });
    });
  }

  function updateSubscriptionListPage(detail, campaignKey) {
    const subscriptions = detail.subscriptions;
    const filters = getSubscriptionListFilters();
    const intro = document.getElementById('subscriptionListIntro');
    if (intro) {
      intro.textContent =
        campaignKey === ALL
          ? 'Todas as subscrições com doações associadas (todas as campanhas).'
          : 'Subscrições com doações associadas na campanha ' + detail.campaignName + '.';
    }

    const summary = document.getElementById('subscriptionListFilterSummary');
    if (summary) {
      const parts = [];
      if (filters.status) {
        const statusRow = (subscriptions?.statusBreakdown || []).find((row) => row.statusKey === filters.status);
        parts.push('Estado: ' + (statusRow?.statusLabel || filters.status));
      }

      if (filters.frequency) {
        const frequencyLabel =
          filters.frequency === NO_FREQUENCY
            ? '(sem frequência)'
            : (subscriptions?.frequencyBreakdown || []).find((row) => frequencyFilterValue(row.frequencyLabel) === filters.frequency)
                ?.frequencyLabel || filters.frequency;
        parts.push('Frequência: ' + frequencyLabel);
      }

      summary.textContent = parts.length ? parts.join(' · ') : 'Sem filtros de estado ou frequência.';
    }

    const backLink = document.getElementById('subscriptionListBackLink');
    if (backLink) {
      const url = new URL('subscriptions.html', window.location.href);
      if (campaignKey !== ALL) {
        url.searchParams.set('campaign', campaignKey);
      }

      backLink.href = url.pathname + url.search;
    }

    const clearFilters = document.getElementById('subscriptionListClearFilters');
    if (clearFilters) {
      clearFilters.href = buildSubscriptionListUrl(campaignKey, { page: 1 });
    }

    if (!subscriptions) {
      renderSubscriptionListTable([]);
      renderSubscriptionListPagination(0, filters, campaignKey);
      return;
    }

    const filteredRows = filterSubscriptionRows(subscriptions.subscriptions, filters);
    const totalPages = Math.max(1, Math.ceil(filteredRows.length / SUBSCRIPTION_PAGE_SIZE));
    const currentPage = Math.min(filters.page, totalPages);
    if (currentPage !== filters.page) {
      setSubscriptionListFilters({ ...filters, page: currentPage });
      return;
    }

    const pageRows = filteredRows.slice(
      (currentPage - 1) * SUBSCRIPTION_PAGE_SIZE,
      currentPage * SUBSCRIPTION_PAGE_SIZE,
    );
    renderSubscriptionListTable(pageRows);
    renderSubscriptionListPagination(filteredRows.length, { ...filters, page: currentPage }, campaignKey);
  }

  function updateSubscriptionsPage(detail, key) {
    const subscriptions = detail.subscriptions;
    const intro = document.getElementById('subscriptionIntro');
    if (intro) {
      intro.textContent =
        key === ALL
          ? 'Doações recorrentes e respetivo desempenho (todas as campanhas).'
          : 'Doações recorrentes na campanha ' + detail.campaignName + '.';
    }

    if (!subscriptions) {
      return;
    }

    updateSubscriptionKpiGrid(subscriptions, key);

    const statusRows = subscriptions.statusBreakdown || [];
    const frequencyRows = subscriptions.frequencyBreakdown || [];
    renderSubscriptionForecastPeriod(subscriptions);
    renderSubscriptionStatusTable(statusRows, key);
    renderSubscriptionFrequencyTable(frequencyRows, key);
    updateSingleDatasetChart(
      'subscriptionStatusChart',
      statusRows.map((r) => r.statusLabel),
      statusRows.map((r) => r.count || 0),
    );
    updateSingleDatasetChart(
      'subscriptionFrequencyCountChart',
      frequencyRows.map((r) => r.frequencyLabel),
      frequencyRows.map((r) => r.subscriptionCount || 0),
    );
    updateSingleDatasetChart(
      'subscriptionFrequencyAmountChart',
      frequencyRows.map((r) => r.frequencyLabel),
      frequencyRows.map((r) => r.totalPaidAmount || 0),
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
      updatePaymentsPage(detail, effectiveKey);
    } else if (page === 'timing.html') {
      updateTimingPage(detail);
    } else if (page === 'campaigns.html') {
      updateCampaignsPage(detail, effectiveKey);
    } else if (page === 'subscriptions.html') {
      updateSubscriptionsPage(detail, effectiveKey);
    } else if (page === 'subscription-list.html') {
      updateSubscriptionListPage(detail, effectiveKey);
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
    select.addEventListener('change', () => {
      if (document.body.dataset.page === 'subscription-list.html') {
        const filters = getSubscriptionListFilters();
        const url = new URL(window.location.href);
        const effectiveKey = normalizeKey(select.value);
        if (effectiveKey === ALL) {
          url.searchParams.delete('campaign');
        } else {
          url.searchParams.set('campaign', effectiveKey);
        }

        url.searchParams.delete('page');
        window.history.replaceState({}, '', url.pathname + url.search);
        applyFilter(select.value);
        return;
      }

      setCampaignKey(select.value);
    });
    applyFilter(select.value);
  }

  document.addEventListener('DOMContentLoaded', init);
})();

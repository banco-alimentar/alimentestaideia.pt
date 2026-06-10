// -----------------------------------------------------------------------
// <copyright file="DonationReportHtmlGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport;

    /// <summary>
    /// Generates static HTML pages for donation analytics.
    /// </summary>
    public static class DonationReportHtmlGenerator
    {
        private const string BrandColor = "#0068C3";
        private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static string embeddedFilterJson = "{}";

        /// <summary>
        /// Builds all report pages from a snapshot.
        /// </summary>
        /// <param name="snapshot">Analytics snapshot.</param>
        /// <param name="siteTitle">Public site title.</param>
        /// <returns>Relative path to HTML content map.</returns>
        public static IReadOnlyDictionary<string, string> GenerateAllPages(DonationReportSnapshot snapshot, string siteTitle)
        {
            embeddedFilterJson = SerializeFilterPayload(snapshot.Filters);

            Dictionary<string, string> pages = new Dictionary<string, string>
            {
                ["styles.css"] = BuildStylesheet(),
                ["report-data.json"] = embeddedFilterJson,
                ["report-filters.js"] = BuildReportFiltersScript(),
                ["index.html"] = BuildIndexPage(snapshot, siteTitle),
                ["campaigns.html"] = BuildCampaignsPage(snapshot, siteTitle),
                ["campaign-evolution.html"] = BuildCampaignEvolutionPage(snapshot, siteTitle),
                ["food-banks.html"] = BuildFoodBanksPage(snapshot, siteTitle),
                ["products.html"] = BuildProductsPage(snapshot, siteTitle),
                ["payments.html"] = BuildPaymentsPage(snapshot, siteTitle),
                ["timing.html"] = BuildTimingPage(snapshot, siteTitle),
                ["cross-analysis.html"] = BuildCrossAnalysisPage(snapshot, siteTitle),
                ["subscriptions.html"] = BuildSubscriptionsPage(snapshot, siteTitle),
                ["subscription-list.html"] = BuildSubscriptionListPage(snapshot, siteTitle),
            };

            return pages;
        }

        private static string SerializeFilterPayload(DonationReportFilterPayload filters)
        {
            if (filters == null)
            {
                return "{}";
            }

            return JsonSerializer.Serialize(filters, JsonOptions);
        }

        private static string BuildIndexPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            DonationReportSummary s = snapshot.Summary;
            string dailyLabels = JsonSerializer.Serialize(snapshot.DailyTrend.Select(d => d.Date.ToString("dd/MM", PtCulture)), JsonOptions);
            string dailyAmounts = JsonSerializer.Serialize(snapshot.DailyTrend.Select(d => Math.Round(d.PaidAmount, 2)), JsonOptions);
            string statusLabels = JsonSerializer.Serialize(snapshot.PaymentStatuses.Select(p => p.StatusLabel), JsonOptions);
            string statusCounts = JsonSerializer.Serialize(snapshot.PaymentStatuses.Select(p => p.Count), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"hero\">");
            body.AppendLine($"<h1 id=\"reportTitle\">Desempenho da campanha</h1>");
            body.AppendLine($"<p class=\"subtitle\" id=\"reportSubtitle\">{WebUtility.HtmlEncode(snapshot.CampaignLabel)}</p>");
            body.AppendLine($"<p class=\"meta\" id=\"reportMeta\">Período: {FormatDate(snapshot.PeriodStart)} – {FormatDate(snapshot.PeriodEnd)} · Atualizado {FormatDateTime(snapshot.GeneratedAtUtc)} UTC</p>");
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"kpi-grid\" id=\"kpiGrid\">");
            body.Append(KpiCard("Total angariado (pago)", FormatCurrency(s.TotalPaidAmount), "Receita confirmada no período"));
            body.Append(KpiCard("Doações pagas", s.PaidDonationCount.ToString(PtCulture), $"Ticket médio {FormatCurrency(s.AveragePaidAmount)}"));
            body.Append(KpiCard("Taxa de conversão", FormatPercent(s.PaymentConversionPercent), $"{s.PendingDonationCount} pendentes · {s.FailedDonationCount} com erro"));
            body.Append(KpiCard("Unidades de produto", s.TotalProductUnits.ToString("N0", PtCulture), $"Valor catálogo {FormatCurrency(s.TotalProductValue)}"));
            body.Append(KpiCard("Bancos alimentares", s.ActiveFoodBankCount.ToString(PtCulture), "Com doações confirmadas"));
            body.Append(KpiCard("Doações em numerário", FormatPercent(s.CashDonationSharePercent), "Parte das doações pagas"));
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"chart-row\">");
            body.AppendLine("<div class=\"card chart-card\"><h2>Evolução diária (€ pagos)</h2><canvas id=\"dailyChart\"></canvas></div>");
            body.AppendLine("<div class=\"card chart-card\"><h2>Funil de pagamento</h2><canvas id=\"statusChart\"></canvas></div>");
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"card\">");
            body.AppendLine("<h2>Destaques para gestão</h2><ul class=\"insights\">");
            body.AppendLine(BuildInsightItems(snapshot));
            body.AppendLine("</ul></section>");

            string script = $@"
<script>
const dailyChart = new Chart(document.getElementById('dailyChart'), {{
  type: 'line',
  data: {{ labels: {dailyLabels}, datasets: [{{ label: '€ pagos', data: {dailyAmounts}, borderColor: '{BrandColor}', backgroundColor: 'rgba(0,104,195,0.12)', fill: true, tension: 0.25 }}] }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
const statusChart = new Chart(document.getElementById('statusChart'), {{
  type: 'doughnut',
  data: {{ labels: {statusLabels}, datasets: [{{ data: {statusCounts}, backgroundColor: ['#2e7d32','#fbc02d','{BrandColor}','#616161'] }}] }},
  options: {{ responsive: true }}
}});
</script>";

            return WrapPage(siteTitle, "index.html", "Painel executivo", body.ToString(), script, snapshot.GeneratedAtUtc);
        }

        private static string BuildCampaignsPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            string labels = JsonSerializer.Serialize(snapshot.Campaigns.Select(c => c.CampaignName), JsonOptions);
            string amounts = JsonSerializer.Serialize(snapshot.Campaigns.Select(c => Math.Round(c.PaidAmount, 2)), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Por campanha</h1><p>Análise histórica de todas as campanhas registadas.</p></section>");
            body.AppendLine("<section class=\"card chart-card\"><canvas id=\"campaignChart\"></canvas></section>");
            body.AppendLine("<section class=\"card\"><table><thead><tr><th>Campanha</th><th>Pago (€)</th><th>Doações</th><th>Pendentes</th><th>Ticket médio</th><th>Conversão</th></tr></thead><tbody id=\"campaignTableBody\">");
            foreach (DonationReportCampaignRow row in snapshot.Campaigns)
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.CampaignName)}</td><td>{FormatCurrency(row.PaidAmount)}</td><td>{row.PaidCount}</td><td>{row.PendingCount}</td><td>{FormatCurrency(row.AveragePaidAmount)}</td><td>{FormatPercent(row.ConversionPercent)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('campaignChart'), {{
  type: 'bar',
  data: {{ labels: {labels}, datasets: [{{ label: 'Total pago (€)', data: {amounts}, backgroundColor: '{BrandColor}' }}] }},
  options: {{ indexAxis: 'y', responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
</script>";

            return WrapPage(siteTitle, "campaigns.html", "Campanhas", body.ToString(), script, snapshot.GeneratedAtUtc);
        }

        private static string BuildCampaignEvolutionPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            DonationReportCampaignComparison comparison = snapshot.Filters?.Comparison ?? new DonationReportCampaignComparison();
            string campaignLabels = JsonSerializer.Serialize(comparison.CampaignLabels, JsonOptions);
            string campaignTotals = JsonSerializer.Serialize(comparison.CampaignTotals.Select(v => Math.Round(v, 2)), JsonOptions);
            string campaignAverages = JsonSerializer.Serialize(comparison.CampaignAverageDonations.Select(v => Math.Round(v, 2)), JsonOptions);
            string campaignMedians = JsonSerializer.Serialize(comparison.CampaignMedianDonations.Select(v => Math.Round(v, 2)), JsonOptions);
            string campaignMaximums = JsonSerializer.Serialize(comparison.CampaignMaxDonations.Select(v => Math.Round(v, 2)), JsonOptions);
            string campaignMinimums = JsonSerializer.Serialize(comparison.CampaignMinDonations.Select(v => Math.Round(v, 2)), JsonOptions);

            var topBanks = comparison.FoodBankAmountSeries.Take(8).ToList();
            string bankDatasets = BuildEvolutionDatasetsJson(topBanks, comparison.CampaignLabels);

            var topProducts = comparison.ProductUnitSeries.Take(8).ToList();
            string productDatasets = BuildEvolutionDatasetsJson(topProducts, comparison.CampaignLabels);

            string campaignSubscriptionCounts = JsonSerializer.Serialize(comparison.CampaignSubscriptionCounts, JsonOptions);
            string subscriptionCountByStatusDatasets = BuildSubscriptionCountByStatusDatasetsJson(comparison.SubscriptionCountByStatusSeries);
            string campaignDonationCounts = JsonSerializer.Serialize(comparison.CampaignDonationCounts, JsonOptions);
            string donationCountByStatusDatasets = BuildDonationCountByStatusDatasetsJson(comparison.DonationCountByStatusSeries);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Evolução entre campanhas</h1>");
            body.AppendLine("<p>Comparação histórica do total angariado, subscrições, doações, bancos alimentares e produtos ao longo das campanhas.</p></section>");
            body.AppendLine("<section class=\"card chart-card\"><h2>Número de subscrições por campanha (total e por estado)</h2><p>Subscrições atribuídas à campanha da doação inicial, com repartição por estado.</p><canvas id=\"campaignSubscriptionCountChart\"></canvas></section>");
            body.AppendLine("<section class=\"card chart-card\"><h2>Número de doações por campanha (total e por estado)</h2><p>Total de doações e repartição por estado de pagamento em cada campanha.</p><canvas id=\"campaignDonationCountChart\"></canvas></section>");
            body.AppendLine("<section class=\"card chart-card\"><h2>Total angariado por campanha (€)</h2><canvas id=\"campaignTotalsChart\"></canvas></section>");
            body.AppendLine("<section class=\"card chart-card\"><h2>Valor da doação por campanha (€)</h2><p>Média, mediana e mínimo entre doações pagas.</p><canvas id=\"donationStatsChart\"></canvas></section>");
            body.AppendLine("<section class=\"card chart-card\"><h2>Máximo por campanha (€)</h2><p>Maior doação paga em cada campanha.</p><canvas id=\"donationMaxChart\"></canvas></section>");
            body.AppendLine("<section class=\"card\"><h2>Detalhe por campanha (€ por doação paga)</h2><table><thead><tr><th>Campanha</th><th>Média</th><th>Mediana</th><th>Máximo</th><th>Mínimo</th></tr></thead><tbody>");
            for (int i = 0; i < comparison.CampaignLabels.Count; i++)
            {
                string label = comparison.CampaignLabels[i];
                double average = i < comparison.CampaignAverageDonations.Count ? comparison.CampaignAverageDonations[i] : 0;
                double median = i < comparison.CampaignMedianDonations.Count ? comparison.CampaignMedianDonations[i] : 0;
                double maximum = i < comparison.CampaignMaxDonations.Count ? comparison.CampaignMaxDonations[i] : 0;
                double minimum = i < comparison.CampaignMinDonations.Count ? comparison.CampaignMinDonations[i] : 0;
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(label)}</td><td>{FormatCurrency(average)}</td><td>{FormatCurrency(median)}</td><td>{FormatCurrency(maximum)}</td><td>{FormatCurrency(minimum)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");
            body.AppendLine("<section class=\"card chart-card\"><h2>Top bancos alimentares — evolução (€ pagos)</h2><canvas id=\"bankEvolutionChart\"></canvas></section>");
            body.AppendLine("<section class=\"card chart-card\"><h2>Top produtos — evolução (unidades)</h2><canvas id=\"productEvolutionChart\"></canvas></section>");

            string script = $@"
<script>
const campaignLabels = {campaignLabels};
new Chart(document.getElementById('campaignSubscriptionCountChart'), {{
  type: 'bar',
  data: {{
    labels: campaignLabels,
    datasets: {subscriptionCountByStatusDatasets}.concat([{{
      type: 'line',
      label: 'Total',
      data: {campaignSubscriptionCounts},
      borderColor: '#002B51',
      backgroundColor: '#002B51',
      tension: 0.2,
      order: 0
    }}])
  }},
  options: {{
    responsive: true,
    interaction: {{ mode: 'index', intersect: false }},
    scales: {{
      x: {{ stacked: true }},
      y: {{ stacked: true, beginAtZero: true, ticks: {{ precision: 0 }} }}
    }}
  }}
}});
new Chart(document.getElementById('campaignDonationCountChart'), {{
  type: 'bar',
  data: {{
    labels: campaignLabels,
    datasets: {donationCountByStatusDatasets}.concat([{{
      type: 'line',
      label: 'Total',
      data: {campaignDonationCounts},
      borderColor: '#002B51',
      backgroundColor: '#002B51',
      tension: 0.2,
      order: 0
    }}])
  }},
  options: {{
    responsive: true,
    interaction: {{ mode: 'index', intersect: false }},
    scales: {{
      x: {{ stacked: true }},
      y: {{ stacked: true, beginAtZero: true, ticks: {{ precision: 0 }} }}
    }}
  }}
}});
new Chart(document.getElementById('campaignTotalsChart'), {{
  type: 'bar',
  data: {{ labels: campaignLabels, datasets: [{{ label: 'Total pago (€)', data: {campaignTotals}, backgroundColor: '{BrandColor}' }}] }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
new Chart(document.getElementById('donationStatsChart'), {{
  type: 'line',
  data: {{
    labels: campaignLabels,
    datasets: [
      {{ label: 'Média', data: {campaignAverages}, borderColor: '{BrandColor}', backgroundColor: '{BrandColor}', tension: 0.2 }},
      {{ label: 'Mediana', data: {campaignMedians}, borderColor: '#002B51', backgroundColor: '#002B51', tension: 0.2 }},
      {{ label: 'Mínimo', data: {campaignMinimums}, borderColor: '#ef6c00', backgroundColor: '#ef6c00', tension: 0.2 }}
    ]
  }},
  options: {{ responsive: true, interaction: {{ mode: 'index', intersect: false }} }}
}});
new Chart(document.getElementById('donationMaxChart'), {{
  type: 'line',
  data: {{
    labels: campaignLabels,
    datasets: [
      {{ label: 'Máximo', data: {campaignMaximums}, borderColor: '#2e7d32', backgroundColor: '#2e7d32', tension: 0.2 }}
    ]
  }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
new Chart(document.getElementById('bankEvolutionChart'), {{
  type: 'line',
  data: {{ labels: campaignLabels, datasets: {bankDatasets} }},
  options: {{ responsive: true, interaction: {{ mode: 'index', intersect: false }} }}
}});
new Chart(document.getElementById('productEvolutionChart'), {{
  type: 'line',
  data: {{ labels: campaignLabels, datasets: {productDatasets} }},
  options: {{ responsive: true, interaction: {{ mode: 'index', intersect: false }} }}
}});
</script>";

            return WrapPage(siteTitle, "campaign-evolution.html", "Evolução", body.ToString(), script, snapshot.GeneratedAtUtc, showCampaignFilter: false);
        }

        private static string BuildSubscriptionCountByStatusDatasetsJson(IList<DonationReportSeriesRow> series)
        {
            if (series == null || series.Count == 0)
            {
                return "[]";
            }

            Dictionary<string, string> colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ativa"] = "#2e7d32",
                ["Captura"] = "#00838f",
                ["Criada"] = BrandColor,
                ["Inativa"] = "#616161",
                ["Erro"] = "#c62828",
            };

            List<object> datasets = new List<object>();
            foreach (DonationReportSeriesRow row in series)
            {
                string color = colors.TryGetValue(row.Label ?? string.Empty, out string mappedColor)
                    ? mappedColor
                    : BrandColor;
                datasets.Add(new
                {
                    label = row.Label,
                    data = row.Values,
                    backgroundColor = color,
                    borderColor = color,
                    stack = "subscriptions",
                    order = 1,
                });
            }

            return JsonSerializer.Serialize(datasets, JsonOptions);
        }

        private static string BuildDonationCountByStatusDatasetsJson(IList<DonationReportSeriesRow> series)
        {
            if (series == null || series.Count == 0)
            {
                return "[]";
            }

            Dictionary<string, string> colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Pago"] = "#2e7d32",
                ["Não pago"] = "#ef6c00",
                ["A aguardar pagamento"] = "#00838f",
                ["Erro de pagamento"] = "#c62828",
            };

            List<object> datasets = new List<object>();
            foreach (DonationReportSeriesRow row in series)
            {
                string color = colors.TryGetValue(row.Label ?? string.Empty, out string mappedColor)
                    ? mappedColor
                    : BrandColor;
                datasets.Add(new
                {
                    label = row.Label,
                    data = row.Values,
                    backgroundColor = color,
                    borderColor = color,
                    stack = "donations",
                    order = 1,
                });
            }

            return JsonSerializer.Serialize(datasets, JsonOptions);
        }

        private static string BuildEvolutionDatasetsJson(IList<DonationReportSeriesRow> series, IList<string> campaignLabels)
        {
            if (series == null || series.Count == 0 || campaignLabels == null || campaignLabels.Count == 0)
            {
                return "[]";
            }

            string[] palette = new[] { BrandColor, "#ef6c00", "#2e7d32", "#6a1b9a", "#00838f", "#002B51", "#fbc02d", "#616161" };
            List<object> datasets = new List<object>();
            for (int i = 0; i < series.Count; i++)
            {
                DonationReportSeriesRow row = series[i];
                datasets.Add(new
                {
                    label = row.Label,
                    data = row.Values,
                    borderColor = palette[i % palette.Length],
                    backgroundColor = palette[i % palette.Length],
                    tension = 0.2,
                    fill = false,
                });
            }

            return JsonSerializer.Serialize(datasets, JsonOptions);
        }

        private static string BuildFoodBanksPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            var top = snapshot.FoodBanks.Take(15).ToList();
            string labels = JsonSerializer.Serialize(top.Select(f => f.FoodBankName), JsonOptions);
            string amounts = JsonSerializer.Serialize(top.Select(f => Math.Round(f.PaidAmount, 2)), JsonOptions);
            string shares = JsonSerializer.Serialize(top.Select(f => Math.Round(f.SharePercent, 1)), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Por banco alimentar</h1><p id=\"foodBankIntro\">Distribuição geográfica das doações confirmadas.</p></section>");
            body.AppendLine("<section class=\"chart-row\"><div class=\"card chart-card\"><h2>Volume pago (€)</h2><canvas id=\"fbAmountChart\"></canvas></div>");
            body.AppendLine("<div class=\"card chart-card\"><h2>Quota (%)</h2><canvas id=\"fbShareChart\"></canvas></div></section>");
            body.AppendLine("<section class=\"card\"><table><thead><tr><th>Banco alimentar</th><th>Pago (€)</th><th>Doações</th><th>Unidades</th><th>Quota</th></tr></thead><tbody id=\"foodBankTableBody\">");
            foreach (DonationReportFoodBankRow row in snapshot.FoodBanks)
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.FoodBankName)}</td><td>{FormatCurrency(row.PaidAmount)}</td><td>{row.PaidCount}</td><td>{row.ProductUnits:N0}</td><td>{FormatPercent(row.SharePercent)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('fbAmountChart'), {{ type: 'bar', data: {{ labels: {labels}, datasets: [{{ data: {amounts}, backgroundColor: '{BrandColor}' }}] }}, options: {{ indexAxis: 'y', plugins: {{ legend: {{ display: false }} }} }} }});
new Chart(document.getElementById('fbShareChart'), {{ type: 'pie', data: {{ labels: {labels}, datasets: [{{ data: {shares} }}] }}, options: {{ responsive: true }} }});
</script>";

            return WrapPage(siteTitle, "food-banks.html", "Bancos alimentares", body.ToString(), script, snapshot.GeneratedAtUtc);
        }

        private static string BuildProductsPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            var top = snapshot.Products.Take(20).ToList();
            string labels = JsonSerializer.Serialize(top.Select(p => p.ProductName), JsonOptions);
            string quantities = JsonSerializer.Serialize(top.Select(p => p.Quantity), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Por produto doado</h1><p id=\"productIntro\">Unidades e valor de catálogo das doações pagas.</p></section>");
            body.AppendLine("<section class=\"card chart-card\"><canvas id=\"productChart\"></canvas></section>");
            body.AppendLine("<section class=\"card\"><table><thead><tr><th>Produto</th><th>Unidade</th><th>Quantidade</th><th>Valor (€)</th><th>Quota</th></tr></thead><tbody id=\"productTableBody\">");
            foreach (DonationReportProductRow row in snapshot.Products)
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.ProductName)}</td><td>{WebUtility.HtmlEncode(row.UnitOfMeasure)}</td><td>{row.Quantity:N0}</td><td>{FormatCurrency(row.Value)}</td><td>{FormatPercent(row.SharePercent)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('productChart'), {{
  type: 'bar',
  data: {{ labels: {labels}, datasets: [{{ label: 'Unidades', data: {quantities}, backgroundColor: '#ef6c00' }}] }},
  options: {{ indexAxis: 'y', plugins: {{ legend: {{ display: false }} }} }}
}});
</script>";

            return WrapPage(siteTitle, "products.html", "Produtos", body.ToString(), script, snapshot.GeneratedAtUtc);
        }

        private static string BuildPaymentsPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            DonationReportSummary summary = snapshot.Summary;
            DonationReportCampaignComparison comparison = snapshot.Filters?.Comparison ?? new DonationReportCampaignComparison();
            string labels = JsonSerializer.Serialize(snapshot.Payments.Select(p => p.PaymentTypeLabel), JsonOptions);
            string amounts = JsonSerializer.Serialize(snapshot.Payments.Select(p => Math.Round(p.PaidAmount, 2)), JsonOptions);
            string avgTickets = JsonSerializer.Serialize(snapshot.Payments.Select(p => Math.Round(p.AveragePaidAmount, 2)), JsonOptions);
            string maxTickets = JsonSerializer.Serialize(snapshot.Payments.Select(p => Math.Round(p.MaxPaidAmount, 2)), JsonOptions);
            string campaignLabels = JsonSerializer.Serialize(comparison.CampaignLabels, JsonOptions);
            string campaignAverages = JsonSerializer.Serialize(comparison.CampaignAverageDonations.Select(v => Math.Round(v, 2)), JsonOptions);
            string campaignMaximums = JsonSerializer.Serialize(comparison.CampaignMaxDonations.Select(v => Math.Round(v, 2)), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Por método de pagamento</h1><p>Mix de receita, ticket médio e maior doação por método e campanha.</p></section>");
            body.AppendLine("<section class=\"kpi-grid\" id=\"paymentKpiGrid\">");
            body.Append(KpiCard("Maior doação (global)", FormatCurrency(summary.MaxSingleDonation), "Valor máximo entre doações pagas"));
            body.AppendLine("</section>");
            body.AppendLine("<section class=\"card chart-card\"><h2>Receita por método</h2><canvas id=\"payAmountChart\"></canvas></section>");
            body.AppendLine("<section class=\"chart-row\"><div class=\"card chart-card\"><h2>Ticket médio</h2><canvas id=\"payAvgChart\"></canvas></div>");
            body.AppendLine("<div class=\"card chart-card\"><h2>Máximo por método</h2><canvas id=\"payMaxChart\"></canvas></div></section>");
            body.AppendLine("<section class=\"chart-row\"><div class=\"card chart-card\"><h2>Média por campanha</h2><p>Média entre doações pagas.</p><canvas id=\"payCampaignAvgChart\"></canvas></div>");
            body.AppendLine("<div class=\"card chart-card\"><h2>Máximo por campanha</h2><p>Maior doação paga.</p><canvas id=\"payCampaignMaxChart\"></canvas></div></section>");
            body.AppendLine("<section class=\"card\"><table><thead><tr><th>Método</th><th>Pago (€)</th><th>Doações</th><th>Ticket médio</th><th>Máximo (€)</th><th>Quota</th></tr></thead><tbody id=\"paymentTableBody\">");
            foreach (DonationReportPaymentRow row in snapshot.Payments)
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.PaymentTypeLabel)}</td><td>{FormatCurrency(row.PaidAmount)}</td><td>{row.PaidCount}</td><td>{FormatCurrency(row.AveragePaidAmount)}</td><td>{FormatCurrency(row.MaxPaidAmount)}</td><td>{FormatPercent(row.SharePercent)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('payAmountChart'), {{ type: 'doughnut', data: {{ labels: {labels}, datasets: [{{ data: {amounts} }}] }}, options: {{ responsive: true }} }});
new Chart(document.getElementById('payAvgChart'), {{ type: 'bar', data: {{ labels: {labels}, datasets: [{{ label: 'Ticket médio (€)', data: {avgTickets}, backgroundColor: '{BrandColor}' }}] }}, options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }} }});
new Chart(document.getElementById('payMaxChart'), {{ type: 'bar', data: {{ labels: {labels}, datasets: [{{ label: 'Máximo (€)', data: {maxTickets}, backgroundColor: '#002B51' }}] }}, options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }} }});
new Chart(document.getElementById('payCampaignAvgChart'), {{ type: 'bar', data: {{ labels: {campaignLabels}, datasets: [{{ label: 'Média (€)', data: {campaignAverages}, backgroundColor: '{BrandColor}' }}] }}, options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }} }});
new Chart(document.getElementById('payCampaignMaxChart'), {{ type: 'bar', data: {{ labels: {campaignLabels}, datasets: [{{ label: 'Máximo (€)', data: {campaignMaximums}, backgroundColor: '#002B51' }}] }}, options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }} }});
</script>";

            return WrapPage(siteTitle, "payments.html", "Pagamentos", body.ToString(), script, snapshot.GeneratedAtUtc);
        }

        private static string BuildTimingPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            DonationReportTemporalAnalysis temporal = snapshot.TemporalAnalysis ?? new DonationReportTemporalAnalysis();
            string hourLabels = JsonSerializer.Serialize(temporal.HourlyDistribution.Select(h => h.HourLabel), JsonOptions);
            string hourCounts = JsonSerializer.Serialize(temporal.HourlyDistribution.Select(h => h.PaidCount), JsonOptions);
            string hourAmounts = JsonSerializer.Serialize(temporal.HourlyDistribution.Select(h => Math.Round(h.PaidAmount, 2)), JsonOptions);
            string weekdayLabels = JsonSerializer.Serialize(temporal.WeekdayDistribution.Select(d => d.DayLabel), JsonOptions);
            string weekdayCounts = JsonSerializer.Serialize(temporal.WeekdayDistribution.Select(d => d.PaidCount), JsonOptions);
            string weekdayAmounts = JsonSerializer.Serialize(temporal.WeekdayDistribution.Select(d => Math.Round(d.PaidAmount, 2)), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Análise temporal</h1>");
            body.AppendLine("<p>Distribuição das doações pagas por hora do dia e por dia da semana (hora local de registo).</p></section>");

            body.AppendLine("<section class=\"kpi-grid\" id=\"timingKpiGrid\">");
            body.Append(KpiCard("Hora com mais doações", temporal.PeakHourLabel, $"{temporal.PeakHourCount:N0} doações pagas"));
            body.Append(KpiCard("Dia com mais doações", temporal.PeakDayLabel, $"{temporal.PeakDayCount:N0} doações pagas"));
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"chart-row\">");
            body.AppendLine("<div class=\"card chart-card\"><h2>Doações por hora do dia</h2><canvas id=\"hourCountChart\"></canvas></div>");
            body.AppendLine("<div class=\"card chart-card\"><h2>Volume pago por hora (€)</h2><canvas id=\"hourAmountChart\"></canvas></div>");
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"chart-row\">");
            body.AppendLine("<div class=\"card chart-card\"><h2>Doações por dia da semana</h2><canvas id=\"weekdayCountChart\"></canvas></div>");
            body.AppendLine("<div class=\"card chart-card\"><h2>Volume pago por dia da semana (€)</h2><canvas id=\"weekdayAmountChart\"></canvas></div>");
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"card\"><h2>Resumo</h2><ul class=\"insights\">");
            body.AppendLine($"<li><strong>Pico horário:</strong> {WebUtility.HtmlEncode(temporal.PeakHourLabel)} concentra {temporal.PeakHourCount:N0} doações pagas.</li>");
            body.AppendLine($"<li><strong>Pico semanal:</strong> {WebUtility.HtmlEncode(temporal.PeakDayLabel)} é o dia com mais doações ({temporal.PeakDayCount:N0}).</li>");
            body.AppendLine("</ul></section>");

            body.AppendLine("<section class=\"card\"><h2>Detalhe por hora</h2><table><thead><tr><th>Hora</th><th>Doações</th><th>Volume (€)</th></tr></thead><tbody>");
            foreach (DonationReportHourlyPoint row in temporal.HourlyDistribution.Where(h => h.PaidCount > 0).OrderByDescending(h => h.PaidCount))
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.HourLabel)}</td><td>{row.PaidCount:N0}</td><td>{FormatCurrency(row.PaidAmount)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            body.AppendLine("<section class=\"card\"><h2>Detalhe por dia da semana</h2><table><thead><tr><th>Dia</th><th>Doações</th><th>Volume (€)</th></tr></thead><tbody>");
            foreach (DonationReportWeekdayPoint row in temporal.WeekdayDistribution)
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.DayLabel)}</td><td>{row.PaidCount:N0}</td><td>{FormatCurrency(row.PaidAmount)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('hourCountChart'), {{
  type: 'bar',
  data: {{ labels: {hourLabels}, datasets: [{{ label: 'Doações', data: {hourCounts}, backgroundColor: '{BrandColor}' }}] }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
new Chart(document.getElementById('hourAmountChart'), {{
  type: 'line',
  data: {{ labels: {hourLabels}, datasets: [{{ label: '€ pagos', data: {hourAmounts}, borderColor: '{BrandColor}', backgroundColor: 'rgba(0,104,195,0.12)', fill: true, tension: 0.25 }}] }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
new Chart(document.getElementById('weekdayCountChart'), {{
  type: 'bar',
  data: {{ labels: {weekdayLabels}, datasets: [{{ label: 'Doações', data: {weekdayCounts}, backgroundColor: '{BrandColor}' }}] }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
new Chart(document.getElementById('weekdayAmountChart'), {{
  type: 'bar',
  data: {{ labels: {weekdayLabels}, datasets: [{{ label: '€ pagos', data: {weekdayAmounts}, backgroundColor: '#002B51' }}] }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
</script>";

            return WrapPage(siteTitle, "timing.html", "Horários", body.ToString(), script, snapshot.GeneratedAtUtc);
        }

        private static string BuildCrossAnalysisPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            var topCross = snapshot.FoodBankByProduct.Take(15).ToList();
            string crossLabels = JsonSerializer.Serialize(topCross.Select(c => $"{c.DimensionA} · {c.DimensionB}"), JsonOptions);
            string crossCounts = JsonSerializer.Serialize(topCross.Select(c => c.Count), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Análise cruzada</h1><p>Combinações entre dimensões para identificar padrões operacionais.</p></section>");
            body.AppendLine("<section class=\"card chart-card\"><h2>Top combinações banco × produto (unidades)</h2><canvas id=\"crossProductChart\"></canvas></section>");

            body.AppendLine("<section class=\"card\"><h2>Campanha × método de pagamento</h2><table><thead><tr><th>Campanha</th><th>Pagamento</th><th>Valor (€)</th><th>Doações</th></tr></thead><tbody>");
            foreach (DonationReportCrossRow row in snapshot.CampaignByPayment.Take(30))
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.DimensionA)}</td><td>{WebUtility.HtmlEncode(row.DimensionB)}</td><td>{FormatCurrency(row.Amount)}</td><td>{row.Count}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            body.AppendLine("<section class=\"card\"><h2>Banco alimentar × método de pagamento</h2><table><thead><tr><th>Banco alimentar</th><th>Pagamento</th><th>Valor (€)</th><th>Doações</th></tr></thead><tbody>");
            foreach (DonationReportCrossRow row in snapshot.FoodBankByPayment.Take(30))
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.DimensionA)}</td><td>{WebUtility.HtmlEncode(row.DimensionB)}</td><td>{FormatCurrency(row.Amount)}</td><td>{row.Count}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('crossProductChart'), {{
  type: 'bar',
  data: {{ labels: {crossLabels}, datasets: [{{ label: 'Unidades', data: {crossCounts}, backgroundColor: '#00838f' }}] }},
  options: {{ indexAxis: 'y', plugins: {{ legend: {{ display: false }} }} }}
}});
</script>";

            return WrapPage(siteTitle, "cross-analysis.html", "Análise cruzada", body.ToString(), script, snapshot.GeneratedAtUtc);
        }

        private static string BuildSubscriptionsPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            DonationReportSubscriptionSection subscriptions = snapshot.Subscriptions ?? new DonationReportSubscriptionSection();
            string statusLabels = JsonSerializer.Serialize(
                subscriptions.StatusBreakdown.Select(row => row.StatusLabel),
                JsonOptions);
            string statusCounts = JsonSerializer.Serialize(
                subscriptions.StatusBreakdown.Select(row => row.Count),
                JsonOptions);
            string frequencyLabels = JsonSerializer.Serialize(
                subscriptions.FrequencyBreakdown.Select(row => row.FrequencyLabel),
                JsonOptions);
            string frequencySubscriptionCounts = JsonSerializer.Serialize(
                subscriptions.FrequencyBreakdown.Select(row => row.SubscriptionCount),
                JsonOptions);
            string frequencyPaidAmounts = JsonSerializer.Serialize(
                subscriptions.FrequencyBreakdown.Select(row => Math.Round(row.TotalPaidAmount, 2)),
                JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Subscrições</h1><p id=\"subscriptionIntro\">Doações recorrentes e respetivo desempenho.</p></section>");

            body.AppendLine("<section class=\"kpi-grid\" id=\"subscriptionKpiGrid\">");
            body.Append(KpiCard(
                "Total via subscrições (pago)",
                FormatCurrency(subscriptions.TotalPaidAmount),
                "Apenas doações com pagamento confirmado"));
            body.Append(KpiCard(
                "Subscrições",
                subscriptions.SubscriptionCount.ToString(PtCulture),
                "Subscrições com doações associadas"));
            body.Append(KpiCard(
                "Doações pagas",
                subscriptions.PaidDonationCount.ToString(PtCulture),
                "Doações de subscrição confirmadas"));
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"chart-row\">");
            body.AppendLine("<div class=\"card chart-card\"><h2>Subscrições por estado</h2><canvas id=\"subscriptionStatusChart\"></canvas></div>");
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"card\"><h2>Subscrições por estado</h2><table><thead><tr><th>Estado</th><th>Subscrições</th><th>Partilha</th></tr></thead><tbody id=\"subscriptionStatusTableBody\">");
            foreach (DonationReportSubscriptionStatusRow row in subscriptions.StatusBreakdown)
            {
                body.AppendLine(
                    $"<tr><td>{WebUtility.HtmlEncode(row.StatusLabel)}</td><td>{row.Count}</td><td>{FormatPercent(row.SharePercent)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            body.AppendLine("<section class=\"chart-row\">");
            body.AppendLine("<div class=\"card chart-card\"><h2>Subscrições por frequência</h2><canvas id=\"subscriptionFrequencyCountChart\"></canvas></div>");
            body.AppendLine("<div class=\"card chart-card\"><h2>Total pago por frequência (€)</h2><canvas id=\"subscriptionFrequencyAmountChart\"></canvas></div>");
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"card\"><h2>Por frequência</h2>");
            if (subscriptions.ForecastPeriodEnd.HasValue)
            {
                body.AppendLine(
                    $"<p class=\"meta\" id=\"subscriptionForecastPeriod\">Previsão para subscrições ativas até {FormatDate(subscriptions.ForecastPeriodEnd.Value)} (desde {FormatDate(subscriptions.ForecastPeriodStart ?? snapshot.GeneratedAtUtc)}).</p>");
            }
            else
            {
                body.AppendLine("<p class=\"meta\" id=\"subscriptionForecastPeriod\"></p>");
            }

            body.AppendLine("<table><thead><tr><th>Frequência</th><th>Subscrições</th><th>Partilha</th><th>Total pago (€)</th><th>Média doação (€)</th><th>Previsto período (€)</th></tr></thead><tbody id=\"subscriptionFrequencyTableBody\">");
            foreach (DonationReportSubscriptionFrequencyRow row in subscriptions.FrequencyBreakdown)
            {
                body.AppendLine(
                    $"<tr><td>{WebUtility.HtmlEncode(row.FrequencyLabel)}</td><td>{row.SubscriptionCount}</td><td>{FormatPercent(row.SubscriptionSharePercent)}</td><td>{FormatCurrency(row.TotalPaidAmount)}</td><td>{FormatCurrency(row.AverageDonationAmount)}</td><td>{FormatCurrency(row.ExpectedUpcomingAmount)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('subscriptionStatusChart'), {{
  type: 'doughnut',
  data: {{ labels: {statusLabels}, datasets: [{{ data: {statusCounts}, backgroundColor: ['#2e7d32','#00838f','{BrandColor}','#616161','#c62828'] }}] }},
  options: {{ responsive: true }}
}});
new Chart(document.getElementById('subscriptionFrequencyCountChart'), {{
  type: 'bar',
  data: {{ labels: {frequencyLabels}, datasets: [{{ label: 'Subscrições', data: {frequencySubscriptionCounts}, backgroundColor: '{BrandColor}' }}] }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
new Chart(document.getElementById('subscriptionFrequencyAmountChart'), {{
  type: 'bar',
  data: {{ labels: {frequencyLabels}, datasets: [{{ label: '€ pagos', data: {frequencyPaidAmounts}, backgroundColor: '#002B51' }}] }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
</script>";

            return WrapPage(siteTitle, "subscriptions.html", "Subscrições", body.ToString(), script, snapshot.GeneratedAtUtc);
        }

        private static string BuildSubscriptionListPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\">");
            body.AppendLine("<h1>Lista de subscrições</h1>");
            body.AppendLine("<p id=\"subscriptionListIntro\">Todas as subscrições no âmbito do filtro selecionado.</p>");
            body.AppendLine("<p class=\"meta\" id=\"subscriptionListFilterSummary\"></p>");
            body.AppendLine("<p><a href=\"subscriptions.html\" id=\"subscriptionListBackLink\">← Voltar ao resumo de subscrições</a> · <a href=\"subscription-list.html\" id=\"subscriptionListClearFilters\">Limpar filtros de estado e frequência</a></p>");
            body.AppendLine("</section>");
            body.AppendLine("<section class=\"card\">");
            body.AppendLine("<table><thead><tr><th>Estado</th><th>Frequência</th><th>Criada</th><th>Doações pagas</th><th>Total doado (€)</th></tr></thead><tbody id=\"subscriptionListTableBody\"></tbody></table>");
            body.AppendLine("<nav id=\"subscriptionListPagination\" class=\"pagination\" aria-label=\"Paginação de subscrições\"></nav>");
            body.AppendLine("</section>");

            return WrapPage(siteTitle, "subscription-list.html", "Lista de subscrições", body.ToString(), string.Empty, snapshot.GeneratedAtUtc);
        }

        private static string BuildInsightItems(DonationReportSnapshot snapshot)
        {
            StringBuilder insights = new StringBuilder();
            DonationReportSummary s = snapshot.Summary;

            if (snapshot.Payments.Count > 0)
            {
                DonationReportPaymentRow topPayment = snapshot.Payments[0];
                insights.AppendLine($"<li><strong>Método dominante:</strong> {WebUtility.HtmlEncode(topPayment.PaymentTypeLabel)} representa {FormatPercent(topPayment.SharePercent)} da receita ({FormatCurrency(topPayment.PaidAmount)}).</li>");
            }

            if (snapshot.FoodBanks.Count > 0)
            {
                DonationReportFoodBankRow topBank = snapshot.FoodBanks[0];
                insights.AppendLine($"<li><strong>Maior banco alimentar:</strong> {WebUtility.HtmlEncode(topBank.FoodBankName)} concentra {FormatPercent(topBank.SharePercent)} das doações pagas.</li>");
            }

            if (snapshot.Products.Count > 0)
            {
                DonationReportProductRow topProduct = snapshot.Products[0];
                insights.AppendLine($"<li><strong>Produto mais doado:</strong> {WebUtility.HtmlEncode(topProduct.ProductName)} — {topProduct.Quantity:N0} unidades ({FormatPercent(topProduct.SharePercent)}).</li>");
            }

            insights.AppendLine($"<li><strong>Eficiência de conversão:</strong> {FormatPercent(s.PaymentConversionPercent)} das intenções de doação no período resultaram em pagamento confirmado.</li>");

            if (snapshot.DailyTrend.Count >= 2)
            {
                DonationReportDailyPoint last = snapshot.DailyTrend[^1];
                DonationReportDailyPoint prev = snapshot.DailyTrend[^2];
                double delta = prev.PaidAmount == 0 ? 0 : ((last.PaidAmount - prev.PaidAmount) / prev.PaidAmount) * 100;
                string trend = delta >= 0 ? "aumento" : "redução";
                insights.AppendLine($"<li><strong>Tendência recente:</strong> {trend} de {FormatPercent(Math.Abs(delta))} no volume pago entre {FormatDate(prev.Date)} e {FormatDate(last.Date)}.</li>");
            }

            return insights.ToString();
        }

        private static string WrapPage(string siteTitle, string activePage, string pageTitle, string bodyHtml, string pageScript, DateTime generatedAtUtc, bool showCampaignFilter = true)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"pt\">");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"utf-8\" />");
            html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            html.AppendLine($"<title>{WebUtility.HtmlEncode(pageTitle)} — {WebUtility.HtmlEncode(siteTitle)}</title>");
            html.AppendLine("<link href=\"https://fonts.googleapis.com/css?family=Open+Sans:400,600,700,800\" rel=\"stylesheet\" />");
            html.AppendLine("<link rel=\"stylesheet\" href=\"styles.css\" />");
            html.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js\" crossorigin=\"anonymous\"></script>");
            html.AppendLine("</head>");
            html.AppendLine($"<body data-page=\"{WebUtility.HtmlEncode(activePage)}\">");
            html.AppendLine("<header class=\"site-header\">");
            html.AppendLine($"<div class=\"brand\"><strong>{WebUtility.HtmlEncode(siteTitle)}</strong>");
            html.AppendLine($"<span class=\"generated-at\">Relatório gerado em {FormatDateTime(generatedAtUtc)} UTC</span></div>");
            html.AppendLine("<nav class=\"nav\">");
            html.Append(NavLink("index.html", "Painel", activePage));
            html.Append(NavLink("campaigns.html", "Campanhas", activePage));
            html.Append(NavLink("campaign-evolution.html", "Evolução", activePage));
            html.Append(NavLink("food-banks.html", "Bancos alimentares", activePage));
            html.Append(NavLink("products.html", "Produtos", activePage));
            html.Append(NavLink("payments.html", "Pagamentos", activePage));
            html.Append(NavLink("subscriptions.html", "Subscrições", activePage));
            html.Append(NavLink("subscription-list.html", "Lista subscrições", activePage));
            html.Append(NavLink("timing.html", "Horários", activePage));
            html.Append(NavLink("cross-analysis.html", "Análise cruzada", activePage));
            html.AppendLine("</nav>");
            if (showCampaignFilter)
            {
                html.AppendLine("<div class=\"filter-bar\"><label for=\"campaignFilter\">Filtrar campanha</label>");
                html.AppendLine("<select id=\"campaignFilter\" aria-label=\"Filtrar por campanha\"></select></div>");
            }

            html.AppendLine("</header>");
            html.AppendLine("<main class=\"container\">");
            html.AppendLine(bodyHtml);
            html.AppendLine("</main>");
            html.AppendLine("<footer class=\"site-footer\">");
            html.AppendLine("<p>Federação Portuguesa dos Bancos Alimentares Contra a Fome · Relatório gerado automaticamente</p>");
            html.AppendLine("</footer>");
            html.AppendLine("<script type=\"application/json\" id=\"reportFilterData\">");
            html.AppendLine(EscapeJsonForHtmlScript(embeddedFilterJson));
            html.AppendLine("</script>");
            html.AppendLine(pageScript);
            html.AppendLine("<script src=\"report-filters.js\"></script>");
            html.AppendLine("</body></html>");
            return html.ToString();
        }

        private static string NavLink(string href, string label, string activePage)
        {
            string cssClass = href.Equals(activePage, StringComparison.OrdinalIgnoreCase) ? "active" : string.Empty;
            return $"<a href=\"{href}\" class=\"{cssClass}\">{WebUtility.HtmlEncode(label)}</a>";
        }

        private static string KpiCard(string title, string value, string hint)
        {
            return $"<article class=\"kpi\"><h3>{WebUtility.HtmlEncode(title)}</h3><p class=\"kpi-value\">{WebUtility.HtmlEncode(value)}</p><p class=\"kpi-hint\">{WebUtility.HtmlEncode(hint)}</p></article>";
        }

        private static string FormatCurrency(double value)
        {
            return value.ToString("C2", PtCulture);
        }

        private static string FormatPercent(double value)
        {
            return value.ToString("0.0", PtCulture) + " %";
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToString("dd/MM/yyyy", PtCulture);
        }

        private static string FormatDateTime(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm", PtCulture);
        }

        private static string EscapeJsonForHtmlScript(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return "{}";
            }

            return json.Replace("</", "<\\/", StringComparison.Ordinal);
        }

        private static string BuildStylesheet()
        {
            return @":root {
  --brand: #0068C3;
  --brand-dark: #002B51;
  --bg: #f6f7f9;
  --card: #ffffff;
  --text: #1f2933;
  --muted: #52606d;
  --border: #d9e2ec;
}
* { box-sizing: border-box; }
body { margin: 0; font-family: 'Open Sans', sans-serif; background: var(--bg); color: var(--text); }
.site-header { background: var(--brand); color: #fff; padding: 1rem 1.5rem; }
.brand { font-size: 1.1rem; margin-bottom: 0.75rem; }
.generated-at { display: block; font-size: 0.85rem; font-weight: 400; opacity: 0.9; margin-top: 0.25rem; }
.nav { display: flex; flex-wrap: wrap; gap: 0.5rem; }
.nav a { color: #fff; text-decoration: none; padding: 0.35rem 0.75rem; border-radius: 999px; background: rgba(255,255,255,0.12); font-size: 0.9rem; }
.nav a.active, .nav a:hover { background: #fff; color: var(--brand-dark); }
.container { max-width: 1200px; margin: 0 auto; padding: 1.5rem; }
.hero { margin-bottom: 1rem; }
.hero h1 { margin: 0 0 0.25rem; }
.subtitle, .meta { color: var(--muted); margin: 0.25rem 0; }
.kpi-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 1rem; margin-bottom: 1.5rem; }
.kpi { background: var(--card); border: 1px solid var(--border); border-radius: 12px; padding: 1rem; }
.kpi h3 { margin: 0 0 0.5rem; font-size: 0.85rem; color: var(--muted); font-weight: 600; }
.kpi-value { margin: 0; font-size: 1.5rem; font-weight: 700; color: var(--brand-dark); }
.kpi-hint { margin: 0.35rem 0 0; font-size: 0.8rem; color: var(--muted); }
.card { background: var(--card); border: 1px solid var(--border); border-radius: 12px; padding: 1rem 1.25rem; margin-bottom: 1.25rem; }
.chart-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1rem; margin-bottom: 1.25rem; }
.chart-card canvas { max-height: 320px; }
table { width: 100%; border-collapse: collapse; font-size: 0.92rem; }
th, td { padding: 0.55rem 0.65rem; border-bottom: 1px solid var(--border); text-align: left; }
th { background: #f0f4f8; font-weight: 600; }
.insights { margin: 0; padding-left: 1.2rem; line-height: 1.6; }
.site-footer { text-align: center; color: var(--muted); font-size: 0.85rem; padding: 2rem 1rem; }
.filter-bar { margin-top: 0.75rem; display: flex; flex-wrap: wrap; align-items: center; gap: 0.5rem; }
.filter-bar label { font-size: 0.9rem; }
.filter-bar select { min-width: 220px; padding: 0.35rem 0.5rem; border-radius: 8px; border: none; }
a { color: var(--brand); }
a:hover { color: var(--brand-dark); }
.kpi-value a { color: inherit; text-decoration: none; border-bottom: 1px dotted currentColor; }
.kpi-value a:hover { color: var(--brand); }
.pagination { display: flex; flex-wrap: wrap; gap: 0.35rem; margin-top: 1rem; align-items: center; }
.pagination a, .pagination span { padding: 0.35rem 0.65rem; border: 1px solid var(--border); border-radius: 8px; text-decoration: none; font-size: 0.9rem; background: #fff; color: var(--brand-dark); }
.pagination a:hover { border-color: var(--brand); color: var(--brand); }
.pagination .active { background: var(--brand); border-color: var(--brand); color: #fff; font-weight: 600; }
.pagination .disabled { opacity: 0.45; pointer-events: none; }
.pagination .summary { border: none; background: transparent; color: var(--muted); padding-left: 0; }
@media (max-width: 640px) { .kpi-value { font-size: 1.25rem; } }";
        }

        private static string BuildReportFiltersScript()
        {
            Assembly assembly = typeof(DonationReportHtmlGenerator).Assembly;
            string resourceName = assembly
                .GetManifestResourceNames()
                .First(n => n.EndsWith("report-filters.js", StringComparison.OrdinalIgnoreCase));
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}

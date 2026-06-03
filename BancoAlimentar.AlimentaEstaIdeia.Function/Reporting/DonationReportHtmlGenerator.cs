// -----------------------------------------------------------------------
// <copyright file="DonationReportHtmlGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport;

    /// <summary>
    /// Generates static HTML pages for donation analytics.
    /// </summary>
    public static class DonationReportHtmlGenerator
    {
        private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Builds all report pages from a snapshot.
        /// </summary>
        /// <param name="snapshot">Analytics snapshot.</param>
        /// <param name="siteTitle">Public site title.</param>
        /// <returns>Relative path to HTML content map.</returns>
        public static IReadOnlyDictionary<string, string> GenerateAllPages(DonationReportSnapshot snapshot, string siteTitle)
        {
            Dictionary<string, string> pages = new Dictionary<string, string>
            {
                ["styles.css"] = BuildStylesheet(),
                ["index.html"] = BuildIndexPage(snapshot, siteTitle),
                ["campaigns.html"] = BuildCampaignsPage(snapshot, siteTitle),
                ["food-banks.html"] = BuildFoodBanksPage(snapshot, siteTitle),
                ["products.html"] = BuildProductsPage(snapshot, siteTitle),
                ["payments.html"] = BuildPaymentsPage(snapshot, siteTitle),
                ["cross-analysis.html"] = BuildCrossAnalysisPage(snapshot, siteTitle),
            };

            return pages;
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
            body.AppendLine($"<h1>Desempenho da campanha</h1>");
            body.AppendLine($"<p class=\"subtitle\">{WebUtility.HtmlEncode(snapshot.CampaignLabel)}</p>");
            body.AppendLine($"<p class=\"meta\">Período: {FormatDate(snapshot.PeriodStart)} – {FormatDate(snapshot.PeriodEnd)} · Atualizado {FormatDateTime(snapshot.GeneratedAtUtc)} UTC</p>");
            body.AppendLine("</section>");

            body.AppendLine("<section class=\"kpi-grid\">");
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
  data: {{ labels: {dailyLabels}, datasets: [{{ label: '€ pagos', data: {dailyAmounts}, borderColor: '#c8102e', backgroundColor: 'rgba(200,16,46,0.12)', fill: true, tension: 0.25 }}] }},
  options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
const statusChart = new Chart(document.getElementById('statusChart'), {{
  type: 'doughnut',
  data: {{ labels: {statusLabels}, datasets: [{{ data: {statusCounts}, backgroundColor: ['#2e7d32','#fbc02d','#c8102e','#616161'] }}] }},
  options: {{ responsive: true }}
}});
</script>";

            return WrapPage(siteTitle, "index.html", "Painel executivo", body.ToString(), script);
        }

        private static string BuildCampaignsPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            string labels = JsonSerializer.Serialize(snapshot.Campaigns.Select(c => c.CampaignName), JsonOptions);
            string amounts = JsonSerializer.Serialize(snapshot.Campaigns.Select(c => Math.Round(c.PaidAmount, 2)), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Por campanha</h1><p>Análise histórica de todas as campanhas registadas.</p></section>");
            body.AppendLine("<section class=\"card chart-card\"><canvas id=\"campaignChart\"></canvas></section>");
            body.AppendLine("<section class=\"card\"><table><thead><tr><th>Campanha</th><th>Pago (€)</th><th>Doações</th><th>Pendentes</th><th>Ticket médio</th><th>Conversão</th></tr></thead><tbody>");
            foreach (DonationReportCampaignRow row in snapshot.Campaigns)
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.CampaignName)}</td><td>{FormatCurrency(row.PaidAmount)}</td><td>{row.PaidCount}</td><td>{row.PendingCount}</td><td>{FormatCurrency(row.AveragePaidAmount)}</td><td>{FormatPercent(row.ConversionPercent)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('campaignChart'), {{
  type: 'bar',
  data: {{ labels: {labels}, datasets: [{{ label: 'Total pago (€)', data: {amounts}, backgroundColor: '#c8102e' }}] }},
  options: {{ indexAxis: 'y', responsive: true, plugins: {{ legend: {{ display: false }} }} }}
}});
</script>";

            return WrapPage(siteTitle, "campaigns.html", "Campanhas", body.ToString(), script);
        }

        private static string BuildFoodBanksPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            var top = snapshot.FoodBanks.Take(15).ToList();
            string labels = JsonSerializer.Serialize(top.Select(f => f.FoodBankName), JsonOptions);
            string amounts = JsonSerializer.Serialize(top.Select(f => Math.Round(f.PaidAmount, 2)), JsonOptions);
            string shares = JsonSerializer.Serialize(top.Select(f => Math.Round(f.SharePercent, 1)), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Por banco alimentar</h1><p>Distribuição geográfica das doações confirmadas no período activo.</p></section>");
            body.AppendLine("<section class=\"chart-row\"><div class=\"card chart-card\"><h2>Volume pago (€)</h2><canvas id=\"fbAmountChart\"></canvas></div>");
            body.AppendLine("<div class=\"card chart-card\"><h2>Quota (%)</h2><canvas id=\"fbShareChart\"></canvas></div></section>");
            body.AppendLine("<section class=\"card\"><table><thead><tr><th>Banco alimentar</th><th>Pago (€)</th><th>Doações</th><th>Unidades</th><th>Quota</th></tr></thead><tbody>");
            foreach (DonationReportFoodBankRow row in snapshot.FoodBanks)
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.FoodBankName)}</td><td>{FormatCurrency(row.PaidAmount)}</td><td>{row.PaidCount}</td><td>{row.ProductUnits:N0}</td><td>{FormatPercent(row.SharePercent)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('fbAmountChart'), {{ type: 'bar', data: {{ labels: {labels}, datasets: [{{ data: {amounts}, backgroundColor: '#1565c0' }}] }}, options: {{ indexAxis: 'y', plugins: {{ legend: {{ display: false }} }} }} }});
new Chart(document.getElementById('fbShareChart'), {{ type: 'pie', data: {{ labels: {labels}, datasets: [{{ data: {shares} }}] }}, options: {{ responsive: true }} }});
</script>";

            return WrapPage(siteTitle, "food-banks.html", "Bancos alimentares", body.ToString(), script);
        }

        private static string BuildProductsPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            var top = snapshot.Products.Take(20).ToList();
            string labels = JsonSerializer.Serialize(top.Select(p => p.ProductName), JsonOptions);
            string quantities = JsonSerializer.Serialize(top.Select(p => p.Quantity), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Por produto doado</h1><p>Unidades e valor de catálogo das doações pagas.</p></section>");
            body.AppendLine("<section class=\"card chart-card\"><canvas id=\"productChart\"></canvas></section>");
            body.AppendLine("<section class=\"card\"><table><thead><tr><th>Produto</th><th>Unidade</th><th>Quantidade</th><th>Valor (€)</th><th>Quota</th></tr></thead><tbody>");
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

            return WrapPage(siteTitle, "products.html", "Produtos", body.ToString(), script);
        }

        private static string BuildPaymentsPage(DonationReportSnapshot snapshot, string siteTitle)
        {
            string labels = JsonSerializer.Serialize(snapshot.Payments.Select(p => p.PaymentTypeLabel), JsonOptions);
            string amounts = JsonSerializer.Serialize(snapshot.Payments.Select(p => Math.Round(p.PaidAmount, 2)), JsonOptions);
            string avgTickets = JsonSerializer.Serialize(snapshot.Payments.Select(p => Math.Round(p.AveragePaidAmount, 2)), JsonOptions);

            StringBuilder body = new StringBuilder();
            body.AppendLine("<section class=\"card\"><h1>Por método de pagamento</h1><p>Mix de receita e comportamento de ticket médio.</p></section>");
            body.AppendLine("<section class=\"chart-row\"><div class=\"card chart-card\"><h2>Receita por método</h2><canvas id=\"payAmountChart\"></canvas></div>");
            body.AppendLine("<div class=\"card chart-card\"><h2>Ticket médio</h2><canvas id=\"payAvgChart\"></canvas></div></section>");
            body.AppendLine("<section class=\"card\"><table><thead><tr><th>Método</th><th>Pago (€)</th><th>Doações</th><th>Ticket médio</th><th>Quota</th></tr></thead><tbody>");
            foreach (DonationReportPaymentRow row in snapshot.Payments)
            {
                body.AppendLine($"<tr><td>{WebUtility.HtmlEncode(row.PaymentTypeLabel)}</td><td>{FormatCurrency(row.PaidAmount)}</td><td>{row.PaidCount}</td><td>{FormatCurrency(row.AveragePaidAmount)}</td><td>{FormatPercent(row.SharePercent)}</td></tr>");
            }

            body.AppendLine("</tbody></table></section>");

            string script = $@"
<script>
new Chart(document.getElementById('payAmountChart'), {{ type: 'doughnut', data: {{ labels: {labels}, datasets: [{{ data: {amounts} }}] }}, options: {{ responsive: true }} }});
new Chart(document.getElementById('payAvgChart'), {{ type: 'bar', data: {{ labels: {labels}, datasets: [{{ label: 'Ticket médio (€)', data: {avgTickets}, backgroundColor: '#6a1b9a' }}] }}, options: {{ plugins: {{ legend: {{ display: false }} }} }} }});
</script>";

            return WrapPage(siteTitle, "payments.html", "Pagamentos", body.ToString(), script);
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

            return WrapPage(siteTitle, "cross-analysis.html", "Análise cruzada", body.ToString(), script);
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

        private static string WrapPage(string siteTitle, string activePage, string pageTitle, string bodyHtml, string pageScript)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"pt\">");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"utf-8\" />");
            html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            html.AppendLine($"<title>{WebUtility.HtmlEncode(pageTitle)} — {WebUtility.HtmlEncode(siteTitle)}</title>");
            html.AppendLine("<link rel=\"stylesheet\" href=\"styles.css\" />");
            html.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js\" crossorigin=\"anonymous\"></script>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<header class=\"site-header\">");
            html.AppendLine($"<div class=\"brand\"><strong>{WebUtility.HtmlEncode(siteTitle)}</strong></div>");
            html.AppendLine("<nav class=\"nav\">");
            html.Append(NavLink("index.html", "Painel", activePage));
            html.Append(NavLink("campaigns.html", "Campanhas", activePage));
            html.Append(NavLink("food-banks.html", "Bancos alimentares", activePage));
            html.Append(NavLink("products.html", "Produtos", activePage));
            html.Append(NavLink("payments.html", "Pagamentos", activePage));
            html.Append(NavLink("cross-analysis.html", "Análise cruzada", activePage));
            html.AppendLine("</nav></header>");
            html.AppendLine("<main class=\"container\">");
            html.AppendLine(bodyHtml);
            html.AppendLine("</main>");
            html.AppendLine("<footer class=\"site-footer\">");
            html.AppendLine("<p>Federação Portuguesa dos Bancos Alimentares Contra a Fome · Relatório gerado automaticamente</p>");
            html.AppendLine("</footer>");
            html.AppendLine(pageScript);
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

        private static string BuildStylesheet()
        {
            return @":root {
  --brand: #c8102e;
  --brand-dark: #8b0a1f;
  --bg: #f6f7f9;
  --card: #ffffff;
  --text: #1f2933;
  --muted: #52606d;
  --border: #d9e2ec;
}
* { box-sizing: border-box; }
body { margin: 0; font-family: 'Segoe UI', system-ui, sans-serif; background: var(--bg); color: var(--text); }
.site-header { background: var(--brand); color: #fff; padding: 1rem 1.5rem; }
.brand { font-size: 1.1rem; margin-bottom: 0.75rem; }
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
@media (max-width: 640px) { .kpi-value { font-size: 1.25rem; } }";
        }
    }
}

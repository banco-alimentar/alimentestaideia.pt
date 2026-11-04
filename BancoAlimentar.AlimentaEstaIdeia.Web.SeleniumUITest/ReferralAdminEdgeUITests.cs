// -----------------------------------------------------------------------
// <copyright file="ReferralAdminEdgeUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;
    using OpenQA.Selenium;
    using Xunit;

    public partial class AutomatedUITests
    {
        private static readonly DateTime AdminEdgeEpoch = new DateTime(2026, 4, 1, 10, 15, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData("Code", false)]
        [InlineData("Code", true)]
        [InlineData("Name", false)]
        [InlineData("Name", true)]
        [InlineData("CreateDate", false)]
        [InlineData("CreateDate", true)]
        [InlineData("LinkOpenCount", false)]
        [InlineData("LinkOpenCount", true)]
        [InlineData("DonationCount", false)]
        [InlineData("DonationCount", true)]
        [InlineData("PaidDonationAmount", false)]
        [InlineData("PaidDonationAmount", true)]
        [InlineData("LastDonationDate", false)]
        [InlineData("LastDonationDate", true)]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralAdminSortingAndPagination")]
        public void Referral_AdminEdge_AllSorts_OrderEveryRowAndPreserveSortAcrossNextPrevious(string column, bool descending)
        {
            PrepareEmptyAdminEdgeList();
            var fixtures = new List<AdminEdgeRow>();
            for (int index = 0; index < 27; index++)
            {
                // Three groups of nine force a tied group across the 25-row page boundary.
                // Code is unique by schema; every other supported sort has deterministic ID ties.
                int group = index % 3;
                fixtures.Add(CreateAdminEdgeRow(
                    "edge" + (26 - index).ToString("D2", CultureInfo.InvariantCulture),
                    new[] { "Bravo", "Charlie", "Alpha" }[group],
                    AdminEdgeEpoch.AddDays(new[] { 2, 0, 1 }[group]),
                    new[] { 9, 0, 18 }[group],
                    group,
                    group == 1 ? 30.5 : 10,
                    AdminEdgeEpoch.AddDays(group == 1 ? 20 : 10),
                    pendingCount: 1));
            }

            // Literal fixture groups form an independent oracle, without querying the page model
            // or sorting persisted aggregates using the production expressions.
            int[] expectedIndexes = AdminEdgeAscendingIndexes(column);
            if (descending)
            {
                Array.Reverse(expectedIndexes);
            }

            var expected = expectedIndexes.Select(index => fixtures[index]).ToArray();
            Login(GetRequiredSeleniumSetting("Admin:Username"), GetRequiredSeleniumSetting("Admin:Password"));
            OpenReferralPage("/Admin/Referrals?sortBy=" + column + "&sortDescending=" + (!descending) + "&pageIndex=2");
            AssertAdminEdgeLocation(column, !descending, 2);

            // Clicking the current header must toggle its direction and return to page one.
            ClickAdminEdgeLink(AdminEdgeSortHeader(column));
            AssertAdminEdgeLocation(column, descending, 1);
            AssertAdminEdgeHeaders(column, descending);
            AssertAdminEdgePagination(column, descending, page: 1);
            var firstPage = AssertAdminEdgeRows(expected.Take(25).ToArray());

            ClickAdminEdgeLink(driver.FindElement(By.CssSelector(".pagination li:last-child a")));
            AssertAdminEdgeLocation(column, descending, 2);
            AssertAdminEdgeHeaders(column, descending);
            AssertAdminEdgePagination(column, descending, page: 2);
            var secondPage = AssertAdminEdgeRows(expected.Skip(25).ToArray());

            int[] completeOrder = firstPage.Concat(secondPage).ToArray();
            Assert.Equal(expected.Select(row => row.Id), completeOrder);
            Assert.Equal(27, completeOrder.Distinct().Count());

            ClickAdminEdgeLink(driver.FindElement(By.CssSelector(".pagination li:first-child a")));
            AssertAdminEdgeLocation(column, descending, 1);
            AssertAdminEdgeHeaders(column, descending);
            AssertAdminEdgePagination(column, descending, page: 1);
            Assert.Equal(firstPage, AssertAdminEdgeRows(expected.Take(25).ToArray()));
        }

        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralAdminEmptyList")]
        public void Referral_AdminEdge_NoActiveReferrals_ShowsEmptyMessageWithoutTableOrPagination()
        {
            PrepareEmptyAdminEdgeList();
            Assert.True(myApplicationDbContext.Referrals.AsNoTracking().Any(), "The empty list must exclude existing inactive referrals.");
            Login(GetRequiredSeleniumSetting("Admin:Username"), GetRequiredSeleniumSetting("Admin:Password"));

            OpenReferralPage("/Admin/Referrals");

            Assert.Equal("/Admin/Referrals", new Uri(driver.Url).AbsolutePath.TrimEnd('/'));
            Assert.Equal("Referências ativas", driver.FindElement(By.TagName("h1")).Text.Trim());
            Assert.Single(driver.FindElements(By.CssSelector("p.text-muted")),
                element => element.Displayed && element.Text.Trim() == "Não existem referências ativas.");
            Assert.Empty(driver.FindElements(By.CssSelector("table, .pagination")));
            Assert.False(myApplicationDbContext.Referrals.AsNoTracking().Any(referral => referral.Active));
        }

        [Theory]
        [InlineData("NoDonations", 0, 0, 8)]
        [InlineData("PendingOnly", 0, 2, 8)]
        [InlineData("PendingOnlyZeroOpenings", 0, 2, 0)]
        [InlineData("PaidWithoutOpenings", 2, 2, 0)]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralAdminEmptyMetrics")]
        public void Referral_AdminEdge_EmptyMetrics_ShowPaidOnlyValuesAndSafeRatios(string scenario, int paidCount, int pendingCount, int openings)
        {
            PrepareEmptyAdminEdgeList();
            var fixture = CreateAdminEdgeRow(
                "edgemetrics", string.Empty, AdminEdgeEpoch, openings, paidCount, 6.25,
                AdminEdgeEpoch.AddDays(10), pendingCount);
            Login(GetRequiredSeleniumSetting("Admin:Username"), GetRequiredSeleniumSetting("Admin:Password"));

            OpenReferralPage("/Admin/Referrals?sortBy=Code&sortDescending=false&pageIndex=1");

            AssertAdminEdgeLocation("Code", false, 1);
            AssertAdminEdgeHeaders("Code", false);
            AssertAdminEdgeRows(new[] { fixture });
            Assert.Empty(driver.FindElements(By.CssSelector(".pagination")));
            Assert.Equal("A mostrar 1–1 de 1 Referências ativas", driver.FindElement(By.CssSelector("p.text-muted")).Text.Trim());
            string ratio = driver.FindElement(By.CssSelector("table tbody tr td:nth-child(6)")).Text.Trim();
            Assert.Equal(scenario == "PaidWithoutOpenings" ? "0,00" : "—", ratio);
            Assert.DoesNotContain("NaN", driver.FindElement(By.TagName("table")).Text);
            Assert.DoesNotContain("Infinity", driver.FindElement(By.TagName("table")).Text);
            Assert.Equal(paidCount + pendingCount, myApplicationDbContext.Donations.AsNoTracking()
                .Count(donation => EF.Property<int?>(donation, "ReferralId") == fixture.Id));
            Assert.Equal(openings, myApplicationDbContext.Referrals.AsNoTracking().Single(referral => referral.Id == fixture.Id).LinkOpenCount);
        }

        private void AssertAdminEdgeOwnedDatabase()
        {
            Assert.True(settings.IsSelfContained, "Admin edge fixtures require the self-contained local host; external targets cannot be mutated.");
            Assert.NotNull(browserApplication);
            browserApplication!.AssertOwnsDatabase(myApplicationDbContext);
        }

        private void PrepareEmptyAdminEdgeList()
        {
            AssertAdminEdgeOwnedDatabase();
            myApplicationDbContext.ChangeTracker.Clear();
            foreach (var referral in myApplicationDbContext.Referrals.Where(referral => referral.Active).ToList())
            {
                // Each test has its own disposable host/database. Preserve the seed's financial
                // records while removing its active rows from the controlled list under assertion.
                referral.Active = false;
            }

            myApplicationDbContext.SaveChanges();
            PrepareReferralBrowser();
        }

        private AdminEdgeRow CreateAdminEdgeRow(
            string code, string name, DateTime created, int openings, int paidCount,
            double paidAmountEach, DateTime lastPaid, int pendingCount)
        {
            AssertAdminEdgeOwnedDatabase();
            string ownerEmail = GetRequiredSeleniumSetting("Site:Username").ToUpperInvariant();
            var owner = myApplicationDbContext.WebUser.Single(user => user.NormalizedEmail == ownerEmail);
            var referral = new Referral
            {
                Code = code,
                Name = name,
                User = owner,
                Active = true,
                IsPublic = false,
                CreateDate = created,
                LinkOpenCount = openings,
            };
            myApplicationDbContext.Referrals.Add(referral);
            var campaign = myApplicationDbContext.Campaigns.First();
            var foodBank = myApplicationDbContext.FoodBanks.First();
            for (int index = 0; index < paidCount + pendingCount; index++)
            {
                bool paid = index < paidCount;
                myApplicationDbContext.Donations.Add(new Donation
                {
                    PublicId = Guid.NewGuid(),
                    User = owner,
                    FoodBank = foodBank,
                    Campaign = campaign,
                    ReferralEntity = referral,
                    PaymentStatus = paid ? PaymentStatus.Payed : index % 2 == 0 ? PaymentStatus.WaitingPayment : PaymentStatus.NotPayed,
                    DonationAmount = paid ? paidAmountEach : 999.99,
                    DonationDate = paid ? lastPaid.AddDays(index - paidCount + 1) : AdminEdgeEpoch.AddYears(1).AddDays(index),
                });
            }

            myApplicationDbContext.SaveChanges();
            return new AdminEdgeRow(
                referral.Id, code, name, created, openings, paidCount,
                paidCount * paidAmountEach, paidCount == 0 ? null : lastPaid);
        }

        private static int[] AdminEdgeAscendingIndexes(string column)
        {
            int[][] groups =
            {
                new[] { 0, 3, 6, 9, 12, 15, 18, 21, 24 },
                new[] { 1, 4, 7, 10, 13, 16, 19, 22, 25 },
                new[] { 2, 5, 8, 11, 14, 17, 20, 23, 26 },
            };
            if (column == "Code")
            {
                return Enumerable.Range(0, 27).Reverse().ToArray();
            }

            int[] groupOrder = column switch
            {
                "Name" => new[] { 2, 0, 1 },
                "CreateDate" => new[] { 1, 2, 0 },
                "LinkOpenCount" => new[] { 1, 0, 2 },
                "DonationCount" => new[] { 0, 1, 2 },
                "PaidDonationAmount" => new[] { 0, 2, 1 },
                "LastDonationDate" => new[] { 0, 2, 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(column)),
            };
            return groupOrder.SelectMany(group => groups[group]).ToArray();
        }

        private IWebElement AdminEdgeSortHeader(string column) => driver.FindElement(By.CssSelector("table thead a[href*='sortBy=" + column + "&']"));

        private void ClickAdminEdgeLink(IWebElement link)
        {
            var table = driver.FindElement(By.TagName("table"));
            link.Click();
            WaitForReferralPageReplacement(table);
        }

        private void AssertAdminEdgeLocation(string column, bool descending, int page)
        {
            Assert.Equal("/Admin/Referrals", new Uri(driver.Url).AbsolutePath.TrimEnd('/'));
            AssertAdminEdgeQuery(driver.Url, column, descending, page);
        }

        private static void AssertAdminEdgeQuery(string url, string column, bool descending, int page)
        {
            var query = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
            Assert.Equal(column, query["sortBy"]);
            Assert.Equal(descending.ToString(), query["sortDescending"], ignoreCase: true);
            Assert.Equal(page.ToString(CultureInfo.InvariantCulture), query["pageIndex"]);
        }

        private void AssertAdminEdgeHeaders(string column, bool descending)
        {
            string[] labels =
            {
                "Imagem", "Código de referência", "Nome", "Data de criação", "Aberturas do link / QR code",
                "Rácio de conversão (aberturas / doações pagas)", "Número de doações",
                "Total doado", "Última doação", string.Empty,
            };
            string?[] columns = { null, "Code", "Name", "CreateDate", "LinkOpenCount", null, "DonationCount", "PaidDonationAmount", "LastDonationDate", null };
            var headers = driver.FindElements(By.CssSelector("table thead th"));
            Assert.Equal(labels.Length, headers.Count);
            for (int index = 0; index < headers.Count; index++)
            {
                bool selected = columns[index] == column;
                Assert.Equal(labels[index] + (selected ? descending ? " ▼" : " ▲" : string.Empty), headers[index].Text.Trim());
                Assert.Equal("col", headers[index].GetAttribute("scope"));
                if (columns[index] != null)
                {
                    var link = Assert.Single(headers[index].FindElements(By.TagName("a")));
                    AssertAdminEdgeQuery(RequiredAttribute(link, "href"), columns[index]!, selected ? !descending : true, 1);
                }
            }
        }

        private void AssertAdminEdgePagination(string column, bool descending, int page)
        {
            var items = driver.FindElements(By.CssSelector(".pagination > li"));
            Assert.Equal(3, items.Count);
            Assert.Equal("Página " + page + " de 2", items[1].Text.Trim());
            var previous = items[0].FindElement(By.TagName("a"));
            var next = items[2].FindElement(By.TagName("a"));
            Assert.Equal("Anterior", previous.Text.Trim());
            Assert.Equal("Seguinte", next.Text.Trim());
            Assert.Equal(page == 1, RequiredAttribute(items[0], "class").Split(' ').Contains("disabled"));
            Assert.Equal(page == 2, RequiredAttribute(items[2], "class").Split(' ').Contains("disabled"));
            Assert.Equal(page == 1 ? "true" : "false", RequiredAttribute(previous, "aria-disabled").ToLowerInvariant());
            Assert.Equal(page == 2 ? "true" : "false", RequiredAttribute(next, "aria-disabled").ToLowerInvariant());
            AssertAdminEdgeQuery(RequiredAttribute(previous, "href"), column, descending, page - 1);
            AssertAdminEdgeQuery(RequiredAttribute(next, "href"), column, descending, page + 1);
            Assert.Equal(page == 1 ? "A mostrar 1–25 de 27 Referências ativas" : "A mostrar 26–27 de 27 Referências ativas",
                driver.FindElement(By.CssSelector("p.text-muted")).Text.Trim());
        }

        private int[] AssertAdminEdgeRows(IReadOnlyList<AdminEdgeRow> expected)
        {
            var rows = driver.FindElements(By.CssSelector("table tbody tr"));
            Assert.Equal(expected.Count, rows.Count);
            var ids = new List<int>();
            for (int index = 0; index < expected.Count; index++)
            {
                var fixture = expected[index];
                var cells = rows[index].FindElements(By.TagName("td"));
                Assert.Equal(10, cells.Count);
                Assert.Empty(cells[0].FindElements(By.TagName("img")));
                Assert.Equal(fixture.Code, cells[1].Text.Trim());
                var campaignLink = Assert.Single(cells[1].FindElements(By.TagName("a")));
                Assert.Equal("/Identity/Account/Manage/CampaignDetail", new Uri(RequiredAttribute(campaignLink, "href")).AbsolutePath);
                int id = int.Parse(System.Web.HttpUtility.ParseQueryString(new Uri(RequiredAttribute(campaignLink, "href")).Query)["id"]!, CultureInfo.InvariantCulture);
                Assert.Equal(fixture.Id, id);
                ids.Add(id);
                Assert.Equal(string.IsNullOrEmpty(fixture.Name) ? "—" : fixture.Name, cells[2].Text.Trim());
                Assert.Equal(fixture.Created.ToString("g", ReferralCulture), cells[3].Text.Trim());
                Assert.Equal(fixture.Openings.ToString(ReferralCulture), cells[4].Text.Trim());
                Assert.Equal(fixture.PaidCount == 0 ? "—" : (fixture.Openings / (double)fixture.PaidCount).ToString("F2", ReferralCulture), cells[5].Text.Trim());
                Assert.Equal(fixture.PaidCount.ToString(ReferralCulture), cells[6].Text.Trim());
                Assert.Equal(fixture.PaidAmount.ToString("F2", ReferralCulture) + " €", cells[7].Text.Trim());
                Assert.Equal(fixture.LastPaid?.ToString("g", ReferralCulture) ?? "—", cells[8].Text.Trim());
                var detailsLink = Assert.Single(cells[9].FindElements(By.TagName("a")));
                Assert.Equal("/Admin/Referrals/Details/" + fixture.Id.ToString(CultureInfo.InvariantCulture),
                    new Uri(RequiredAttribute(detailsLink, "href")).AbsolutePath);
            }

            return ids.ToArray();
        }

        private sealed record AdminEdgeRow(int Id, string Code, string Name, DateTime Created, int Openings, int PaidCount, double PaidAmount, DateTime? LastPaid);
    }
}

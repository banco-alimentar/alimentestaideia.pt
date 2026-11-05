// -----------------------------------------------------------------------
// <copyright file="DuplicateGroupRulesTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using Xunit;

    /// <summary>
    /// Tests the C# port of the SQL planner rules.
    /// </summary>
    public class DuplicateGroupRulesTests
    {
        /// <summary>
        /// The successful-status list must match the list hard-coded in the SQL script.
        /// </summary>
        [Fact]
        public void SuccessfulStatusesMatchSqlScript()
        {
            string script = File.ReadAllText(Path.Combine(RepositoryRoot(), "BancoAlimentar.AlimentaEstaIdeia.Web", "Migrations", "Scripts", "MergeDuplicateEasyPayPaymentIds.sql"));
            Match list = Regex.Match(script, @"IN \((N'[a-z]+'(?:, N'[a-z]+')*)\)");
            Assert.True(list.Success, "The status list was not found in MergeDuplicateEasyPayPaymentIds.sql.");
            string[] sqlStatuses = Regex.Matches(list.Groups[1].Value, "N'([a-z]+)'").Select(match => match.Groups[1].Value).ToArray();

            Assert.Equal(sqlStatuses.OrderBy(value => value), DuplicateGroupRules.SqlSuccessfulStatuses.OrderBy(value => value));
            Assert.All(sqlStatuses, status => Assert.True(DuplicateGroupRules.IsSuccessfulStatusText(status), status));
        }

        /// <summary>
        /// Status checks trim spaces and ignore case, like the SQL script.
        /// </summary>
        [Theory]
        [InlineData("paid", true)]
        [InlineData(" PAID ", true)]
        [InlineData("Success", true)]
        [InlineData("approved", true)]
        [InlineData("COMPLETED", true)]
        [InlineData("Failed", false)]
        [InlineData("err", false)]
        [InlineData("pending", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSuccessfulStatusTextFollowsSql(string? status, bool expected)
        {
            Assert.Equal(expected, DuplicateGroupRules.IsSuccessfulStatusText(status));
        }

        /// <summary>
        /// Amounts are compared as <c>decimal(19,4)</c>.
        /// </summary>
        [Theory]
        [InlineData(10f, 10.0, true)]
        [InlineData(12.3f, 12.3, true)]
        [InlineData(10f, 10.01, false)]
        [InlineData(null, 10.0, false)]
        [InlineData(10f, null, false)]
        public void AmountsMatchAtFourDecimals(float? paid, double? amount, bool expected)
        {
            Assert.Equal(expected, DuplicateGroupRules.AmountsMatch(paid, amount));
        }

        /// <summary>
        /// Provider IDs are grouped case-insensitively and ignoring trailing spaces, like the database collation.
        /// </summary>
        [Fact]
        public void EvaluateGroupsUsesDatabaseCollation()
        {
            var groups = DuplicateGroupRules.EvaluateGroups(new[]
            {
                GroupScenarios.Payment(1, GroupScenarios.T0, providerId: "abc"),
                GroupScenarios.Payment(2, GroupScenarios.T0, providerId: "ABC  "),
                GroupScenarios.Payment(3, GroupScenarios.T0, providerId: "other"),
            });

            GroupEvaluation group = Assert.Single(groups);
            Assert.Equal(1, group.GroupId);
            Assert.Equal(new[] { 1, 2 }, group.Members.Select(member => member.PaymentId));
        }

        /// <summary>
        /// The current rank prefers completed rows, then the newest.
        /// </summary>
        [Fact]
        public void CurrentRankPrefersCompletedThenNewest()
        {
            GroupEvaluation group = DuplicateGroupRules.EvaluateGroup(new[]
            {
                GroupScenarios.Payment(10, GroupScenarios.T0, completed: GroupScenarios.T0),
                GroupScenarios.Payment(11, GroupScenarios.T0.AddDays(1)),
                GroupScenarios.Payment(12, GroupScenarios.T0.AddHours(1), completed: GroupScenarios.T0.AddHours(1)),
            });

            Assert.Equal(12, group.CurrentRankPaymentId);
        }

        /// <summary>
        /// One confirmed, paid, successful owner whose amount matches is ConfirmedPaid.
        /// </summary>
        [Fact]
        public void ConfirmedPaidOwnerIsReconciled()
        {
            GroupEvaluation group = GroupScenarios.ReconciliationToolCopy();

            Assert.Equal(DecisionRule.ConfirmedPaid, group.DecisionRule);
            Assert.Equal(102, group.SuggestedCanonicalPaymentId);
            Assert.Equal(ReconciliationAction.ReconcileIdentifiers, group.PlannerAction);
            Assert.False(group.HasSafeDeletionEvidence);
        }

        /// <summary>
        /// Failed attempts replaced by a successful payment outside the group can be retired or deleted.
        /// </summary>
        [Fact]
        public void SupersededAttemptsHaveDeletionBasis()
        {
            GroupEvaluation group = GroupScenarios.SupersededAttempts();

            Assert.Equal(DecisionRule.NoOwnerRetire, group.DecisionRule);
            Assert.Equal(ReconciliationAction.RetireIdentifier, group.PlannerAction);
            Assert.All(group.Members, member => Assert.Equal(DeletionBasis.SupersededByReplacement, member.DeletionBasis));
            Assert.True(group.HasSafeDeletionEvidence);
        }

        /// <summary>
        /// A replacement whose amount differs from the donation is not proof that the donation was paid.
        /// </summary>
        [Fact]
        public void ReplacementWithDifferentAmountIsNotSuccessful()
        {
            var donation = new DonationFacts(700, Model.PaymentStatus.Payed, 15.0, 999);
            var replacement = new ReplacementPaymentFacts(999, "MBWayPayment", "Success", 14f, 700, null, null);
            GroupEvaluation group = DuplicateGroupRules.EvaluateGroup(new[]
            {
                GroupScenarios.Payment(300, GroupScenarios.T0, status: "Failed", donationId: 700, forward: donation, replacement: replacement),
                GroupScenarios.Payment(301, GroupScenarios.T0, status: "Failed", donationId: 700, forward: donation, replacement: replacement),
            });

            Assert.All(group.Members, member => Assert.False(member.HasSuccessfulReplacement));
            Assert.All(group.Members, member => Assert.Null(member.DeletionBasis));
            Assert.False(group.HasSafeDeletionEvidence);
        }

        /// <summary>
        /// A PayPal replacement counts when it is COMPLETED with payment and payer IDs.
        /// </summary>
        [Fact]
        public void PayPalReplacementCountsWhenCompleted()
        {
            var donation = new DonationFacts(700, Model.PaymentStatus.Payed, 15.0, 999);
            var replacement = new ReplacementPaymentFacts(999, DuplicateGroupRules.PayPalPaymentType, "COMPLETED", null, null, "PAY-1", "PAYER-1");
            GroupEvaluation group = DuplicateGroupRules.EvaluateGroup(new[]
            {
                GroupScenarios.Payment(300, GroupScenarios.T0, status: "Failed", donationId: 700, forward: donation, replacement: replacement),
                GroupScenarios.Payment(301, GroupScenarios.T0, status: "Failed", donationId: 700, forward: donation, replacement: replacement),
            });

            Assert.True(group.HasSafeDeletionEvidence);
        }

        /// <summary>
        /// A NULL paid amount is not zero: SQL treats the comparison as unknown, so there is no deletion basis.
        /// </summary>
        [Fact]
        public void NullPaidAmountBlocksDeletionBasis()
        {
            GroupEvaluation zero = GroupScenarios.AbandonedOrphans(paid: 0f);
            GroupEvaluation unknown = GroupScenarios.AbandonedOrphans(paid: null);

            Assert.All(zero.Members, member => Assert.Equal(DeletionBasis.AbandonedOrphan, member.DeletionBasis));
            Assert.All(unknown.Members, member => Assert.Null(member.DeletionBasis));
            Assert.Equal(DecisionRule.NoOwnerRetire, unknown.DecisionRule);
        }

        /// <summary>
        /// Several confirmed members are MultiConfirmed and get no planner action.
        /// </summary>
        [Fact]
        public void SeveralConfirmedMembersAreMultiConfirmed()
        {
            GroupEvaluation group = GroupScenarios.SimultaneousCharges();

            Assert.Equal(DecisionRule.MultiConfirmed, group.DecisionRule);
            Assert.Null(group.SuggestedCanonicalPaymentId);
            Assert.Null(group.PlannerAction);
            Assert.Equal(2, group.ConfirmedOwnerCount);
        }

        /// <summary>
        /// A member linked to a Payed donation without a confirmed payment needs review.
        /// </summary>
        [Fact]
        public void UnconfirmedPaidDonationNeedsReview()
        {
            var donation = new DonationFacts(900, Model.PaymentStatus.Payed, 20.0, null);
            GroupEvaluation group = DuplicateGroupRules.EvaluateGroup(new[]
            {
                GroupScenarios.Payment(1, GroupScenarios.T0, donationId: 900, forward: donation),
                GroupScenarios.Payment(2, GroupScenarios.T0),
            });

            Assert.Equal(DecisionRule.ReviewUnconfirmedPaidDonation, group.DecisionRule);
            Assert.Null(group.PlannerAction);
        }

        /// <summary>
        /// An empty group is rejected.
        /// </summary>
        [Fact]
        public void EmptyGroupIsRejected()
        {
            Assert.Throws<ArgumentException>(() => DuplicateGroupRules.EvaluateGroup(Array.Empty<PaymentMemberInput>()));
        }

        private static string RepositoryRoot()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "BancoAlimentar.AlimentaEstaIdeia.Web.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}

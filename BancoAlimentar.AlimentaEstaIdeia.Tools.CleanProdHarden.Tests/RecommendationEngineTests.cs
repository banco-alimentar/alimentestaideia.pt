// -----------------------------------------------------------------------
// <copyright file="RecommendationEngineTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using Xunit;

    /// <summary>
    /// Tests pattern classification, recommendations, and suggested reasons.
    /// </summary>
    public class RecommendationEngineTests
    {
        /// <summary>
        /// Each scenario is classified as the pattern it was built to represent.
        /// </summary>
        [Fact]
        public void ClassifiesKnownPatterns()
        {
            Assert.Equal(GroupPattern.ReconciliationToolCopy, GroupPatternClassifier.Classify(GroupScenarios.ReconciliationToolCopy()));
            Assert.Equal(GroupPattern.ConfirmedPaymentWithZeroValueRetries, GroupPatternClassifier.Classify(GroupScenarios.ZeroValueRetries()));
            Assert.Equal(GroupPattern.SupersededAttempts, GroupPatternClassifier.Classify(GroupScenarios.SupersededAttempts()));
            Assert.Equal(GroupPattern.AbandonedOrphanAttempts, GroupPatternClassifier.Classify(GroupScenarios.AbandonedOrphans()));
            Assert.Equal(GroupPattern.UnpaidAttempts, GroupPatternClassifier.Classify(GroupScenarios.AbandonedOrphans(paid: null)));
            Assert.Equal(GroupPattern.SimultaneousSubscriptionCharges, GroupPatternClassifier.Classify(GroupScenarios.SimultaneousCharges()));
            Assert.Equal(GroupPattern.RecurringChargesSharingId, GroupPatternClassifier.Classify(GroupScenarios.SimultaneousCharges(sameDay: false)));
        }

        /// <summary>
        /// Every pattern has a title and an explanation.
        /// </summary>
        [Fact]
        public void EveryPatternHasText()
        {
            foreach (GroupPattern pattern in System.Enum.GetValues<GroupPattern>())
            {
                Assert.False(string.IsNullOrWhiteSpace(GroupPatternClassifier.Title(pattern)));
                Assert.False(string.IsNullOrWhiteSpace(GroupPatternClassifier.Explanation(pattern)));
            }
        }

        /// <summary>
        /// A reconciliation-tool copy keeps the ID on the confirmed owner with high confidence.
        /// </summary>
        [Fact]
        public void ToolCopyRecommendsReconcileToOwner()
        {
            GroupRecommendation recommendation = RecommendationEngine.Recommend(GroupScenarios.ReconciliationToolCopy(), GroupContext.Empty);

            Assert.Equal(ReconciliationAction.ReconcileIdentifiers, recommendation.Action);
            Assert.Equal(102, recommendation.CanonicalPaymentId);
            Assert.Equal(RecommendationConfidence.High, recommendation.Confidence);
            Assert.DoesNotContain(ReconciliationAction.RetireIdentifier, recommendation.Alternatives);
            Assert.DoesNotContain(ReconciliationAction.DeleteGroup, recommendation.Alternatives);
            Assert.Contains("payment 102", recommendation.SuggestedReason);
        }

        /// <summary>
        /// Superseded attempts are retired by default; delete is offered only when the environment allows it.
        /// </summary>
        [Fact]
        public void SupersededAttemptsRecommendRetireWithDeleteAlternative()
        {
            GroupEvaluation group = GroupScenarios.SupersededAttempts();

            GroupRecommendation allowed = RecommendationEngine.Recommend(group, new GroupContext { CanDeleteGroup = true });
            GroupRecommendation blocked = RecommendationEngine.Recommend(group, new GroupContext { CanDeleteGroup = false });

            Assert.Equal(ReconciliationAction.RetireIdentifier, allowed.Action);
            Assert.Null(allowed.CanonicalPaymentId);
            Assert.Equal(RecommendationConfidence.High, allowed.Confidence);
            Assert.Contains(ReconciliationAction.DeleteGroup, allowed.Alternatives);
            Assert.DoesNotContain(ReconciliationAction.DeleteGroup, blocked.Alternatives);
            Assert.DoesNotContain(ReconciliationAction.DeleteGroup, RecommendationEngine.AllowedActions(group, new GroupContext { CanDeleteGroup = false }));
        }

        /// <summary>
        /// Retire is not allowed while a member is confirmed.
        /// </summary>
        [Fact]
        public void RetireIsNotAllowedWhenConfirmed()
        {
            IReadOnlyList<ReconciliationAction> actions = RecommendationEngine.AllowedActions(GroupScenarios.ZeroValueRetries(), GroupContext.Empty);

            Assert.Equal(new[] { ReconciliationAction.ReconcileIdentifiers }, actions);
        }

        /// <summary>
        /// MultiConfirmed prefers the only member whose donation has an invoice.
        /// </summary>
        [Fact]
        public void MultiConfirmedPrefersInvoicedDonation()
        {
            GroupEvaluation group = GroupScenarios.SimultaneousCharges();

            GroupRecommendation invoiced = RecommendationEngine.Recommend(group, new GroupContext { DonationIdsWithInvoice = new HashSet<int> { 801 } });
            GroupRecommendation fallback = RecommendationEngine.Recommend(group, GroupContext.Empty);

            Assert.Equal(501, invoiced.CanonicalPaymentId);
            Assert.Equal(RecommendationConfidence.Low, invoiced.Confidence);
            Assert.Equal(500, fallback.CanonicalPaymentId);
            Assert.NotEmpty(invoiced.FollowUps);
        }

        /// <summary>
        /// The suggested reason follows the selected action.
        /// </summary>
        [Fact]
        public void SuggestReasonFollowsSelectedAction()
        {
            GroupEvaluation group = GroupScenarios.SupersededAttempts();
            GroupPattern pattern = GroupPatternClassifier.Classify(group);

            string retire = RecommendationEngine.SuggestReason(group, pattern, ReconciliationAction.RetireIdentifier, null);
            string delete = RecommendationEngine.SuggestReason(group, pattern, ReconciliationAction.DeleteGroup, null);
            string reconcile = RecommendationEngine.SuggestReason(group, pattern, ReconciliationAction.ReconcileIdentifiers, 301);

            Assert.Contains("retire it with audit on all 2 members", retire);
            Assert.Contains("delete all 2 unpaid attempts", delete);
            Assert.Contains("keep provider ID on payment 301", reconcile);
            Assert.Contains("NoOwnerRetire", delete);
        }

        /// <summary>
        /// No reason is suggested for an incomplete selection.
        /// </summary>
        [Fact]
        public void SuggestReasonIsEmptyForIncompleteSelection()
        {
            GroupEvaluation group = GroupScenarios.SupersededAttempts();

            Assert.Equal(string.Empty, RecommendationEngine.SuggestReason(group, GroupPattern.SupersededAttempts, null, null));
            Assert.Equal(string.Empty, RecommendationEngine.SuggestReason(group, GroupPattern.SupersededAttempts, ReconciliationAction.ReconcileIdentifiers, null));
        }

        /// <summary>
        /// Amounts are formatted with the invariant culture.
        /// </summary>
        [Theory]
        [InlineData(10.0, "10.00")]
        [InlineData(12.345, "12.345")]
        [InlineData(null, "null")]
        public void FormatAmountUsesInvariantCulture(double? amount, string expected)
        {
            Assert.Equal(expected, RecommendationEngine.FormatAmount(amount));
        }
    }
}

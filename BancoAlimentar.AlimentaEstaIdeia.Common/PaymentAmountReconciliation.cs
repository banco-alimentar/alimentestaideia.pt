// -----------------------------------------------------------------------
// <copyright file="PaymentAmountReconciliation.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common
{
    using System;

    /// <summary>
    /// Validates that payment provider amounts match the expected donation total.
    /// </summary>
    public static class PaymentAmountReconciliation
    {
        /// <summary>
        /// Default tolerance for currency rounding (EUR cents).
        /// </summary>
        public const double DefaultTolerance = 0.01d;

        /// <summary>
        /// Returns true when requested and paid amounts match the expected donation amount within tolerance.
        /// </summary>
        /// <param name="expectedDonationAmount">Donation amount stored server-side.</param>
        /// <param name="requested">Amount requested by the payment provider.</param>
        /// <param name="paid">Amount paid according to the payment provider.</param>
        /// <param name="tolerance">Allowed absolute difference.</param>
        /// <returns>True when all three values align.</returns>
        public static bool AmountsMatchDonation(
            double expectedDonationAmount,
            double requested,
            double paid,
            double tolerance = DefaultTolerance)
        {
            return IsWithinTolerance(expectedDonationAmount, requested, tolerance)
                && IsWithinTolerance(expectedDonationAmount, paid, tolerance)
                && IsWithinTolerance(requested, paid, tolerance);
        }

        /// <summary>
        /// Returns true when the provider value matches the donation amount within tolerance.
        /// </summary>
        /// <param name="expectedDonationAmount">Donation amount stored server-side.</param>
        /// <param name="providerValue">Value reported by Easypay.</param>
        /// <param name="tolerance">Allowed absolute difference.</param>
        /// <returns>True when values align.</returns>
        public static bool ProviderValueMatchesDonation(
            double expectedDonationAmount,
            double providerValue,
            double tolerance = DefaultTolerance)
        {
            return IsWithinTolerance(expectedDonationAmount, providerValue, tolerance);
        }

        private static bool IsWithinTolerance(double expected, double actual, double tolerance)
        {
            return Math.Abs(expected - actual) <= tolerance;
        }
    }
}

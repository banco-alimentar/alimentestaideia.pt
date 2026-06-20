// -----------------------------------------------------------------------
// <copyright file="DonationPaymentCompletionMultiBankTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Xunit;

    /// <summary>
    /// Tests for multibanco awaiting-payment detection.
    /// </summary>
    public class DonationPaymentCompletionMultiBankTests
    {
        /// <summary>
        /// Waiting donations with an open multibanco row still need payment communication.
        /// </summary>
        [Fact]
        public void IsAwaitingMultiBankPayment_ReturnsTrueForWaitingDonation()
        {
            var donation = new Donation { PaymentStatus = PaymentStatus.WaitingPayment };
            var payment = new MultiBankPayment();

            Assert.True(DonationPaymentCompletion.IsAwaitingMultiBankPayment(donation, payment));
        }

        /// <summary>
        /// Paid donations must not receive multibanco payment prompts.
        /// </summary>
        [Fact]
        public void IsAwaitingMultiBankPayment_ReturnsFalseWhenDonationIsPaid()
        {
            var donation = new Donation { PaymentStatus = PaymentStatus.Payed };
            var payment = new MultiBankPayment();

            Assert.False(DonationPaymentCompletion.IsAwaitingMultiBankPayment(donation, payment));
        }

        /// <summary>
        /// Completed payment rows must not receive multibanco payment prompts.
        /// </summary>
        [Fact]
        public void IsAwaitingMultiBankPayment_ReturnsFalseWhenPaymentCompleted()
        {
            var donation = new Donation { PaymentStatus = PaymentStatus.WaitingPayment };
            var payment = new MultiBankPayment { Completed = DateTime.UtcNow };

            Assert.False(DonationPaymentCompletion.IsAwaitingMultiBankPayment(donation, payment));
        }

        /// <summary>
        /// Provider success status must not receive multibanco payment prompts.
        /// </summary>
        [Fact]
        public void IsAwaitingMultiBankPayment_ReturnsFalseWhenPaymentStatusIsSuccess()
        {
            var donation = new Donation { PaymentStatus = PaymentStatus.WaitingPayment };
            var payment = new MultiBankPayment { Status = "Success" };

            Assert.False(DonationPaymentCompletion.IsAwaitingMultiBankPayment(donation, payment));
        }

        /// <summary>
        /// Successful EasyPay payments record completion metadata.
        /// </summary>
        [Fact]
        public void MarkSuccessfulEasyPayPayment_SetsCompletedAndSuccessStatus()
        {
            var payment = new MultiBankPayment();

            DonationPaymentCompletion.MarkSuccessfulEasyPayPayment(payment);

            Assert.True(payment.Completed.HasValue);
            Assert.Equal("Success", payment.Status);
        }
    }
}

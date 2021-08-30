// -----------------------------------------------------------------------
// <copyright file="DonationRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Easypay.Rest.Client.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// This class defines unit tests for donation repository.
    /// </summary>
    public class DonationRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture _fixture;
        private readonly DonationRepository _donationRepository;
        private readonly ApplicationDbContext _context;

        public DonationRepositoryTests(ServicesFixture servicesFixture)
        {
            this._fixture = servicesFixture;
            this._donationRepository = this._fixture.ServiceProvider.GetRequiredService<DonationRepository>();
            this._context = _fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        [Fact]
        public async Task Can_Get_TotalDonations()
        {
            var items = await _context.ProductCatalogues.ToListAsync();
            var result = _donationRepository.GetTotalDonations(items);
            Assert.Equal(1, result.First().Total);
        }

        [Fact]
        public async Task Can_ClaimDonationToUser()
        {
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "newtest@newtest.com",
                FullName = "New Test User"
            };
            await _fixture.UserManager.CreateAsync(user);

            var result = _donationRepository.ClaimDonationToUser(_fixture.PublicId, user);
            Assert.True(result);

        }

        [Fact]
        public async Task Can_Not_ClaimDonationToUser_When_PublicId_IsNot_Guid()
        {
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "newtest@newtest.com",
                FullName = "New Test User"
            };
            await _fixture.UserManager.CreateAsync(user);

            var result = _donationRepository.ClaimDonationToUser("notguidvalue", user);
            Assert.False(result);

        }

        [Fact]
        public async Task Can_Not_ClaimDonationToUser_With_Empty_PublicId()
        {
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "newtest@newtest.com",
                FullName = "New Test User"
            };
            await _fixture.UserManager.CreateAsync(user);

            var result = _donationRepository.ClaimDonationToUser(string.Empty, user);
            Assert.False(result);

        }

        [Fact]
        public void Can_GetDonationFromPublicId()
        {
            var result = _donationRepository.GetDonationIdFromPublicId(new Guid(_fixture.PublicId));
            Assert.Equal(_fixture.DonationId, result);
        }

        [Fact]
        public void Can_GetDonationFromTransactionKey()
        {
            var result = _donationRepository.GetDonationIdFromPaymentTransactionId(_fixture.TransactionKey);
            Assert.Equal(_fixture.DonationId, result);
        }


        [Fact]
        public void Can_FindPaymentByType()
        {
            var result = _donationRepository.FindPaymentByType<CreditCardPayment>(_fixture.DonationId);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);

        }


        [Fact]
        public async Task Can_Update_CreditCard_Payment()
        {
            var result = _donationRepository.UpdateCreditCardPayment(new Guid(_fixture.PublicId), "anything");
            Assert.True(result);

            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            Assert.Equal(PaymentStatus.NotPayed, donation.PaymentStatus);

            result = _donationRepository.UpdateCreditCardPayment(new Guid(_fixture.PublicId), "err");
            Assert.True(result);

            donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            Assert.Equal(PaymentStatus.ErrorPayment, donation.PaymentStatus);

            result = _donationRepository.UpdateCreditCardPayment(new Guid(_fixture.PublicId), "ok");
            Assert.True(result);

            donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);

        }

        [Fact]
        public async Task Can_UpdateDonationPaymentId()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            var result = _donationRepository.UpdateDonationPaymentId(donation, "COMPLETED", "somerandomtoken", "12345");
            Assert.True(result);
            Assert.True(donation.Payments.Count == 2);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_UpdateDonationPaymentId_When_Donation_Has_No_Payments()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;
            var result = _donationRepository.UpdateDonationPaymentId(donation, "COMPLETED", "somerandomtoken", "12345");
            Assert.True(result);
            Assert.True(donation.Payments.Count == 1);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public void Can_Not_UpdateDonationPaymentId_When_Donation_IsNull()
        {
            var result = _donationRepository.UpdateDonationPaymentId(null, "COMPLETED", "somerandomtoken", "12345");
            Assert.False(result);
        }

        [Fact]
        public async Task Can_UpdateMultiBankPayment()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;
            var result = _donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");
            Assert.True(result);
            Assert.True(donation.Payments.Count == 1);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public void Can_Not_Can_UpdateMultiBankPayment_When_Donation_IsNull()
        {
            var result = _donationRepository.UpdateMultiBankPayment(null, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");
            Assert.False(result);
        }

        [Fact]
        public async Task Can_UpdateMultiBankPayment_When_Donation_Has_No_Payments()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;
            var result = _donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");
            Assert.True(result);
            Assert.True(donation.Payments.Count == 1);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public void Can_UpdatePaymentTransaction_To_Payed()
        {
            var result = _donationRepository.UpdatePaymentTransaction(string.Empty, _fixture.TransactionKey, GenericNotificationRequest.StatusEnum.Success, string.Empty);
            Assert.True(result > 0);

            var donation = _context.PaymentItems
                    .Where(p => p.Payment.TransactionKey == _fixture.TransactionKey)
                    .Select(p => p.Donation)
                    .FirstOrDefault();

            Assert.NotNull(donation);
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
        }

        [Fact]
        public async Task Can_UpdatePaymentTransaction_To_ErrorPayment()
        {
            var result = _donationRepository.UpdatePaymentTransaction(string.Empty, _fixture.TransactionKey, GenericNotificationRequest.StatusEnum.Failed, string.Empty);
            Assert.True(result > 0);

            var donation = _context.PaymentItems
                    .Where(p => p.Payment.TransactionKey == _fixture.TransactionKey)
                    .Select(p => p.Donation)
                    .FirstOrDefault();

            Assert.NotNull(donation);
            Assert.Equal(PaymentStatus.ErrorPayment, donation.PaymentStatus);

            //Update the status back to payed.
            donation.PaymentStatus = PaymentStatus.Payed;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public void Can_Not_UpdatePaymentTransaction_When_TransactionKey_Is_Invalid()
        {
            var result = _donationRepository.UpdatePaymentTransaction(string.Empty, "wrong-transaction-key", GenericNotificationRequest.StatusEnum.Failed, string.Empty);
            Assert.True(result == 0);
        }

        [Fact]
        public async Task Can_CompleteMultiBankPayment()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;
            _donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), _fixture.TransactionKey, "entity", "refrence");

            var result = _donationRepository.CompleteMultiBankPayment(string.Empty, _fixture.TransactionKey, GenericNotificationRequest.TypeEnum.Capture.ToString(), GenericNotificationRequest.StatusEnum.Success.ToString(), "message");
            Assert.True(result > -1);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_Not_CompleteMultiBankPayment_With_InValid_TransactionKey()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;
            _donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), _fixture.TransactionKey, "entity", "refrence");

            var result = _donationRepository.CompleteMultiBankPayment(string.Empty, "wrong-transaction-key", GenericNotificationRequest.TypeEnum.Capture.ToString(), GenericNotificationRequest.StatusEnum.Success.ToString(), "message");
            Assert.True(result == -1);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_CreateMBWayPayment()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;

            var result = _donationRepository.CreateMBWayPayment(donation, "easypayid", _fixture.TransactionKey, "alias");
            Assert.True(result);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public void Can_Not_CreateMBWayPayment_When_Donation_IsNull()
        {
            var result = _donationRepository.CreateMBWayPayment(null, "easypayid", _fixture.TransactionKey, "alias");
            Assert.False(result);
        }

        [Fact]
        public async Task Can_CreateCreditCardPayment()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;

            var result = _donationRepository.CreateCreditCardPaymnet(donation, "easypayid", _fixture.TransactionKey, "url", DateTime.Now);
            Assert.True(result);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public void Can_Not_CreateCreditCardPayment_When_Donation_IsNull()
        {
            var result = _donationRepository.CreateCreditCardPaymnet(null, "easypayid", _fixture.TransactionKey, "url", DateTime.Now);
            Assert.False(result);
        }

        [Fact]
        public async Task Can_CompleteCreditCardPayment()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;
            var testTransactionKey = Guid.NewGuid().ToString();
            _donationRepository.CreateCreditCardPaymnet(donation, "easypayid", testTransactionKey, "url", DateTime.Now);

            var result = _donationRepository.CompleteCreditCardPayment("easypayid", testTransactionKey, "easypayment-transactionid", DateTime.Now,
                10, 10, 0, 0, 0, 0);

            Assert.True(result > 0);


            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_Not_CompleteCreditCardPayment_With_Wrong_TransactionKey()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;
            var testTransactionKey = Guid.NewGuid().ToString();
            _donationRepository.CreateCreditCardPaymnet(donation, "easypayid", testTransactionKey, "url", DateTime.Now);

            var result = _donationRepository.CompleteCreditCardPayment("easypayid", "wrong-transaction-key", "easypayment-transactionid", DateTime.Now,
                10, 10, 0, 0, 0, 0);

            Assert.True(result == 0);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }


        [Fact]
        public async Task Can_Not_CompleteCreditCardPayment_With_MisMatched_TransactionKey()
        {
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;
            var testTransactionKey = Guid.NewGuid().ToString();
            _donationRepository.CreateCreditCardPaymnet(donation, "easypayid", testTransactionKey, "url", DateTime.Now);

            var result = _donationRepository.CompleteCreditCardPayment("easypayid", _fixture.TransactionKey, "easypayment-transactionid", DateTime.Now,
                10, 10, 0, 0, 0, 0);

            Assert.True(result == 0);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_CompleteMBWayPayment()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;

            _donationRepository.CreateMBWayPayment(donation, "easypayid", testTransactionKey, "alias");

            var result = _donationRepository.CompleteMBWayPayment("easypayid", testTransactionKey,
                10, 10, 0, 0, 0, 0);

            Assert.True(result > 0);


            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_Not_CompleteMBWayPayment_With_Wrong_TransactionKey()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;

            _donationRepository.CreateMBWayPayment(donation, "easypayid", testTransactionKey, "alias");

            var result = _donationRepository.CompleteMBWayPayment("easypayid", "wrong-transaction-key",
                10, 10, 0, 0, 0, 0);

            Assert.True(result == -1);


            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_Not_CompleteMBWayPayment_With_MisMatched_TransactionKey()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;

            _donationRepository.CreateMBWayPayment(donation, "easypayid", testTransactionKey, "alias");

            var result = _donationRepository.CompleteMBWayPayment("easypayid", _fixture.TransactionKey,
                10, 10, 0, 0, 0, 0);

            Assert.True(result == -1);


            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_GetCurrentMultiBankPayment()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;

            _donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");

            var result = _donationRepository.GetCurrentMultiBankPayment(donation.Id);
            Assert.NotNull(result);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_Not_GetCurrentMultiBankPayment_For_Wrong_DonationId()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await _context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            var payments = donation.Payments;
            donation.Payments = null;

            _donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");

            var result = _donationRepository.GetCurrentMultiBankPayment(4000);
            Assert.Null(result);

            //Add payments back
            donation.Payments = payments;
            await _context.SaveChangesAsync();
        }

        [Fact]
        public void Can_GetFullDonationById()
        {
            var result = _donationRepository.GetFullDonationById(_fixture.DonationId);

            Assert.NotNull(result);
            Assert.NotNull(result.User);
            Assert.NotNull(result.DonationItems);
            Assert.NotNull(result.FoodBank);
            Assert.NotNull(result.ConfirmedPayment);
        }

        [Fact]
        public void Can_GetPaymentsForDonation()
        {
            var result = _donationRepository.GetPaymentsForDonation(_fixture.DonationId);

            Assert.NotNull(result);

        }

        [Fact]
        public void Can_GetUserDonation()
        {
            var result = _donationRepository.GetUserDonation(_fixture.UserId);

            Assert.NotNull(result);

            var donation = result.FirstOrDefault();
            Assert.NotNull(donation.DonationItems);
            Assert.NotNull(donation.FoodBank);
            Assert.NotNull(donation.Payments);
        }

        [Fact]
        public void Can_GetPaymentType()
        {
            var result = _donationRepository.GetPaymentType(new PayPalPayment());
            Assert.True(PaymentType.Paypal == result);
            result = _donationRepository.GetPaymentType(new CreditCardPayment());
            Assert.True(PaymentType.CreditCard == result);
            result = _donationRepository.GetPaymentType(new MBWayPayment());
            Assert.True(PaymentType.MBWay == result);
            result = _donationRepository.GetPaymentType(new MultiBankPayment());
            Assert.True(PaymentType.MultiBanco == result);
            result = _donationRepository.GetPaymentType(null);
            Assert.True(PaymentType.None == result);
        }
    }
}

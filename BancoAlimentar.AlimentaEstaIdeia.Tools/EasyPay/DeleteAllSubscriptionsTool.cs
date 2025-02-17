﻿namespace BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Easypay.Rest.Client.Api;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Linq;

    internal class DeleteAllSubscriptionsTool : EasyPayTool
    {
        public DeleteAllSubscriptionsTool(
            ApplicationDbContext context,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
                : base(context, unitOfWork, configuration)
        {
        }

        public override void ExecuteTool()
        {
            SubscriptionPaymentApi subscriptionPaymentApi = this.GetSubscriptionPaymentApi();
            var allSubscriptions = subscriptionPaymentApi.SubscriptionGet(recordsPerPage: 100);
            foreach (var subscription in allSubscriptions.Data)
            {
                try
                {
                    subscriptionPaymentApi.SubscriptionIdDelete(Guid.Parse(subscription.Id.ToString()));
                    Console.WriteLine($"Subscription {subscription.Id} Deleted");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Subscription {subscription.Id} Error {ex.Message}");
                }
            }

            var allDbSubscriptions = this.Context.Subscriptions
                .Include(p => p.Donations)
                .ToList();
            foreach (var subscription in allDbSubscriptions)
            {
                foreach (var donation in subscription.Donations)
                {
                    Donation deletedDonation = this.Context.Donations
                        .Where(p => p.Id == donation.Id)
                        .Include(p => p.DonationItems)
                        .Include(p => p.ConfirmedPayment)
                        .FirstOrDefault();

                    if (deletedDonation != null)
                    {
                        if (deletedDonation.ConfirmedPayment != null)
                        {
                            this.Context.Entry(deletedDonation.ConfirmedPayment).State = EntityState.Deleted;
                        }

                        var payment = this.Context.Payments.Where(p => p.Donation.Id == deletedDonation.Id).FirstOrDefault();
                        if (payment != null)
                        {
                            this.Context.Entry(payment).State = EntityState.Deleted;
                            this.Context.SaveChanges();
                        }

                        foreach (var donationItem in deletedDonation.DonationItems)
                        {
                            this.Context.Entry(donationItem).State = EntityState.Deleted;
                        }
                        this.Context.SaveChanges();

                        var invoice = this.Context.Invoices.Where(p => p.Donation.Id == deletedDonation.Id).FirstOrDefault();
                        if (invoice != null)
                        {
                            this.Context.Entry(invoice).State = EntityState.Deleted;
                            this.Context.SaveChanges();
                        }

                        this.Context.Entry(deletedDonation).State = EntityState.Deleted;
                        this.Context.SaveChanges();
                    }
                }

                //var userSubscription = this.Context.UsersSubscriptions.Where(p => p.Subscription.Id == subscription.Id).First();
                //this.Context.Entry(userSubscription).State = EntityState.Deleted;
                //this.Context.SaveChanges();

                this.Context.Entry(subscription).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                this.Context.SaveChanges();
            }
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="UpdateSubscriptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Subscription = BancoAlimentar.AlimentaEstaIdeia.Model.Subscription;

    /// <summary>
    /// Update subscriptions Azure Function.
    /// </summary>
    public class UpdateSubscriptions : MultiTenantFunction
    {
        private readonly Func<Subscription, IConfiguration, ISubscriptionPaymentApi> subscriptionPaymentApiFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubscriptions"/> class.
        /// </summary>
        public UpdateSubscriptions(TelemetryConfiguration telemetryConfiguration, IServiceProvider serviceProvider)
            : this(telemetryConfiguration, serviceProvider, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubscriptions"/> class.
        /// </summary>
        /// <param name="telemetryConfiguration">Application Insights configuration.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="subscriptionPaymentApiFactory">Factory used to create tenant-specific EasyPay clients.</param>
        public UpdateSubscriptions(
            TelemetryConfiguration telemetryConfiguration,
            IServiceProvider serviceProvider,
            Func<Subscription, IConfiguration, ISubscriptionPaymentApi> subscriptionPaymentApiFactory)
            : base(telemetryConfiguration, serviceProvider)
        {
            this.subscriptionPaymentApiFactory = subscriptionPaymentApiFactory ?? this.CreateSubscriptionPaymentApi;
            this.ExecuteFunction = new Func<IUnitOfWork, ApplicationDbContext, Task>(this.UpdateSubscriptionsFunction);
            this.ExecuteFunctionWithReport = this.UpdateSubscriptionsFunctionWithReport;
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        [Function("UpdateSubscriptions")]
        public async Task Run([TimerTrigger("* * */24 * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            try
            {
                await this.RunFunctionCore();
            }
            catch (Exception ex)
            {
                this.TelemetryClient.TrackException(
                    ex,
                    new Dictionary<string, string>()
                    {
                        { "FunctionName", "UpdateSubscriptions" },
                    });
            }
        }

        private async Task UpdateSubscriptionsFunction(IUnitOfWork context, ApplicationDbContext applicationDbContext)
        {
            await this.UpdateSubscriptionsFunctionWithReport(context, applicationDbContext, null).ConfigureAwait(false);
        }

        private async Task UpdateSubscriptionsFunctionWithReport(
            IUnitOfWork context,
            ApplicationDbContext applicationDbContext,
            IFunctionExecutionReportExecution report)
        {
            List<Subscription> subscriptions = await applicationDbContext.Subscriptions
                .Include(subscription => subscription.InitialDonation)
                .ThenInclude(donation => donation.FoodBank)
                .Where(subscription =>
                    subscription.Status == SubscriptionStatus.Active
                    && !subscription.IsDeleted
                    && subscription.SubscriptionType == SubscriptionPaymentType.CC
                    && subscription.ExpirationTime > DateTime.UtcNow
                    && subscription.InitialDonation != null)
                .ToListAsync()
                .ConfigureAwait(false);

            int updated = 0;
            int failed = 0;
            foreach (Subscription subscription in subscriptions)
            {
                try
                {
                    if (!Guid.TryParse(subscription.EasyPaySubscriptionId, out Guid easypaySubscriptionId))
                    {
                        throw new InvalidOperationException("The active subscription has an invalid Easypay subscription id.");
                    }

                    ISubscriptionPaymentApi api = this.subscriptionPaymentApiFactory(subscription, this.Configuration);
                    SubscriptionIdGet200Response current = api.SubscriptionIdGet(easypaySubscriptionId);
                    if (current == null)
                    {
                        throw new InvalidOperationException("Easypay returned no subscription details.");
                    }

                    if (current.Retries >= 3
                        || current.Method?.Status != SubscriptionIdGet200ResponseMethod.StatusEnum.Active)
                    {
                        continue;
                    }

                    SubscriptionIdPatch200Response response = api.SubscriptionIdPatch(
                        easypaySubscriptionId,
                        new SubscriptionIdPatchRequest
                        {
                            Retries = 3,
                            Failover = current.Failover,
                        });
                    if (response == null || !string.Equals(response.Status, "ok", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Easypay did not confirm the subscription retry update.");
                    }

                    updated++;
                }
                catch (Exception exception)
                {
                    failed++;
                    this.TelemetryClient.TrackException(
                        exception,
                        new Dictionary<string, string>
                        {
                            { "FunctionName", "UpdateSubscriptions" },
                            { "SubscriptionId", subscription.EasyPaySubscriptionId ?? string.Empty },
                        });
                    report?.RecordError($"Failed to synchronize retry settings for subscription {subscription.Id}.");
                }
            }

            int skipped = subscriptions.Count - updated - failed;
            report?.SetCounter("subscriptionsExamined", subscriptions.Count);
            report?.SetCounter("subscriptionsUpdated", updated);
            report?.SetCounter("subscriptionsSkipped", skipped);
            report?.SetCounter("subscriptionsFailed", failed);
            report?.RecordActivity(
                "subscription-synchronization",
                failed == 0 ? FunctionExecutionReportActivitySeverity.Information : FunctionExecutionReportActivitySeverity.Warning,
                $"Examined {subscriptions.Count} active credit-card subscriptions; updated {updated}, skipped {skipped}, and failed {failed}.");
            if (failed > 0)
            {
                report?.MarkOutcome(updated > 0 ? FunctionExecutionReportOutcome.Partial : FunctionExecutionReportOutcome.Failed);
            }
        }

        private ISubscriptionPaymentApi CreateSubscriptionPaymentApi(Subscription subscription, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new InvalidOperationException("Tenant configuration is unavailable for subscription synchronization.");
            }

            if (!Enum.TryParse(configuration["Tenant:PaymentStrategy"], ignoreCase: true, out PaymentStrategy paymentStrategy))
            {
                throw new InvalidOperationException("Tenant payment strategy is unavailable for subscription synchronization.");
            }

            string accountId;
            string apiKey;
            if (paymentStrategy == PaymentStrategy.SharedPaymentProcessor)
            {
                accountId = configuration["Easypay:AccountId"];
                apiKey = configuration["Easypay:ApiKey"];
            }
            else if (paymentStrategy == PaymentStrategy.IndividualPaymentProcessorPerFoodBank
                && subscription.InitialDonation?.FoodBank != null)
            {
                int foodBankId = subscription.InitialDonation.FoodBank.Id;
                accountId = configuration[$"Easypay:AccountId-{foodBankId}"];
                apiKey = configuration[$"Easypay:ApiKey-{foodBankId}"];
            }
            else
            {
                throw new InvalidOperationException("A supported Easypay payment strategy and food bank are required.");
            }

            if (string.IsNullOrWhiteSpace(configuration["Easypay:BaseUrl"])
                || string.IsNullOrWhiteSpace(accountId)
                || string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Easypay API URL or credentials are not configured for subscription synchronization.");
            }

            var easypayConfiguration = new Easypay.Rest.Client.Client.Configuration
            {
                BasePath = configuration["Easypay:BaseUrl"] + "/2.0",
            };
            easypayConfiguration.DefaultHeaders.Add("Content-Type", "application/json");
            easypayConfiguration.ApiKey.Add("AccountId", accountId);
            easypayConfiguration.ApiKey.Add("ApiKey", apiKey);
            return new SubscriptionPaymentApi(easypayConfiguration);
        }
    }
}

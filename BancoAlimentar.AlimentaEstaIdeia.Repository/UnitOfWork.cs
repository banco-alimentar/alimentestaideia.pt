// -----------------------------------------------------------------------
// <copyright file="UnitOfWork.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Unit of work for the Entity Framework core.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly TelemetryClient telemetryClient;
        private readonly IMemoryCache memoryCache;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
        /// </summary>
        /// <param name="applicationDbContext"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="telemetryClient">A reference to the <see cref="TelemetryClient"/>.</param>
        /// <param name="memoryCache">A referece to the memory cache system.</param>
        /// <param name="nifApiValidator">Nif API validator.</param>
        public UnitOfWork(
            ApplicationDbContext applicationDbContext,
            TelemetryClient telemetryClient,
            IMemoryCache memoryCache,
            NifApiValidator nifApiValidator)
        {
            this.applicationDbContext = applicationDbContext;
            this.telemetryClient = telemetryClient;
            this.memoryCache = memoryCache;
            this.Donation = new DonationRepository(applicationDbContext, memoryCache, telemetryClient);
            this.DonationItem = new DonationItemRepository(applicationDbContext, memoryCache, telemetryClient);
            this.FoodBank = new FoodBankRepository(applicationDbContext, memoryCache, telemetryClient);
            this.ProductCatalogue = new ProductCatalogueRepository(applicationDbContext, memoryCache, telemetryClient);
            this.User = new UserRepository(applicationDbContext, memoryCache, telemetryClient);
            this.Invoice = new InvoiceRepository(applicationDbContext, memoryCache, telemetryClient, nifApiValidator);
            this.CampaignRepository = new CampaignRepository(applicationDbContext, memoryCache, telemetryClient);
            this.SubscriptionRepository = new SubscriptionRepository(applicationDbContext, memoryCache, telemetryClient);
            this.SubscriptionNotificationRepository = new SubscriptionNotificationRepository(applicationDbContext, memoryCache, telemetryClient);
            this.ReferralRepository = new ReferralRepository(applicationDbContext, memoryCache, telemetryClient);
            this.PaymentNotificationRepository = new PaymentNotificationRepository(applicationDbContext, memoryCache, telemetryClient);
            this.SetTelemetryClient();
        }

        /// <inheritdoc/>
        public DonationItemRepository DonationItem { get; internal set; }

        /// <inheritdoc/>
        public DonationRepository Donation { get; internal set; }

        /// <inheritdoc/>
        public FoodBankRepository FoodBank { get; internal set; }

        /// <inheritdoc/>
        public ProductCatalogueRepository ProductCatalogue { get; internal set; }

        /// <inheritdoc/>
        public UserRepository User { get; internal set; }

        /// <inheritdoc/>
        public InvoiceRepository Invoice { get; internal set; }

        /// <inheritdoc/>
        public CampaignRepository CampaignRepository { get; internal set; }

        /// <inheritdoc/>
        public ReferralRepository ReferralRepository { get; internal set; }

        /// <inheritdoc/>
        public SubscriptionRepository SubscriptionRepository { get; internal set; }

        /// <inheritdoc/>
        public SubscriptionNotificationRepository SubscriptionNotificationRepository { get; internal set; }

        /// <inheritdoc/>
        public PaymentNotificationRepository PaymentNotificationRepository { get; internal set; }

        /// <inheritdoc/>
        public IMemoryCache MemoryCache => this.memoryCache;

        /// <inheritdoc/>
        public int Complete()
        {
            return this.applicationDbContext.SaveChanges();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the class.
        /// </summary>
        /// <param name="disposing">True if disposing, false otherwise.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.applicationDbContext.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        private void SetTelemetryClient()
        {
            var properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var telemetryClientProperty = property.PropertyType.GetProperty("TelemetryClient");
                if (telemetryClientProperty != null)
                {
                    telemetryClientProperty.SetValue(property.GetValue(this), this.telemetryClient);
                }
            }
        }
    }
}

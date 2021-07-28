// -----------------------------------------------------------------------
// <copyright file="IUnitOfWork.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Unit of work contract.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="DonationItemRepository"/>.
        /// </summary>
        DonationItemRepository DonationItem { get; }

        /// <summary>
        /// Gets the <see cref="DonationRepository"/>.
        /// </summary>
        DonationRepository Donation { get; }

        /// <summary>
        /// Gets the <see cref="FoodBankRepository"/>.
        /// </summary>
        FoodBankRepository FoodBank { get; }

        /// <summary>
        /// Gets the <see cref="ProductCatalogueRepository"/>.
        /// </summary>
        ProductCatalogueRepository ProductCatalogue { get; }

        /// <summary>
        /// Gets the <see cref="UserRepository"/>.
        /// </summary>
        UserRepository User { get; }

        /// <summary>
        /// Gets the <see cref="InvoiceRepository"/>.
        /// </summary>
        InvoiceRepository Invoice { get; }

        /// <summary>
        /// Gets the <see cref="CampaignRepository"/>.
        /// </summary>
        CampaignRepository CampaignRepository { get; }

        /// <summary>
        /// Gets the <see cref="ReferralRepository"/>.
        /// </summary>
        ReferralRepository ReferralRepository { get; }

        /// <summary>
        /// Gets the <see cref="SubscriptionRepository"/>.
        /// </summary>
        SubscriptionRepository SubscriptionRepository { get; }

        /// <summary>
        /// Gets the cache system.
        /// </summary>
        public IMemoryCache MemoryCache { get; }

        /// <summary>
        /// Complete the in memmory changes.
        /// </summary>
        /// <returns>Number of affected rows.</returns>
        int Complete();
    }
}

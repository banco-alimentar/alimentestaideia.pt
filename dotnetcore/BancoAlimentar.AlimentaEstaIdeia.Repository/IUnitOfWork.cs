﻿namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;

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
        /// Complete the in memmory changes.
        /// </summary>
        /// <returns>Number of affected rows.</returns>
        int Complete();
    }
}

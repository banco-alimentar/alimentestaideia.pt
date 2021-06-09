// -----------------------------------------------------------------------
// <copyright file="SinglePaymentAuditingTable.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.AzureTables
{
    using Azure.Data.Tables;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Represent the Single Payment Azure Table auditing table.
    /// </summary>
    public class SinglePaymentAuditingTable
    {
        private readonly IConfiguration configuration;
        private TableEntity entity;

        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePaymentAuditingTable"/> class.
        /// </summary>
        /// <param name="configuration">System configuration.</param>
        /// <param name="publicDonationId">The public donation id.</param>
        /// <param name="email">User's email.</param>
        public SinglePaymentAuditingTable(IConfiguration configuration, string publicDonationId, string email)
        {
            this.entity = new TableEntity(email, publicDonationId);
            this.configuration = configuration;
        }

        /// <summary>
        /// Add a property to the entity.
        /// </summary>
        /// <param name="key">Name of the property.</param>
        /// <param name="value">Value of the property.</param>
        public void AddProperty(string key, object value)
        {
            this.entity.Add(key, value);
        }

        /// <summary>
        /// Save the entity in the Azure Table.
        /// </summary>
        public void SaveEntity()
        {
            TableClient client = new TableClient(
                this.configuration["AzureStorage:ConnectionString"],
                this.configuration["AzureStorage:SinglePaymentAuditingTableName"]);

            client.AddEntity(this.entity);
        }
    }
}

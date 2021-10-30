// -----------------------------------------------------------------------
// <copyright file="EmailAuditingTable.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.AzureTables
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.AzureTables.Model;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Represent the Email Azure Table auditing table.
    /// </summary>
    public class EmailAuditingTable
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAuditingTable"/> class.
        /// </summary>
        /// <param name="configuration">System configuration.</param>
        public EmailAuditingTable(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <param name="userId">User id.</param>
        /// <returns>A reference to the <see cref="EmailAuditing"/> item.</returns>
        public EmailAuditing GetEntityById(int donationId, string userId)
        {
            EmailAuditing result = null;

            TableClient client = new TableClient(
                this.configuration["AzureStorage:ConnectionString"],
                this.configuration["AzureStorage:EmailAuditingTableName"]);

            Response<EmailAuditing> response = client.GetEntity<EmailAuditing>(userId, donationId.ToString());
            if (response != null && response.Value != null)
            {
                result = response.Value;
            }

            return result;
        }

        /// <summary>
        /// Save the entity in the Azure Table.
        /// </summary>
        /// <param name="value">Email auditing record.</param>
        public void SaveEntity(EmailAuditing value)
        {
            TableClient client = new TableClient(
                this.configuration["AzureStorage:ConnectionString"],
                this.configuration["AzureStorage:EmailAuditingTableName"]);

            client.UpsertEntity(value);
        }
    }
}

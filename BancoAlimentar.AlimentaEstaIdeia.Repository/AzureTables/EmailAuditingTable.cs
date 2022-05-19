// -----------------------------------------------------------------------
// <copyright file="EmailAuditingTable.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.AzureTables;

using System.Linq;
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
        TableClient client = new TableClient(
            this.configuration["AzureStorage:ConnectionString"],
            this.configuration["AzureStorage:EmailAuditingTableName"]);

        return client
            .Query<EmailAuditing>(p => p.PartitionKey == userId && p.RowKey == donationId.ToString())
            .FirstOrDefault();
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

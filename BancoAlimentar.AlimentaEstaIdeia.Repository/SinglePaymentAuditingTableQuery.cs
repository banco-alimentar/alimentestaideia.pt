// -----------------------------------------------------------------------
// <copyright file="SinglePaymentAuditingTableQuery.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Represent the Single Payment Azure Table auditing table query.
    /// </summary>
    public class SinglePaymentAuditingTableQuery
    {
        private readonly IConfiguration configuration;
        private readonly TableClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePaymentAuditingTableQuery"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public SinglePaymentAuditingTableQuery(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.client = new TableClient(
                this.configuration["AzureStorage:ConnectionString"],
                this.configuration["AzureStorage:SinglePaymentAuditingTableName"]);
        }

        /// <summary>
        /// Get the entity by transaction key.
        /// </summary>
        /// <param name="transactionKey">EasyPay transaction key.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<TableEntity> GetEntityByTransactionKey(string transactionKey)
        {
            List<TableEntity> entities = new List<TableEntity>();
            await foreach (var entity in this.client.QueryAsync<TableEntity>($"TransactionKey eq '{transactionKey}'"))
            {
                entities.Add(entity);
            }

            return entities.FirstOrDefault();
        }
    }
}

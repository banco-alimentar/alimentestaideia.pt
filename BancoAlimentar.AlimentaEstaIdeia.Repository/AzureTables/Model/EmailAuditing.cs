// -----------------------------------------------------------------------
// <copyright file="EmailAuditing.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.AzureTables.Model
{
    using System;
    using Azure;
    using Azure.Data.Tables;

    /// <summary>
    /// Represent the email audinting row.
    /// </summary>
    public class EmailAuditing : ITableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAuditing"/> class.
        /// </summary>
        public EmailAuditing()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAuditing"/> class.
        /// </summary>
        /// <param name="partitionkey">Partition key.</param>
        /// <param name="rowKey">Row key.</param>
        public EmailAuditing(string partitionkey, string rowKey)
        {
            this.PartitionKey = partitionkey;
            this.RowKey = rowKey;
        }

        /// <summary>
        /// Gets or sets the email log.
        /// </summary>
        public string EmailLog { get; set; }

        /// <summary>
        /// Gets or sets the payment id.
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Gets or sets the easypay transaction id.
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets the partition key.
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the row key.
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the etag.
        /// </summary>
        public ETag ETag { get; set; }
    }
}

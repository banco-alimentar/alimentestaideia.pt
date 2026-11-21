// -----------------------------------------------------------------------
// <copyright file="TargetConnectionInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Hosting
{
    using System;
    using Microsoft.Data.SqlClient;

    /// <summary>
    /// Describes the configured target connection without exposing credentials.
    /// </summary>
    public sealed class TargetConnectionInfo
    {
        /// <summary>
        /// Connection string name.
        /// </summary>
        public const string ConnectionStringName = "DefaultConnection";

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetConnectionInfo"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string, if configured.</param>
        public TargetConnectionInfo(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                this.Error = $"ConnectionStrings:{ConnectionStringName} is not configured.";
                return;
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                this.DataSource = builder.DataSource;
                this.InitialCatalog = builder.InitialCatalog;
                this.IsConfigured = true;
            }
            catch (ArgumentException exception)
            {
                this.Error = $"ConnectionStrings:{ConnectionStringName} is invalid: {exception.Message}";
            }
        }

        /// <summary>
        /// Gets a value indicating whether a valid connection string is configured.
        /// </summary>
        public bool IsConfigured { get; }

        /// <summary>
        /// Gets the configured server.
        /// </summary>
        public string? DataSource { get; }

        /// <summary>
        /// Gets the configured database.
        /// </summary>
        public string? InitialCatalog { get; }

        /// <summary>
        /// Gets the configuration error.
        /// </summary>
        public string? Error { get; }
    }
}

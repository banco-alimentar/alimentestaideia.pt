// -----------------------------------------------------------------------
// <copyright file="SqlServerFiscalFixture.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using Microsoft.Data.SqlClient;
    using Testcontainers.MsSql;
    using Xunit;

    /// <summary>
    /// Owns the opt-in SQL Server Testcontainers lifecycle for relational fiscal tests.
    /// </summary>
    public sealed class SqlServerFiscalFixture : IAsyncLifetime
    {
        private const string OptInVariable = "RUN_SQLSERVER_FISCAL_TESTS";
        private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";
        private MsSqlContainer container;

        /// <summary>
        /// Gets a value indicating whether a Docker-backed SQL Server instance is available for this test run.
        /// </summary>
        public bool IsAvailable { get; private set; }

        /// <summary>
        /// Gets the reason relational tests cannot run in this environment.
        /// </summary>
        public string UnavailableReason { get; private set; }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable(OptInVariable), "1", StringComparison.Ordinal))
            {
                this.UnavailableReason = $"Set {OptInVariable}=1 on a Docker-capable host to run SQL Server fiscal tests.";
                return;
            }

            try
            {
                this.container = new MsSqlBuilder(SqlServerImage).Build();
                await this.container.StartAsync();
                this.IsAvailable = true;
            }
            catch (Exception exception)
            {
                this.UnavailableReason =
                    $"SQL Server Testcontainers could not start ({exception.GetType().Name}). Ensure Docker is running and retry.";
                if (this.container != null)
                {
                    await this.container.DisposeAsync();
                    this.container = null;
                }
            }
        }

        /// <inheritdoc />
        public async Task DisposeAsync()
        {
            if (this.container != null)
            {
                await this.container.DisposeAsync();
            }
        }

        /// <summary>
        /// Skips the current test when the opt-in SQL Server fixture is unavailable.
        /// </summary>
        public void SkipUnlessAvailable()
        {
            Skip.If(!this.IsAvailable, this.UnavailableReason);
        }

        /// <summary>
        /// Creates an isolated database in the test container.
        /// </summary>
        /// <returns>The isolated test database.</returns>
        public async Task<SqlServerFiscalDatabase> CreateDatabaseAsync()
        {
            this.SkipUnlessAvailable();
            string databaseName = $"FiscalTests_{Guid.NewGuid():N}";
            var builder = new SqlConnectionStringBuilder(this.container.GetConnectionString())
            {
                InitialCatalog = "master",
            };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{databaseName}]";
            await command.ExecuteNonQueryAsync();

            builder.InitialCatalog = databaseName;
            return new SqlServerFiscalDatabase(builder.ConnectionString, databaseName);
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="SqlRunner.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Storage;

    /// <summary>
    /// Runs raw SQL on an EF context's connection, inside its current transaction when there is one.
    /// </summary>
    internal static class SqlRunner
    {
        /// <summary>
        /// Command timeout for analysis and execution batches, in seconds.
        /// </summary>
        public const int CommandTimeoutSeconds = 120;

        /// <summary>
        /// Creates an <c>nvarchar(max)</c> parameter.
        /// </summary>
        /// <param name="name">Parameter name, including <c>@</c>.</param>
        /// <param name="value">Value.</param>
        /// <returns>The parameter.</returns>
        public static SqlParameter Text(string name, string? value)
        {
            return new SqlParameter(name, SqlDbType.NVarChar, -1) { Value = (object?)value ?? DBNull.Value };
        }

        /// <summary>
        /// Creates an <c>int</c> parameter.
        /// </summary>
        /// <param name="name">Parameter name, including <c>@</c>.</param>
        /// <param name="value">Value.</param>
        /// <returns>The parameter.</returns>
        public static SqlParameter Int(string name, int? value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = (object?)value ?? DBNull.Value };
        }

        /// <summary>
        /// Creates a JSON array parameter of IDs for <c>OPENJSON</c>.
        /// </summary>
        /// <param name="name">Parameter name, including <c>@</c>.</param>
        /// <param name="ids">IDs.</param>
        /// <returns>The parameter.</returns>
        public static SqlParameter Ids(string name, IEnumerable<int> ids)
        {
            return Text(name, JsonSerializer.Serialize(ids.Distinct().OrderBy(id => id)));
        }

        /// <summary>
        /// Runs a query and maps every row.
        /// </summary>
        /// <typeparam name="T">Row type.</typeparam>
        /// <param name="database">EF database facade.</param>
        /// <param name="sql">SQL batch.</param>
        /// <param name="map">Row mapper.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Mapped rows.</returns>
        public static async Task<List<T>> QueryAsync<T>(
            DatabaseFacade database,
            string sql,
            Func<DbDataReader, T> map,
            CancellationToken cancellationToken,
            params SqlParameter[] parameters)
        {
            await database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using DbCommand command = CreateCommand(database, sql, parameters);
                await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                var rows = new List<T>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    rows.Add(map(reader));
                }

                return rows;
            }
            finally
            {
                await database.CloseConnectionAsync();
            }
        }

        /// <summary>
        /// Runs a query and returns the first column of the first row.
        /// </summary>
        /// <param name="database">EF database facade.</param>
        /// <param name="sql">SQL batch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="parameters">Parameters.</param>
        /// <returns>The value, or null for no row or <c>DBNull</c>.</returns>
        public static async Task<object?> ScalarAsync(DatabaseFacade database, string sql, CancellationToken cancellationToken, params SqlParameter[] parameters)
        {
            await database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using DbCommand command = CreateCommand(database, sql, parameters);
                object? value = await command.ExecuteScalarAsync(cancellationToken);
                return value is DBNull ? null : value;
            }
            finally
            {
                await database.CloseConnectionAsync();
            }
        }

        /// <summary>
        /// Runs a statement batch.
        /// </summary>
        /// <param name="database">EF database facade.</param>
        /// <param name="sql">SQL batch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Rows affected.</returns>
        public static async Task<int> ExecuteAsync(DatabaseFacade database, string sql, CancellationToken cancellationToken, params SqlParameter[] parameters)
        {
            await database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using DbCommand command = CreateCommand(database, sql, parameters);
                return await command.ExecuteNonQueryAsync(cancellationToken);
            }
            finally
            {
                await database.CloseConnectionAsync();
            }
        }

        /// <summary>
        /// Reads a nullable column.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="reader">Reader.</param>
        /// <param name="ordinal">Column ordinal.</param>
        /// <returns>The value or default.</returns>
        public static T? Get<T>(DbDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? default : reader.GetFieldValue<T>(ordinal);
        }

        /// <summary>
        /// Quotes an identifier as <c>QUOTENAME</c> does.
        /// </summary>
        /// <param name="identifier">Identifier.</param>
        /// <returns>Bracket-quoted identifier.</returns>
        public static string QuoteName(string identifier)
        {
            ArgumentNullException.ThrowIfNull(identifier);
            return "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";
        }

        private static DbCommand CreateCommand(DatabaseFacade database, string sql, SqlParameter[] parameters)
        {
            DbCommand command = database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Transaction = database.CurrentTransaction?.GetDbTransaction();
            command.Parameters.AddRange(parameters);
            return command;
        }
    }
}

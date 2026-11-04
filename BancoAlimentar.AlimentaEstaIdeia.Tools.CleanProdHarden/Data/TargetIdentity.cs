// -----------------------------------------------------------------------
// <copyright file="TargetIdentity.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    /// <summary>
    /// Identity of the target database, as SQL Server reports it.
    /// </summary>
    /// <param name="ServerName"><c>SERVERPROPERTY('ServerName')</c>, which the SQL script binds plans to.</param>
    /// <param name="DatabaseName"><c>DB_NAME()</c>.</param>
    /// <param name="Login"><c>ORIGINAL_LOGIN()</c>.</param>
    /// <param name="ProductVersion">SQL Server version.</param>
    public sealed record TargetIdentity(string ServerName, string DatabaseName, string Login, string ProductVersion)
    {
        /// <summary>
        /// SQL that returns the identity columns in order.
        /// </summary>
        public const string Sql = "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')), DB_NAME(), ORIGINAL_LOGIN(), CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion'));";

        /// <summary>
        /// Gets the key used to separate decisions per target.
        /// </summary>
        public string Key => $"{this.ServerName}|{this.DatabaseName}";
    }
}

// -----------------------------------------------------------------------
// <copyright file="SqlServerFiscalCollection.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using Xunit;

    /// <summary>
    /// Serializes SQL Server fiscal tests because each test creates and destroys a database in
    /// the same local container.
    /// </summary>
    [CollectionDefinition("SQL Server fiscal", DisableParallelization = true)]
    public class SqlServerFiscalCollection : ICollectionFixture<SqlServerFiscalFixture>
    {
    }
}

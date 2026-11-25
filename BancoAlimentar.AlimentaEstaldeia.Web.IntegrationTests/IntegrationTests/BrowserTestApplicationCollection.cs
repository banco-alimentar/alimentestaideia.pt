// -----------------------------------------------------------------------
// <copyright file="BrowserTestApplicationCollection.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using Xunit;

    /// <summary>
    /// Groups tests that start isolated browser hosts so they do not compete with other test collections.
    /// </summary>
    [CollectionDefinition(nameof(BrowserTestApplicationCollection), DisableParallelization = true)]
    public sealed class BrowserTestApplicationCollection
    {
    }
}

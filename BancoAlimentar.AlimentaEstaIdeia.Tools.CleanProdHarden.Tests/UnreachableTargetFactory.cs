// -----------------------------------------------------------------------
// <copyright file="UnreachableTargetFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// A target-database factory that fails the test if anything tries to reach the database.
    /// </summary>
    internal sealed class UnreachableTargetFactory : IDbContextFactory<ApplicationDbContext>
    {
        /// <inheritdoc/>
        public ApplicationDbContext CreateDbContext()
        {
            throw new InvalidOperationException("The target database must not be used by this code path.");
        }
    }
}

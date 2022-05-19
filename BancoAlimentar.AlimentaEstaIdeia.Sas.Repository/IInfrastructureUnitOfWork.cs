// -----------------------------------------------------------------------
// <copyright file="IInfrastructureUnitOfWork.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;

using System;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Unit of work contract for the Infrastructure database.
/// </summary>
public interface IInfrastructureUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the <see cref="TenantRepository"/>.
    /// </summary>
    TenantRepository TenantRepository { get; }

    /// <summary>
    /// Gets the cache system.
    /// </summary>
    public IMemoryCache MemoryCache { get; }

    /// <summary>
    /// Complete the in memmory changes.
    /// </summary>
    /// <returns>Number of affected rows.</returns>
    int Complete();
}

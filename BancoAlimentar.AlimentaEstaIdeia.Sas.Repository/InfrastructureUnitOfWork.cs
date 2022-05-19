// -----------------------------------------------------------------------
// <copyright file="InfrastructureUnitOfWork.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;

using System;
using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;

/// <inheritdoc/>
public class InfrastructureUnitOfWork : IInfrastructureUnitOfWork
{
    private readonly InfrastructureDbContext infrastructureDbContext;
    private readonly TelemetryClient telemetryClient;
    private readonly IMemoryCache memoryCache;
    private bool disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfrastructureUnitOfWork"/> class.
    /// </summary>
    /// <param name="infrastructureDbContext">Infrastructure db context.</param>
    /// <param name="telemetryClient">Telemetry client.</param>
    /// <param name="memoryCache">Memory cache service.</param>
    public InfrastructureUnitOfWork(
        InfrastructureDbContext infrastructureDbContext,
        TelemetryClient telemetryClient,
        IMemoryCache memoryCache)
    {
        this.infrastructureDbContext = infrastructureDbContext;
        this.telemetryClient = telemetryClient;
        this.memoryCache = memoryCache;

        this.TenantRepository = new TenantRepository(infrastructureDbContext, memoryCache, telemetryClient);
    }

    /// <summary>
    /// Gets the <see cref="TenantRepository"/>.
    /// </summary>
    public TenantRepository TenantRepository { get; internal set; }

    /// <summary>
    /// Gets the memory cache service.
    /// </summary>
    public IMemoryCache MemoryCache => this.memoryCache;

    /// <inheritdoc/>
    public int Complete()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose the class.
    /// </summary>
    /// <param name="disposing">True if disposing, false otherwise.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                this.infrastructureDbContext.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            this.disposedValue = true;
        }
    }
}

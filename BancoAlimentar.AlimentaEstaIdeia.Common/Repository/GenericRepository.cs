// -----------------------------------------------------------------------
// <copyright file="GenericRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Generic repository.
/// </summary>
/// <typeparam name="Type">Type of the entity.</typeparam>
/// /// <typeparam name="TDbContext">Type of the DbContext.</typeparam>
public class GenericRepository<Type, TDbContext> : IGenericRepository<Type>
    where Type : class
    where TDbContext : DbContext
{
    private readonly TDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericRepository{Type, TDbContext}"/> class.
    /// </summary>
    /// <param name="context">A reference to the <see cref="DbContext"/>.</param>
    /// <param name="memoryCache">A reference to the Memory cache system.</param>
    /// <param name="telemetryClient">Telemetry client.</param>
    public GenericRepository(
        TDbContext context,
        IMemoryCache memoryCache,
        TelemetryClient telemetryClient)
    {
        this.context = context;
        this.MemoryCache = memoryCache;
        this.TelemetryClient = telemetryClient;
    }

    /// <summary>
    /// Gets or sets the Application Insights Telemetry Client.
    /// </summary>
    public TelemetryClient TelemetryClient { get; set; }

    /// <summary>
    /// Gets the memory cache system.
    /// </summary>
    public IMemoryCache MemoryCache { get; }

    /// <summary>
    /// Gets the <see cref="DbContext"/>.
    /// </summary>
    protected TDbContext DbContext
    {
        get
        {
            return this.context;
        }
    }

    /// <inheritdoc/>
    public void Add(Type entity)
    {
        this.context.Set<Type>().Add(entity);
    }

    /// <inheritdoc/>
    public void AddRange(IEnumerable<Type> entities)
    {
        this.context.Set<Type>().AddRange(entities);
    }

    /// <inheritdoc/>
    public IQueryable<Type> Find(Expression<Func<Type, bool>> expression)
    {
        return this.context.Set<Type>().Where(expression);
    }

    /// <inheritdoc/>
    public IQueryable<Type> GetAll()
    {
        return this.context.Set<Type>().AsQueryable();
    }

    /// <inheritdoc/>
    public Type GetById(int id)
    {
        return this.context.Set<Type>().Find(id);
    }

    /// <inheritdoc/>
    public void Modify(Type entity)
    {
        EntityEntry<Type> entry = this.context.Entry<Type>(entity);
        entry.State = EntityState.Modified;
    }

    /// <inheritdoc/>
    public void Remove(Type entity)
    {
        this.context.Set<Type>().Remove(entity);
    }

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<Type> entities)
    {
        this.context.Set<Type>().RemoveRange(entities);
    }
}

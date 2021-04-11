// -----------------------------------------------------------------------
// <copyright file="GenericRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;

    /// <summary>
    /// Generic repository.
    /// </summary>
    /// <typeparam name="T">Type of the entity.</typeparam>
    public class GenericRepository<T> : IGenericRepository<T>
        where T : class
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericRepository{T}"/> class.
        /// </summary>
        /// <param name="context">A reference to the <see cref="ApplicationDbContext"/>.</param>
        public GenericRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the Application Insights Telemetry Client.
        /// </summary>
        public TelemetryClient TelemetryClient { get; set; }

        /// <summary>
        /// Gets the <see cref="ApplicationDbContext"/>.
        /// </summary>
        protected ApplicationDbContext DbContext
        {
            get
            {
                return this.context;
            }
        }

        /// <inheritdoc/>
        public void Add(T entity)
        {
            this.context.Set<T>().Add(entity);
        }

        /// <inheritdoc/>
        public void AddRange(IEnumerable<T> entities)
        {
            this.context.Set<T>().AddRange(entities);
        }

        /// <inheritdoc/>
        public IQueryable<T> Find(Expression<Func<T, bool>> expression)
        {
            return this.context.Set<T>().Where(expression);
        }

        /// <inheritdoc/>
        public IQueryable<T> GetAll()
        {
            return this.context.Set<T>().AsQueryable();
        }

        /// <inheritdoc/>
        public T GetById(int id)
        {
            return this.context.Set<T>().Find(id);
        }

        /// <inheritdoc/>
        public void Modify(T entity)
        {
            EntityEntry<T> entry = this.context.Entry<T>(entity);
            entry.State = EntityState.Modified;
        }

        /// <inheritdoc/>
        public void Remove(T entity)
        {
            this.context.Set<T>().Remove(entity);
        }

        /// <inheritdoc/>
        public void RemoveRange(IEnumerable<T> entities)
        {
            this.context.Set<T>().RemoveRange(entities);
        }
    }
}

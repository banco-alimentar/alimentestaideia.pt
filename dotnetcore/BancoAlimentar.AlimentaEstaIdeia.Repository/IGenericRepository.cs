// -----------------------------------------------------------------------
// <copyright file="IGenericRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;

    /// <summary>
    /// Interface for the GenericRepository.
    /// </summary>
    /// <typeparam name="T">Type of the repository.</typeparam>
    public interface IGenericRepository<T>
        where T : class
    {
        /// <summary>
        /// Gets an entity by the id.
        /// </summary>
        /// <param name="id">Id of the entity.</param>
        /// <returns>A reference to the entity.</returns>
        T GetById(int id);

        /// <summary>
        /// Gets all the element in the repository.
        /// </summary>
        /// <returns>A collection with all the entities.</returns>
        IQueryable<T> GetAll();

        /// <summary>
        /// Perform a search in the repository.
        /// </summary>
        /// <param name="expression">Linq Expression.</param>
        /// <returns>A collection with all the entities.</returns>
        IQueryable<T> Find(Expression<Func<T, bool>> expression);

        /// <summary>
        /// Adds a new element to the repository.
        /// </summary>
        /// <param name="entity">A reference to the element.</param>
        void Add(T entity);

        /// <summary>
        /// Adds a collection of elements to the repository.
        /// </summary>
        /// <param name="entities">A reference to the collection element to add.</param>
        void AddRange(IEnumerable<T> entities);

        /// <summary>
        /// Remove a element from the repository.
        /// </summary>
        /// <param name="entity">A reference to the element.</param>
        void Remove(T entity);

        /// <summary>
        /// Mark an entity as modified.
        /// </summary>
        /// <param name="entity">A reference to the element.</param>
        void Modify(T entity);

        /// <summary>
        /// Remove a collection of elements from the repository.
        /// </summary>
        /// <param name="entities">A reference to the collection element to remove.</param>
        void RemoveRange(IEnumerable<T> entities);
    }
}

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

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
        /// Gets the <see cref="ApplicationDbContext"/>.
        /// </summary>
        protected ApplicationDbContext DbContext
        {
            get
            {
                return this.context;
            }
        }

        /// <summary>
        /// Adds a new element to the repository.
        /// </summary>
        /// <param name="entity">A reference to the element.</param>
        public void Add(T entity)
        {
            this.context.Set<T>().Add(entity);
        }

        /// <summary>
        /// Adds a collection of elements to the repository.
        /// </summary>
        /// <param name="entities">A reference to the collection element to add.</param>
        public void AddRange(IEnumerable<T> entities)
        {
            this.context.Set<T>().AddRange(entities);
        }

        /// <summary>
        /// Perform a search in the repository.
        /// </summary>
        /// <param name="expression">Linq Expression.</param>
        /// <returns>A collection with all the entities.</returns>
        public IQueryable<T> Find(Expression<Func<T, bool>> expression)
        {
            return this.context.Set<T>().Where(expression);
        }

        /// <summary>
        /// Gets all the element in the repository.
        /// </summary>
        /// <returns>A collection with all the entities.</returns>
        public IQueryable<T> GetAll()
        {
            return this.context.Set<T>().AsQueryable();
        }

        /// <summary>
        /// Gets an entity by the id.
        /// </summary>
        /// <param name="id">Id of the entity.</param>
        /// <returns>A reference to the entity.</returns>
        public T GetById(int id)
        {
            return this.context.Set<T>().Find(id);
        }

        /// <summary>
        /// Remove a element from the repository.
        /// </summary>
        /// <param name="entity">A reference to the element.</param>
        public void Remove(T entity)
        {
            this.context.Set<T>().Remove(entity);
        }

        /// <summary>
        /// Remove a collection of elements from the repository.
        /// </summary>
        /// <param name="entities">A reference to the collection element to remove.</param>
        public void RemoveRange(IEnumerable<T> entities)
        {
            this.context.Set<T>().RemoveRange(entities);
        }
    }
}

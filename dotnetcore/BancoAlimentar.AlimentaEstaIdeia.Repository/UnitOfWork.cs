namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Unit of work for the Entity Framework core.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext applicationDbContext;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
        /// </summary>
        /// <param name="applicationDbContext"><see cref="ApplicationDbContext"/> instance.</param>
        public UnitOfWork(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
            this.Donation = new DonationRepository(applicationDbContext);
            this.DonationItem = new DonationItemRepository(applicationDbContext);
            this.FoodBank = new FoodBankRepository(applicationDbContext);
            this.ProductCatalogue = new ProductCatalogueRepository(applicationDbContext);
            this.User = new UserRepository(applicationDbContext);
            this.Invoice = new InvoiceRepository(applicationDbContext);
        }

        /// <inheritdoc/>
        public DonationItemRepository DonationItem { get; internal set; }

        /// <inheritdoc/>
        public DonationRepository Donation { get; internal set; }

        /// <inheritdoc/>
        public FoodBankRepository FoodBank { get; internal set; }

        /// <inheritdoc/>
        public ProductCatalogueRepository ProductCatalogue { get; internal set; }

        /// <inheritdoc/>
        public UserRepository User { get; internal set; }

        /// <inheritdoc/>
        public InvoiceRepository Invoice { get; internal set; }

        /// <inheritdoc/>
        public int Complete()
        {
            return this.applicationDbContext.SaveChanges();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.applicationDbContext.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

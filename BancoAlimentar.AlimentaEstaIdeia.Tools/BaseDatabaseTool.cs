namespace BancoAlimentar.AlimentaEstaIdeia.Tools
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.EntityFrameworkCore;

    public abstract class BaseDatabaseTool : DbContext
    {
        public BaseDatabaseTool(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            Context = context;
            UnitOfWork = unitOfWork;
        }

        protected ApplicationDbContext Context { get; }

        protected IUnitOfWork UnitOfWork { get; }

        public abstract void ExecuteTool();
    }
}

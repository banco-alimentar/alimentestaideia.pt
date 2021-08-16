namespace BancoAlimentar.AlimentaEstaIdeia.Tools
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class BaseDatabaseTool
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

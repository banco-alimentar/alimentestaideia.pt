namespace BancoAlimentar.AlimentaEstaIdeia.Tools
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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

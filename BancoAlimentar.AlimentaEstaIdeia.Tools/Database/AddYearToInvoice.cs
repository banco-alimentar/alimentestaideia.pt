namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    public class AddYearToInvoice : BaseDatabaseTool
    {
        public AddYearToInvoice(
            ApplicationDbContext context,
            IUnitOfWork unitOfWork)
            : base(context, unitOfWork)
        {
        }

        public override void ExecuteTool()
        {
            var strategy = this.Context.Database.CreateExecutionStrategy();
            strategy.ExecuteInTransaction(
                this.Context,
                    operation: (context) =>
                    {
                        int[] years = new int[] { 2020, 2021, 2022 };
                        foreach (var year in years)
                        {
                            this.Context.Database.ExecuteSqlInterpolated($"UPDATE [Invoices] SET [Year] = {year} WHERE Year([Created]) = {year}");
                        }

                    }, verifySucceeded: (context) => { return this.Context.Invoices.Where(p => p.Year == 0).Count() == 0; });
        }
    }
}

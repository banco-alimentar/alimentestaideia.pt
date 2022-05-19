namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database;

using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class ConsolidateNifInDonationTable : BaseDatabaseTool
{
    public ConsolidateNifInDonationTable(ApplicationDbContext context, IUnitOfWork unitOfWork) : base(context, unitOfWork)
    {
    }

    public override void ExecuteTool()
    {
        var donations = this.Context.Donations
            .Where(p => p.Nif == null && p.PaymentStatus == PaymentStatus.Payed)
            .Select(p => p.Id)
            .ToList();

        foreach (var item in donations)
        {
            Donation donation = this.Context.Donations
                .Include(p => p.User)
                .Where(p => p.Id == item)
                .First();

            donation.Nif = donation.User.Nif;
            this.Context.Entry(donation).State = EntityState.Modified;
        }

        this.Context.SaveChanges();
    }
}

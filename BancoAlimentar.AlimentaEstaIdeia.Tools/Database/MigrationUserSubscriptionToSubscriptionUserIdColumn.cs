namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;

    public class MigrationUserSubscriptionToSubscriptionUserIdColumn : BaseDatabaseTool
    {
        public MigrationUserSubscriptionToSubscriptionUserIdColumn(
            ApplicationDbContext context,
            IUnitOfWork unitOfWork) : base(context, unitOfWork)
        {
        }

        public override void ExecuteTool()
        {
            // List<WebUserSubscriptions> subscriptions = this.Context.UsersSubscriptions
            //   .Include(p => p.Subscription)
            //   .Include(p => p.User)
            //   .ToList();

            // foreach (var item in subscriptions)
            // {
            //    item.Subscription.User = item.User;
            //    this.Context.Entry(item.Subscription).State = EntityState.Modified;
            // }

            // this.Context.SaveChanges();
        }
    }
}

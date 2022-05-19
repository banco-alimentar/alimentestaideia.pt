namespace BancoAlimentar.AlimentaEstaIdeia.Tools;

using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Easypay.Rest.Client.Api;
using Easypay.Rest.Client.Client;
using Microsoft.Extensions.Configuration;

public abstract class EasyPayTool : BaseDatabaseTool
{
    private readonly Configuration easypayConfig;
    private readonly IConfiguration configuration;

    protected EasyPayTool(
        ApplicationDbContext context, 
        IUnitOfWork unitOfWork,
        IConfiguration configuration) 
            : base(context, unitOfWork)
    {
        this.configuration = configuration;
        easypayConfig = new Configuration();
        easypayConfig.BasePath = configuration["Easypay:BaseUrl"] + "/2.0";
        easypayConfig.ApiKey.Add("AccountId", configuration["Easypay:AccountId"]);
        easypayConfig.ApiKey.Add("ApiKey", configuration["Easypay:ApiKey"]);
        easypayConfig.DefaultHeaders.Add("Content-Type", "application/json");
        easypayConfig.UserAgent = $" {GetType().Assembly.GetName().Name}/{GetType().Assembly.GetName().Version.ToString()}(Easypay.Rest.Client/{Configuration.Version})";
    }

    /// <summary>
    /// Gets the <see cref="SinglePaymentApi"/>.
    /// </summary>
    /// <returns>A reference to the <see cref="SinglePaymentApi"/>.</returns>
    public SinglePaymentApi GetSinglePaymentApi()
    {
        return new SinglePaymentApi(easypayConfig);
    }

    /// <summary>
    /// Gets the <see cref="SubscriptionPaymentApi"/>.
    /// </summary>
    /// <returns>A reference to the <see cref="SubscriptionPaymentApi"/>.</returns>
    public SubscriptionPaymentApi GetSubscriptionPaymentApi()
    {
        return new SubscriptionPaymentApi(easypayConfig);
    }
}

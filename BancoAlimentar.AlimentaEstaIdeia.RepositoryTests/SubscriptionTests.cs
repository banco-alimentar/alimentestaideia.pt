namespace BancoAlimentar.AlimentaEstaIdeia.RepositoryTests
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [TestClass()]
    public class SubscriptionTests
    {
        IConfiguration Configuration { get; set; }
        ServiceCollection ServiceCollection { get; set; }
        ServiceProvider ServiceProvider { get; set; }

        public SubscriptionTests()
        {
            ServiceCollection = new ServiceCollection();

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<SubscriptionTests>();

            Configuration = builder.Build();

            ServiceCollection.AddScoped<DonationRepository>();
            ServiceCollection.AddScoped<ProductCatalogueRepository>();
            ServiceCollection.AddScoped<FoodBankRepository>();
            ServiceCollection.AddScoped<DonationItemRepository>();
            ServiceCollection.AddScoped<InvoiceRepository>();
            ServiceCollection.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(
                   Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web")));

            ServiceProvider = ServiceCollection.BuildServiceProvider();

            Configuration easypayConfig = new Configuration();
            easypayConfig.BasePath = this.Configuration["Easypay:BaseUrl"] + "/2.0";
            easypayConfig.ApiKey.Add("AccountId", this.Configuration["Easypay:AccountId"]);
            easypayConfig.ApiKey.Add("ApiKey", this.Configuration["Easypay:ApiKey"]);
            easypayConfig.DefaultHeaders.Add("Content-Type", "application/json");
            easypayConfig.UserAgent = $" {GetType().Assembly.GetName().Name}/{GetType().Assembly.GetName().Version.ToString()}(Easypay.Rest.Client/{Easypay.Rest.Client.Client.Configuration.Version})";
            subscriptionPaymentApi = new SubscriptionPaymentApi(easypayConfig);
        }

        private SubscriptionPaymentApi subscriptionPaymentApi;

        [TestMethod]
        public void GetSubscriptionTest()
        {
            var response = subscriptionPaymentApi.SubscriptionGet();
        }

        [TestMethod]
        public void CreateSubscriptionTest()
        {
            PaymentSubscription subscription = new PaymentSubscription(
                Guid.NewGuid(),
                new SubscriptionCapture("my subscription1", Guid.NewGuid().ToString(), new SinglePaymentRequestCapture("Alimente esta ideia Donation", Guid.NewGuid().ToString())),
                DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                PaymentSubscription.CurrencyEnum.EUR,
                new Customer(default(Guid),
                    "customer1",
                    "email@customer.com",
                    "123456789",
                    "+351",
                    "123456789",
                    Guid.NewGuid().ToString(),
                    "PT"),
                Guid.NewGuid().ToString(),
                18.3d,
                PaymentSubscription.FrequencyEnum._1M,
                0,
                DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                false,
                true,
                0,
                PaymentSubscriptionMethodAvailable.Cc,
                null,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

            var response = subscriptionPaymentApi.SubscriptionPost(subscription);
            Assert.AreEqual(response.Status, "ok");
            Assert.AreEqual(response.Method.Status, PaymentRecurringResponseMethod.StatusEnum.Waiting);
            CollectionAssert.AllItemsAreNotNull(response.Messages);
            Assert.IsNotNull(response.Id);

            subscriptionPaymentApi.SubscriptionIdDelete(response.Id.ToString());
        }

        [TestMethod]
        public void GetSubscriptionFromIdTest()
        {
            PaymentSubscription subscription = new PaymentSubscription(
                Guid.NewGuid(),
                new SubscriptionCapture("my subscription1", Guid.NewGuid().ToString(), new SinglePaymentRequestCapture("Alimente esta ideia Donation", Guid.NewGuid().ToString())),
                DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                PaymentSubscription.CurrencyEnum.EUR,
                new Customer(default(Guid),
                    "customer1",
                    "email@customer.com",
                    "123456789",
                    "+351",
                    "123456789",
                    Guid.NewGuid().ToString(),
                    "PT"),
                Guid.NewGuid().ToString(),
                18.3d,
                PaymentSubscription.FrequencyEnum._1M,
                0,
                DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                false,
                true,
                0,
                PaymentSubscriptionMethodAvailable.Cc,
                null,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

            var response = subscriptionPaymentApi.SubscriptionPost(subscription);
            Assert.AreEqual(response.Status, "ok");
            Assert.AreEqual(response.Method.Status, PaymentRecurringResponseMethod.StatusEnum.Waiting);
            CollectionAssert.AllItemsAreNotNull(response.Messages);
            Assert.IsNotNull(response.Id);

            var subscriptionResponse = subscriptionPaymentApi.SubscriptionIdGet(response.Id.ToString());

            subscriptionPaymentApi.SubscriptionIdDelete(response.Id.ToString());
        }

        [TestMethod]
        public void PatchSubscriptionFromIdTest()
        {
            PaymentSubscription subscription = new PaymentSubscription(
                Guid.NewGuid(),
                new SubscriptionCapture("my subscription1", Guid.NewGuid().ToString(), new SinglePaymentRequestCapture("Alimente esta ideia Donation", Guid.NewGuid().ToString())),
                DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                PaymentSubscription.CurrencyEnum.EUR,
                new Customer(default(Guid),
                    "customer1",
                    "email@customer.com",
                    "123456789",
                    "+351",
                    "123456789",
                    Guid.NewGuid().ToString(),
                    "PT"),
                Guid.NewGuid().ToString(),
                18.3d,
                PaymentSubscription.FrequencyEnum._1M,
                0,
                DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                false,
                true,
                0,
                PaymentSubscriptionMethodAvailable.Cc,
                null,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

            var response = subscriptionPaymentApi.SubscriptionPost(subscription);
            Assert.AreEqual(response.Status, "ok");
            Assert.AreEqual(response.Method.Status, PaymentRecurringResponseMethod.StatusEnum.Waiting);
            CollectionAssert.AllItemsAreNotNull(response.Messages);
            Assert.IsNotNull(response.Id);

            double newAmount = 25.36d;

            PaymentSubscriptionPatchable patch = new PaymentSubscriptionPatchable();
            patch.Value = newAmount;

            subscriptionPaymentApi.SubscriptionIdPatch(response.Id.ToString(), patch);
            var subscriptionResponse = subscriptionPaymentApi.SubscriptionIdGet(response.Id.ToString());
            Assert.AreEqual(subscriptionResponse.Value, newAmount);
            subscriptionPaymentApi.SubscriptionIdDelete(response.Id.ToString());
        }
    }
}

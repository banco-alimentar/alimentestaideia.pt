namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;
    using Easypay.Rest.Client.Model;
    using System.Threading;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Api;

    [TestClass()]
    public class PaymentModelTests
    {
        IConfiguration Configuration { get; set; }
        ServiceCollection ServiceCollection { get; set; }

        ServiceProvider ServiceProvider { get; set; }

        public PaymentModelTests()
        {
            ServiceCollection = new ServiceCollection();

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<PaymentModelTests>();

            Configuration = builder.Build();

            ServiceCollection.AddScoped<DonationRepository>();
            ServiceCollection.AddScoped<ProductCatalogueRepository>();
            ServiceCollection.AddScoped<FoodBankRepository>();
            ServiceCollection.AddScoped<DonationItemRepository>();
            ServiceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
            ServiceCollection.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(
                   Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web")));

            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        [TestMethod()]
        public async Task EasyPayTest()
        {
            IUnitOfWork context = this.ServiceProvider.GetRequiredService<IUnitOfWork>();
            Donation temporalDonation = CreateTemporalDonation(context);
            PaymentModel paymnetModel = new PaymentModel(
                this.Configuration,
                this.ServiceProvider.GetRequiredService<IUnitOfWork>())
            {
                DonationId = temporalDonation.Id,
                Donation = temporalDonation,
            };

            try
            {
                PaymentSingle payment = new PaymentSingle(method: new PaymentMethod())
                {
                    Type = PaymentSingle.TypeEnum.Sale,
                    ExpirationTime = DateTime.UtcNow.AddDays(1).ToShortTimeString(),
                    Currency = PaymentSingle.CurrencyEnum.EUR,
                    Customer = new Customer()
                    {
                        Email = temporalDonation.User.Email,
                        Phone = temporalDonation.User.PhoneNumber,
                        FiscalNumber = temporalDonation.User.Nif,
                        Language = Thread.CurrentThread.CurrentUICulture.Name,
                    },
                    Value = (double)temporalDonation.ServiceAmount,
                };

                Configuration config = new Configuration();
                config.BasePath = Configuration["Easypay:BaseUrl"];
                config.ApiKey.Add("AccountId", Configuration["Easypay:AccountId"]);
                config.ApiKey.Add("ApiKey", Configuration["Easypay:ApiKey"]);

                SinglePaymentApi easyPayApi = new SinglePaymentApi(config);
                var headerParameters = new Multimap<string, string>();
                headerParameters.Add("Content-Type", "application/json");
                var apiResponse = await easyPayApi.AsynchronousClient.PostAsync<PaymentSingle>(
                    "/single",
                    new RequestOptions()
                    {
                        Data = payment,
                        HeaderParameters = headerParameters,
                    },
                    null,
                    CancellationToken.None);

                Assert.IsTrue(apiResponse.StatusCode == System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
            finally
            {
                context.DonationItem.RemoveRange(temporalDonation.DonationItems);
                context.Donation.Remove(temporalDonation);
                int affectedRows = context.Complete();
                Assert.IsTrue(affectedRows > 0);
            }


        }

        private Donation CreateTemporalDonation(IUnitOfWork context)
        {
            Donation result = new Donation()
            {
                DonationDate = DateTime.UtcNow,
                DonationItems = new List<DonationItem>(),
                FoodBank = context.FoodBank.GetById(2),
                Referral = "Testing",
                ServiceAmount = 23,
                User = context.User.FindUserById("0b93837a-7de9-4cfa-95a8-e4dc45d06be5"),
                WantsReceipt = false,
                PaymentStatus = PaymentStatus.WaitingPayment,
            };

            int count = 0;
            foreach (var item in context.ProductCatalogue.GetAll().ToList())
            {
                result.DonationItems.Add(new DonationItem()
                {
                    Donation = result,
                    ProductCatalogue = item,
                    Price = item.Cost,
                    Quantity = ++count,
                });
            }

            context.Donation.Add(result);
            int affectedRows = context.Complete();
            Assert.IsTrue(affectedRows > 0);

            return result;
        }
    }
}
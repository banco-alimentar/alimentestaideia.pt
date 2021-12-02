namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Reflection;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Delete old subscriptions.
    /// </summary>
    public class DeleteOldSubscriptionFunction
    {
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Default constructor for <see cref="DeleteOldSubscriptionFunction"/>.
        /// </summary>
        /// <param name="telemetryConfiguration">Telemetry configuration.</param>
        public DeleteOldSubscriptionFunction(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        [FunctionName("DeleteOldSubscriptionFunction")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer, ILogger log)
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                .AddJsonFile("appsettings.json",
                                optional: true,
                                reloadOnChange: true)
                .Build();
            var config = FunctionInitializer.GetUnitOfWork(Configuration, telemetryClient);
            
        }
    }
}

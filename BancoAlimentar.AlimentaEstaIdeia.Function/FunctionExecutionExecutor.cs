// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionExecutor.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>Resolves only the five known function classes and invokes their shared runners.</summary>
    public sealed class FunctionExecutionExecutor : IFunctionExecutionCommandExecutor, IFunctionExecutionSkipReporter
    {
        private readonly IServiceScopeFactory scopeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionExecutionExecutor"/> class.
        /// </summary>
        /// <param name="scopeFactory">Service scope factory.</param>
        public FunctionExecutionExecutor(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        /// <inheritdoc />
        public Task ExecuteAsync(string functionKey, CancellationToken cancellationToken = default)
        {
            return this.ExecuteAsync(CreateCommand(functionKey), cancellationToken);
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(FunctionExecutionCommand command, CancellationToken cancellationToken = default)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            cancellationToken.ThrowIfCancellationRequested();
            using IServiceScope scope = this.scopeFactory.CreateScope();
            await ExecuteInScopeAsync(scope.ServiceProvider, command, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task RecordSkippedAsync(FunctionExecutionCommand command, CancellationToken cancellationToken = default)
        {
            return this.ExecuteAsync(command, cancellationToken);
        }

        private static FunctionExecutionCommand CreateCommand(string functionKey)
        {
            return new FunctionExecutionCommand
            {
                FunctionKey = functionKey,
                CommandId = Guid.NewGuid().ToString("D"),
                RequestedAtUtc = DateTime.UtcNow,
                TriggerType = "AdminManualTrigger",
                TriggerSource = "AdminManualTrigger",
                CorrelationId = Guid.NewGuid().ToString("N"),
            };
        }

        private static Task ExecuteInScopeAsync(
            IServiceProvider serviceProvider,
            FunctionExecutionCommand command,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (command.FunctionKey)
            {
                case "GenerateDonationReportFunction":
                    return serviceProvider.GetRequiredService<GenerateDonationReportFunction>().RunManualAsync(command);
                case "GenerateSiteHealthReportFunction":
                    return serviceProvider.GetRequiredService<GenerateSiteHealthReportFunction>().RunManualAsync(command);
                case "DeleteOldSubscriptionFunction":
                    return serviceProvider.GetRequiredService<DeleteOldSubscriptionFunction>().RunManualAsync(command);
                case "MultiBancoPaymentNotificationFunction":
                    return serviceProvider.GetRequiredService<MultiBancoPaymentNotificationFunction>().RunManualAsync(command);
                case "UpdateSubscriptions":
                    return serviceProvider.GetRequiredService<UpdateSubscriptions>().RunManualAsync(command);
                default:
                    throw new InvalidOperationException("The function key is not present in the execution catalog.");
            }
        }
    }
}

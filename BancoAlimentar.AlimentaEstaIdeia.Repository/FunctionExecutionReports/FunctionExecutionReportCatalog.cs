// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportCatalog.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>Canonical catalog of Azure Functions reported by the application.</summary>
    public static class FunctionExecutionReportCatalog
    {
        private static readonly IReadOnlyList<FunctionExecutionFunctionDescriptor> descriptors =
            new ReadOnlyCollection<FunctionExecutionFunctionDescriptor>(new List<FunctionExecutionFunctionDescriptor>
            {
                new FunctionExecutionFunctionDescriptor("GenerateDonationReportFunction", "FunctionExecutionReport_GenerateDonationReport_Name", "FunctionExecutionReport_GenerateDonationReport_Description", "0 0 6 * * *", FunctionExecutionReportScopeKind.Tenant),
                new FunctionExecutionFunctionDescriptor("GenerateSiteHealthReportFunction", "FunctionExecutionReport_GenerateSiteHealthReport_Name", "FunctionExecutionReport_GenerateSiteHealthReport_Description", "0 0 7 * * *", FunctionExecutionReportScopeKind.Global),
                new FunctionExecutionFunctionDescriptor("DeleteOldSubscriptionFunction", "FunctionExecutionReport_DeleteOldSubscription_Name", "FunctionExecutionReport_DeleteOldSubscription_Description", "* * */24 * * *", FunctionExecutionReportScopeKind.Tenant),
                new FunctionExecutionFunctionDescriptor("MultiBancoPaymentNotificationFunction", "FunctionExecutionReport_MultiBancoPaymentNotification_Name", "FunctionExecutionReport_MultiBancoPaymentNotification_Description", "0 59 11 * * *", FunctionExecutionReportScopeKind.Tenant),
                new FunctionExecutionFunctionDescriptor("UpdateSubscriptions", "FunctionExecutionReport_UpdateSubscriptions_Name", "FunctionExecutionReport_UpdateSubscriptions_Description", "* * */24 * * *", FunctionExecutionReportScopeKind.Tenant),
            });

        /// <summary>Gets all known function descriptors.</summary>
        public static IReadOnlyList<FunctionExecutionFunctionDescriptor> All => descriptors;

        /// <summary>Finds a descriptor by stable key.</summary>
        /// <param name="functionKey">Function key.</param>
        /// <returns>Descriptor or null.</returns>
        public static FunctionExecutionFunctionDescriptor Find(string functionKey)
        {
            foreach (FunctionExecutionFunctionDescriptor descriptor in descriptors)
            {
                if (string.Equals(descriptor.FunctionKey, functionKey, System.StringComparison.Ordinal))
                {
                    return descriptor;
                }
            }

            return null;
        }
    }
}

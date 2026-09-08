// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportStorageResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;

    /// <summary>Typed result for execution report storage operations.</summary>
    public sealed class FunctionExecutionReportStorageResult
    {
        /// <summary>Gets or sets the operation state.</summary>
        public FunctionExecutionReportStorageState State { get; set; }

        /// <summary>Gets or sets the report, when available.</summary>
        public FunctionExecutionReport Report { get; set; }

        /// <summary>Gets or sets the latest pointer, when available.</summary>
        public FunctionExecutionReportLatestPointer LatestPointer { get; set; }

        /// <summary>Gets or sets the business outcome before storage failure.</summary>
        public FunctionExecutionReportOutcome BusinessOutcome { get; set; }

        /// <summary>Gets or sets a safe diagnostic.</summary>
        public string Diagnostic { get; set; }

        /// <summary>Gets or sets the storage exception for internal telemetry.</summary>
        public Exception Exception { get; set; }

        /// <summary>Gets or sets whether the report was persisted.</summary>
        public bool ReportPersisted { get; set; }

        /// <summary>Gets or sets the Azure Storage account name used for the report.</summary>
        public string StorageAccountName { get; set; }

        /// <summary>Gets or sets the Azure Blob Storage container name used for the report.</summary>
        public string StorageContainerName { get; set; }

        /// <summary>Gets or sets the exact blob path used for the report.</summary>
        public string StorageBlobPath { get; set; }

        /// <summary>Gets or sets the URI of the exact report blob without credentials.</summary>
        public string StorageBlobUri { get; set; }

        /// <summary>Creates a successful read result.</summary>
        public static FunctionExecutionReportStorageResult Success(FunctionExecutionReport report)
        {
            return new FunctionExecutionReportStorageResult
            {
                State = FunctionExecutionReportStorageState.Available,
                Report = report,
                ReportPersisted = true,
            };
        }
    }
}

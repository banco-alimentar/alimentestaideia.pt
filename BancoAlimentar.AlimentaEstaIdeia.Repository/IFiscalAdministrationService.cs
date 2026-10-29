// -----------------------------------------------------------------------
// <copyright file="IFiscalAdministrationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Manages tenant-bound fiscal issuer and series configuration.
    /// </summary>
    public interface IFiscalAdministrationService
    {
        /// <summary>Creates an issuer profile in the current trusted tenant scope.</summary>
        /// <param name="request">The non-secret issuer profile configuration.</param>
        /// <returns>The persisted issuer profile.</returns>
        FiscalIssuerProfile CreateIssuerProfile(FiscalIssuerProfileAdministrationRequest request);

        /// <summary>Updates an unused issuer profile in the current trusted tenant scope.</summary>
        /// <param name="request">The non-secret issuer profile configuration.</param>
        /// <returns>The persisted issuer profile.</returns>
        FiscalIssuerProfile UpdateIssuerProfile(FiscalIssuerProfileAdministrationRequest request);

        /// <summary>Creates or updates a communicated fiscal series.</summary>
        /// <param name="request">The series configuration and AT communication evidence.</param>
        /// <returns>The persisted fiscal series.</returns>
        FiscalDocumentSeries ConfigureSeries(FiscalDocumentSeriesAdministrationRequest request);

        /// <summary>Closes a fiscal series without deleting its historic evidence.</summary>
        /// <param name="fiscalDocumentSeriesId">The series to close.</param>
        /// <returns>The closed fiscal series.</returns>
        FiscalDocumentSeries CloseSeries(int fiscalDocumentSeriesId);

        /// <summary>Exports SAF-T from immutable fiscal snapshots for one issuer and period.</summary>
        /// <param name="fiscalIssuerProfileId">The issuer selected by the administrator.</param>
        /// <param name="startDate">The inclusive first invoice date.</param>
        /// <param name="endDate">The inclusive final invoice date.</param>
        /// <returns>The deterministic SAF-T XML.</returns>
        string ExportSaft(int fiscalIssuerProfileId, DateOnly startDate, DateOnly endDate);
    }
}

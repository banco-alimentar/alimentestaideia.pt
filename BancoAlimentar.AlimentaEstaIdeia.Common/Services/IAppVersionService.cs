// -----------------------------------------------------------------------
// <copyright file="IAppVersionService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common.Services
{
    /// <summary>
    /// Gets the application version.
    /// </summary>
    public interface IAppVersionService
    {
        /// <summary>
        /// Gets the version of the running app.
        /// </summary>
        string Version { get; }
    }
}

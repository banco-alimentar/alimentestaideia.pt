// -----------------------------------------------------------------------
// <copyright file="GlobalPageHandlerModelConvention.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Tenants
{
    using Microsoft.AspNetCore.Mvc.ApplicationModels;

    /// <summary>
    /// Global page.
    /// </summary>
    public class GlobalPageHandlerModelConvention : IPageHandlerModelConvention
    {
        /// <summary>
        /// Apply.
        /// </summary>
        /// <param name="model">Model.</param>
        public void Apply(PageHandlerModel model)
        {
        }
    }
}

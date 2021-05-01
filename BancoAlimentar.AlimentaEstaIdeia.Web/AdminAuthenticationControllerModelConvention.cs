// -----------------------------------------------------------------------
// <copyright file="AdminAuthenticationControllerModelConvention.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    using System.Linq;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.AspNetCore.Mvc.Authorization;

    public class AdminAuthenticationControllerModelConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            string adminArea;

            if (controller.RouteValues.Any()
                && controller.RouteValues.TryGetValue("area", out adminArea)
                && adminArea.Equals("admin"))
            {
                controller.Filters.Add(new AuthorizeFilter("AdminArea"));
            }
        }
    }
}

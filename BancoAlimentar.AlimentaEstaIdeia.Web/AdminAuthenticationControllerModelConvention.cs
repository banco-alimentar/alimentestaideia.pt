using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
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

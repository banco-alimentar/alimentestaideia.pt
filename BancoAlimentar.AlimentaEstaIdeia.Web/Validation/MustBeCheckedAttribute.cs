// -----------------------------------------------------------------------
// <copyright file="MustBeCheckedAttribute.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Validation
{
    using System.ComponentModel.DataAnnotations;

    public class MustBeCheckedAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return value != null && (bool)value;
        }
    }
}

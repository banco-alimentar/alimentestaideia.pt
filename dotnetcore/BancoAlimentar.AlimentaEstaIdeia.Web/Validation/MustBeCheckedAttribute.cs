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

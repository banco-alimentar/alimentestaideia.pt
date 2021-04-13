namespace BancoAlimentar.AlimentaEstaIdeia.Web.Validation
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class NifAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return ValidateNif((string)value);
        }

        public static bool ValidateNif(string nif)
        {
            if (nif == null)
            {
                return false;
            }

            if (nif.Equals("000000000"))
            {
                return true;
            }

            int checkDigit;
            char firstNumber;
            int result;

            // Verifica se é numerico e tem 9 digitos
            if (int.TryParse(nif, out result) && nif.Length == 9)
            {
                // primeiro número do NIF
                firstNumber = nif[0];

                // Verifica se o nif comeca por (1, 2, 5, 6, 8, 9) que são os valores possíveis para os NIF's em PT
                if (firstNumber.Equals('1') || firstNumber.Equals('2') || firstNumber.Equals('5') || firstNumber.Equals('6') || firstNumber.Equals('8') || firstNumber.Equals('9'))
                {
                    // Calcula o CheckDigit
                    checkDigit = (Convert.ToInt16(firstNumber.ToString()) * 9);
                    for (int i = 2; i <= 8; i++)
                    {
                        checkDigit += Convert.ToInt16(nif[i - 1].ToString()) * (10 - i);
                    }

                    checkDigit = 11 - (checkDigit % 11);

                    // Se checkDigit for superior a 10 passa a 0
                    if (checkDigit >= 10)
                    {
                        checkDigit = 0;
                    }

                    // Compara o digito de controle com o último numero do NIF
                    // Se igual, o NIF é válido.
                    if (checkDigit.ToString() == nif[8].ToString())
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

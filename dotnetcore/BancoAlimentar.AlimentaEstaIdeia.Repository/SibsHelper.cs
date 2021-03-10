﻿namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Sibs Multibanco helper.
    /// </summary>
    public class SibsHelper
    {

        public static string GetFileId(string previousFileId)
        {
            var currentDate = DateTime.Now.ToString("yyyyMMdd");
            var previousFileDate = previousFileId.Substring(0, 8);

            if (currentDate.Equals(previousFileDate))
            {
                return (int.Parse(previousFileId) + 1).ToString(CultureInfo.InvariantCulture);
            }

            return string.Format("{0}1", DateTime.Now.ToString("yyyyMMdd"));
        }

        /// <summary>
        /// Generate MultiBanco reference.
        /// </summary>
        /// <param name="entity">Entity id.</param>
        /// <param name="reference">Donation id.</param>
        /// <param name="amount">Donation amount.</param>
        /// <returns>Service reference.</returns>
        public static string GenerateReference(string entity, int reference, decimal amount)
        {
            var referenceString = reference.ToString(CultureInfo.InvariantCulture).PadLeft(7, '0');
            var amountString = amount.ToString("f2", CultureInfo.InvariantCulture).Replace(".", string.Empty).PadLeft(8, '0');

            var digits = string.Format("{0}{1}{2}", entity, referenceString, amountString);

            var checkDigits = 0;

            var count = digits.Count();
            for (var i = 0; i < count; i++)
            {
                var digit = Convert.ToInt32(digits.Substring(i, 1));
                var weight = Convert.ToInt32(Math.Pow(10, Convert.ToInt64(21 - i)) % 97);

                checkDigits += Convert.ToInt32(weight * digit);
            }

            var finalReference = string.Format("{0}{1}", referenceString,
                                               (98 - Convert.ToInt32(checkDigits) % 97).ToString("00", CultureInfo.InvariantCulture));

            return string.Format("{0} {1} {2}", finalReference.Substring(0, 3), finalReference.Substring(3, 3), finalReference.Substring(6, 3));
        }
    }
}
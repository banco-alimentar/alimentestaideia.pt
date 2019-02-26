using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Link.BA.Donate.Business
{
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
                var weight = Convert.ToInt32(Math.Pow(10, Convert.ToInt64(21 - i))%97);

                checkDigits += Convert.ToInt32(weight*digit);
            }

            var finalReference = string.Format("{0}{1}", referenceString,
                                               (98 - Convert.ToInt32(checkDigits) % 97).ToString("00", CultureInfo.InvariantCulture));

            return string.Format("{0} {1} {2}", finalReference.Substring(0, 3), finalReference.Substring(3, 3), finalReference.Substring(6, 3));
        }

        public static string GenerateReferenceAngola(string entity, int reference, decimal amount)
        {
            int[] weights = { 31, 71, 75, 56, 25, 51, 73, 17, 89, 38, 62, 45, 53, 15, 50, 5, 49, 34, 81, 76, 27, 90, 9, 30, 3 };

            var referenceString = reference.ToString(CultureInfo.InvariantCulture).PadLeft(7, '0');
            var amountString = amount.ToString("f2", CultureInfo.InvariantCulture).Replace(".", string.Empty).PadLeft(13, '0');

            var digits = string.Format("{0}{1}{2}", entity, referenceString, amountString);

            var checkDigits = 0;

            var count = digits.Count();
            for (var i = 0; i < count; i++)
            {
                var digit = Convert.ToInt32(digits.Substring(i, 1));
                var weight = weights[i];

                checkDigits += Convert.ToInt32(weight * digit);
            }

            var finalReference = string.Format("{0}{1}", referenceString,
                                               (98 - Convert.ToInt32(checkDigits) % 97).ToString("00", CultureInfo.InvariantCulture));

            return string.Format("{0} {1} {2}", finalReference.Substring(0, 3), finalReference.Substring(3, 3), finalReference.Substring(6, 3));
        }
    }
}

using System;
using System.Security.Cryptography;

namespace CaptchaMVC.HtmlHelpers
{
    /// <summary>
    /// Generates truly random numbers
    /// </summary>
    internal static class RandomNumber
    {
        private static readonly byte[] Randb = new byte[4];

        private static readonly RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider();

        /// <summary>
        /// Generate a positive random number
        /// </summary>
        /// <returns></returns>
        private static int Next()
        {
            Rand.GetBytes(Randb);
            var value = BitConverter.ToInt32(Randb, 0);

            return Math.Abs(value);
        }

        /// <summary>
        /// Generate a positive random number
        /// </summary>
        /// <param name="max">Maximum</param>
        /// <returns></returns>
        public static int Next(int max)
        {
            return Next() % (max + 1);
        }

        /// <summary>
        /// Generate a positive random number
        /// </summary>
        /// <param name="min">Minimum</param>
        /// <param name="max">Maximum</param>
        /// <returns></returns>
        public static int Next(int min, int max)
        {
            return Next(max - min) + min;
        }
    }
}

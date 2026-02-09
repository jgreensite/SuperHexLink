// Credit: https://scottlilly.com/create-better-random-numbers-in-c/
using System;
using System.Security.Cryptography;

namespace FDL.Library.Numeric
{
    /// <summary>
    /// Cryptographically-strong random number helper.
    /// Uses <see cref="RandomNumberGenerator"/> (the modern, non-deprecated API).
    /// </summary>
    public static class RandomNumber
    {
        /// <summary>
        /// Returns a cryptographically-random integer in the inclusive range
        /// [<paramref name="minimumValue"/>, <paramref name="maximumValue"/>].
        /// </summary>
        public static int Between(int minimumValue, int maximumValue)
        {
            if (minimumValue > maximumValue)
                throw new ArgumentOutOfRangeException(nameof(minimumValue),
                    "minimumValue must be <= maximumValue");

            if (minimumValue == maximumValue)
                return minimumValue;

            byte[] randomNumber = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            // Convert to a positive 31-bit value and map into the desired range.
            int value = BitConverter.ToInt32(randomNumber, 0) & int.MaxValue;
            int range = maximumValue - minimumValue + 1;
            return minimumValue + (value % range);
        }
    }
}
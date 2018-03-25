using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace System
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNotNullOrEmpty(this string value)
        {
            return !IsNullOrEmpty(value);
        }


        public static string TruncateString(this string value, int maxLength)
        {
            return value.TruncateString(maxLength, true);
        }

        public static string TruncateString(this string value, int maxLength, bool appendDots)
        {
            if (value.IsNotNullOrEmpty())
            {
                if (appendDots)
                {
                    return value.Substring(0, Math.Min(value.Length, maxLength)) + "...";
                }
                else
                {
                    return value.Substring(0, Math.Min(value.Length, maxLength));
                }
            }
            return value;
        }

        /// <summary>
        /// Accepts a string value and returns a Boolean if a conversion succeeds else returns null
        /// </summary>
        /// <param name="value">String value to be converted</param>
        /// <returns>A Nullable Of Boolean</returns>
        public static Nullable<bool> AsBoolean(this string value)
        {
            bool res;
            if (bool.TryParse(value, out res))
                return res;
            else
                return null;
        }

        /// <summary>
        /// Accepts a string value and returns a guid if a conversion succeeds else returns null
        /// </summary>
        /// <param name="value">String value to be converted</param>
        /// <returns>A Nullable Of Guid</returns>
        public static Nullable<Guid> AsGuid(this string value)
        {
            Guid result;

            if (Guid.TryParse(value, out result))
                return result;

            return null;
        }

        /// <summary>
        /// Accepts a string value and returns an integer if a conversion succeeds else returns null
        /// </summary>
        /// <param name="value">String value to be converted</param>
        /// <returns>A Nullable Of integer</returns>
        public static Nullable<int> AsInteger(this string value)
        {
            int result;

            if (int.TryParse(value, out result))
                return result;

            return null;
        }

        public static double? AsDouble(this string value)
        {
            double result;

            if (double.TryParse(value, out result))
                return result;

            return null;
        }

        /// <summary>
        /// Accepts a string value and returns a decimal if a conversion succeeds else returns null
        /// </summary>
        /// <param name="value">String value to be converted</param>
        /// <returns>A Nullable Of decimal</returns>
        public static Nullable<decimal> AsDecimal(this string value)
        {
            decimal result;

            if (decimal.TryParse(value, out result))
                return result;

            return null;
        }


        public static long? AsLong(this string value)
        {
            long result;

            if (long.TryParse(value, out result))
                return result;

            return null;
        }

    }
}

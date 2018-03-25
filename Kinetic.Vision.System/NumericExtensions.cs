using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class NumericExtensions
    {
        public static string ToOrdinal(this long number)
        {

            if (number < 0) return number.ToString();
            long rem = number % 100;
            if (rem >= 11 && rem <= 13) return number + "th";
            switch (number % 10)
            {
                case 1:
                    return number + "st";
                case 2:
                    return number + "nd";
                case 3:
                    return number + "rd";
                default:
                    return number + "th";
            }
        }

        public static string ToOrdinal(this int number)
        {
            return ((long)number).ToOrdinal();
        }

        public static string ToOrdinal(this string number)
        {
            if (String.IsNullOrEmpty(number)) return number;
            var dict = new Dictionary<string, string>();
            dict.Add("zero", "zeroth");
            dict.Add("nought", "noughth");
            dict.Add("one", "first");
            dict.Add("two", "second");
            dict.Add("three", "third");
            dict.Add("four", "fourth");
            dict.Add("five", "fifth");
            dict.Add("six", "sixth");
            dict.Add("seven", "seventh");
            dict.Add("eight", "eighth");
            dict.Add("nine", "ninth");
            dict.Add("ten", "tenth");
            dict.Add("eleven", "eleventh");
            dict.Add("twelve", "twelfth");
            dict.Add("thirteen", "thirteenth");
            dict.Add("fourteen", "fourteenth");
            dict.Add("fifteen", "fifteenth");
            dict.Add("sixteen", "sixteenth");
            dict.Add("seventeen", "seventeenth");
            dict.Add("eighteen", "eighteenth");
            dict.Add("nineteen", "nineteenth");
            dict.Add("twenty", "twentieth");
            dict.Add("thirty", "thirtieth");
            dict.Add("forty", "fortieth");
            dict.Add("fifty", "fiftieth");
            dict.Add("sixty", "sixtieth");
            dict.Add("seventy", "seventieth");
            dict.Add("eighty", "eightieth");
            dict.Add("ninety", "ninetieth");
            dict.Add("hundred", "hundredth");
            dict.Add("thousand", "thousandth");
            dict.Add("million", "millionth");
            dict.Add("billion", "billionth");
            dict.Add("trillion", "trillionth");
            dict.Add("quadrillion", "quadrillionth");
            dict.Add("quintillion", "quintillionth");
            // rough check whether it's a valid number
            string temp = number.ToLower().Trim().Replace(" and ", " ");
            string[] words = temp.Split(new char[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words)
            {
                if (!dict.ContainsKey(word)) return number;
            }
            // extract last word
            number = number.TrimEnd().TrimEnd('-');
            int index = number.LastIndexOfAny(new char[] { ' ', '-' });
            string last = number.Substring(index + 1);
            // make replacement and maintain original capitalization
            if (last == last.ToLower())
            {
                last = dict[last];
            }
            else if (last == last.ToUpper())
            {
                last = dict[last.ToLower()].ToUpper();
            }
            else
            {
                last = last.ToLower();
                last = Char.ToUpper(dict[last][0]) + dict[last].Substring(1);
            }
            return number.Substring(0, index + 1) + last;
        }
    }
}

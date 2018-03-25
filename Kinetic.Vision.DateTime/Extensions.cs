using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class Extensions
    {
        private const double HoursInYear = 8765.81;

        public static System.DateTime ToUniversalTime(this System.DateTime value, string SourceTimeZone)
        {
            return TimeZoneInfo.ConvertTimeToUtc(value, TimeZoneInfo.FindSystemTimeZoneById(SourceTimeZone));
        }

        /// <summary>
        /// Accepts a Nullable birthday and returns an age
        /// </summary>
        /// <param name="value">Nullable birthday</param>
        /// <param name="defaultValue">A default if value is null</param>
        /// <returns>A string expressing the relative age</returns>
        public static string ToStringAge(this Nullable<System.DateTime> value, string defaultValue)
        {
            if (value != null)
            {
                return Extensions.ToStringAge(value.Value);
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Accepts a birthday and returns an age
        /// </summary>
        /// <param name="value">birthday</param>
        /// <returns>A string expressing the relative age</returns>
        public static string ToStringAge(this System.DateTime value)
        {
            return string.Format("{0:0} Years old", (System.DateTime.Now - value).TotalHours / HoursInYear);
        }

        public static int ToIntAge(this Nullable<System.DateTime> value)
        {
            if (value != null)
            {
                return Extensions.ToIntAge(value.Value);
            }
            else
            {
                return 0;
            }
        }

        public static int ToIntAge(this System.DateTime value)
        {
            return (int)((System.DateTime.Now - value).TotalHours / HoursInYear);
        }

        public static string ToShortAusDate(this Nullable<System.DateTime> value)
        {
            if (value != null)
                return Extensions.ToShortAusDate(value.Value);
            else
                return string.Empty;
        }

        public static string ToShortAusDate(this System.DateTime value)
        {
            return string.Format("{0:dd/MM/yyyy}", value);
        }

        public static string ToShortAusDateTime(this System.DateTime value)
        {
            return string.Format("{0:dd/MM/yyyy HH:mm}", value);
        }

        /// <summary>
        /// Converts a DateTime to a double representing the equivalent Epoch time.
        /// </summary>
        /// <param name="value">DateTime</param>
        /// <returns>Equivalent Epoch time</returns>
        public static double ToEpoch(this System.DateTime value)
        {
            TimeSpan span = (value.ToUniversalTime() - new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            return span.TotalMilliseconds;
        }

        public static bool Intersect(this Nullable<System.DateTime> StartDate, Nullable<System.DateTime> DateRangeStart, Nullable<System.DateTime> DateRangeEnd)
        {
            if (StartDate.HasValue && DateRangeStart.HasValue && DateRangeEnd.HasValue)
            {
                return Intersect(StartDate.Value, DateRangeStart.Value, DateRangeEnd.Value);
            }
            else
            {
                return false;
            }
        }

        public static bool Intersect(this System.DateTime StartDate, System.DateTime DateRangeStart, System.DateTime DateRangeEnd)
        {
            return Intersect(StartDate, StartDate, DateRangeStart, DateRangeEnd);
        }

        public static bool Intersect(this System.DateTime start1, System.DateTime end1, System.DateTime start2, System.DateTime end2)
        {
            return (start1 == start2) || (start1 > start2 ? start1 <= end2 : start2 <= end1);
        }

        public static bool IsWeekend(this System.DateTime value)
        {
            return (value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday);
        }
    }

}

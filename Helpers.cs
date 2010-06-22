using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimeStamper.DSL;

namespace WorldCupBot
{
    // some helpers for various things
    // this isn't the ideal way to include these, but stuck for time, it works.
    // TODO: might be a good idea to implement them similarly to the trigger/functions.
    public class Helpers
    {
        /// <summary>
        /// Time-related functions
        /// </summary>
        public class Time
        {

            /// <summary>
            /// Parse a date string ("in 1 day", etc.)
            /// </summary>
            /// <param name="text">The human time-frame</param>
            /// <returns>A date-range of possible matches</returns>
            public static DateRange ParseDate(string text)
            {
                var parser = new Parser(new Lexer(new StringCharacterBuffer(text, 3)));

                if (parser.Errors.Count > 0) throw new Exception(parser.Errors[0]);
                var dateRange = parser.Eval();
                if (dateRange.Dates.Count == 0) throw new Exception("Dates not parsed");
                return dateRange;
            }
            
            /// <summary>
            /// Provides a human-readadble difference between a time and now.
            /// </summary>
            /// <param name="then">The DateTime to compare to</param>
            /// <returns>Human-readable difference</returns>
            public static string ReadableDifference(DateTime then)
            {
                long ticksThen = then.Ticks;
                long ticksNow = DateTime.UtcNow.Ticks;

                TimeSpan difference;
                bool future = false;

                if (ticksThen > ticksNow)
                {
                    future = true;
                    difference = new TimeSpan(ticksThen - ticksNow);
                }
                else 
                {
                    future = false;
                    difference = new TimeSpan(ticksNow - ticksThen);
                }

                int days = (int)Math.Floor(difference.TotalDays);

                int hours = (days == 0 ? (int)Math.Floor(difference.TotalHours) : difference.Hours);

                int minutes = (hours == 0 && days == 0 ? (int)Math.Floor(difference.TotalMinutes) : difference.Minutes);

                List<string> components = new List<string>();

                if (days != 0)
                    components.Add(days + " day" + (days == 1 ? "" : "s"));

                if (hours != 0)
                    components.Add(hours + " hour" + (hours == 1 ? "" : "s"));

                if (minutes != 0)
                    components.Add(minutes + " minute" + (minutes == 1 ? "" : "s"));

                string val = string.Join(", ", components.ToArray());;

                return (future ? "in " + val : val + " ago");

            }

        }

    }
}

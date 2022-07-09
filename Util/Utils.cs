namespace Ja3farBot.Util
{
    public class Utils
    {
        public static DateTimeSpan GetDuration(DateTime DateTime1, DateTime DateTime2)
        {
            int s = DateTime2.Second - DateTime1.Second;
            int m = DateTime2.Minute - DateTime1.Minute;
            int h = DateTime2.Hour - DateTime1.Hour;
            int D = DateTime2.Day - DateTime1.Day;
            int M = DateTime2.Month - DateTime1.Month;
            int Y = DateTime2.Year - DateTime1.Year;

            if (s < 0)
            {
                s += 60;
                m--;
            }
            if (m < 0)
            {
                m += 60;
                h--;
            }
            if (h < 0)
            {
                h += 24;
                D--;
            }
            if (D < 0)
            {
                switch (DateTime1.Month)
                {
                    case 1:
                    case 3:
                    case 5:
                    case 7:
                    case 8:
                    case 10:
                    case 12:
                        D += 31;
                        break;
                    case 4:
                    case 6:
                    case 9:
                    case 11:
                        D += 30;
                        break;
                    case 2:
                        if (DateTime1.Year % 4 == 0) D += 29;
                        else D += 28;
                        break;
                }
                M--;
            }
            if (M < 0)
            {
                M += 12;
                Y--;
            }

            return new()
            {
                Seconds = s,
                Minutes = m,
                Hours = h,
                Days = D,
                Months = M,
                Years = Y
            };
        }
    }

    public class DateTimeSpan
    {
        public int Years { get; set; }
        public int Months { get; set; }
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }

        public override string ToString()
        {
            string Y;
            string M;
            string D;
            string h;
            string m;
            string s;
            string result;

            if (Years == 0 && Months == 0 && Days == 0 && Hours == 0 && Minutes == 0)
            {
                if (Seconds == 1) s = $"{Seconds} second";
                else s = $"{Seconds} seconds";

                result = s;
            }
            else if (Years == 0 && Months == 0 && Days == 0 && Hours == 0)
            {
                if (Seconds == 1) s = $"{Seconds} second";
                else s = $"{Seconds} seconds";
                if (Minutes == 1) m = $"{Minutes} minute";
                else m = $"{Minutes} minutes";
                result = $"{m} & {s}";
            }
            else if (Years == 0 && Months == 0 && Days == 0)
            {
                if (Seconds == 1) s = $"{Seconds} second";
                else s = $"{Seconds} seconds";
                if (Minutes == 1) m = $"{Minutes} minute";
                else m = $"{Minutes} minutes";
                if (Hours == 1) h = $"{Hours} hour";
                else h = $"{Hours} hours";
                result = $"{h}, {m} & {s}";
            }
            else if (Years == 0 && Months == 0)
            {
                if (Minutes == 1) m = $"{Minutes} minute";
                else m = $"{Minutes} minutes";
                if (Hours == 1) h = $"{Hours} hour";
                else h = $"{Hours} hours";
                if (Days == 1) D = $"{Days} day";
                else D = $"{Days} days";
                result = $"{D}, {h} & {m}";
            }
            else if (Years == 0)
            {
                if (Hours == 1) h = $"{Hours} hour";
                else h = $"{Hours} hours";
                if (Days == 1) D = $"{Days} day";
                else D = $"{Days} days";
                if (Months == 1) M = $"{Months} month";
                else M = $"{Months} months";
                result = $"{M}, {D} & {h}";
            }
            else
            {
                if (Days == 1) D = $"{Days} day";
                else D = $"{Days} days";
                if (Months == 1) M = $"{Months} month";
                else M = $"{Months} months";
                if (Years == 1) Y = $"{Years} year";
                else Y = $"{Years} years";
                result = $"{Y}, {M} & {D}";
            }
            return result + " ago.";
        }
    }
}

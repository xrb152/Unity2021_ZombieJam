using System;
using System.Globalization;

namespace Utils.Extensions
{
    public static class FormatTime
    {
        public static string HoursStringFormat(int totalHours)
        {
            var hours = totalHours / 3600;
            var minutes = totalHours % 3600 / 60;
            var seсonds = totalHours % 60;
            return $"{hours}小时{minutes}分";
        }

        public static string DayAndHoursToString(int totalSeconds)
        {
            var days = totalSeconds / 86400;
            var hours = totalSeconds % 86400 / 3600;
            
            return $"{days}天{hours}小时";
        }
        
        public static string MinutesStringFormat(int totalSeconds)
        {
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;

            return $"{minutes}分{seconds}秒";
        }
        
        public static int GetSecondsFromHoursInt(int totalHours)
        {
            return totalHours * 3600;
        }
        
        public static int GetSecondsFromMinutesInt(int totalMinutes)
        {
            return totalMinutes * 60;
        }

        public static DateTime StrToDateTime(string strTime)
        {
            var aaa = Convert.ToDateTime(strTime);
            return aaa;
        }
        
        public static string DateTimeToStr(DateTime time)
        {
            return time.ToString(CultureInfo.CurrentCulture);
        }
    }
}
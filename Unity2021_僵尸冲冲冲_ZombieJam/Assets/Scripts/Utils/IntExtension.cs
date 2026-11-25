using System;
using System.Globalization;
using UnityEngine;

namespace Utils
{
    public static class IntExtension
    {
        public static bool ToBool(this int value)
        {
            return value > 0 ? true : false;
        }

        public static string ToFormattedBigNumber(this int number)
        {
            return GetFormattedInt(number);
        }

        public static string ToSpaceBetweenChars(this int number, bool isForced = false)
        {
            if (!isForced && Mathf.Abs(number) < 100)
            {
                return number.ToString();
            }
            
            return string.Format(CultureInfo.InvariantCulture, "{0:0,#}", number).Replace(",", " ");
        }

        // 全部改成中文单位（万 / 亿 / 万亿），界面上不再出现 K、M、B 这类英文字母
        private static string GetFormattedInt(int value)
        {
            if (value < 0)
            {
                return "-" + GetFormattedInt(-value);
            }

            if (value < 10000)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            if (value < 100000000)
            {
                return FormatWithUnit(value / 10000d, "万");
            }

            if (value < 1000000000000d)
            {
                return FormatWithUnit(value / 100000000d, "亿");
            }

            return FormatWithUnit(value / 1000000000000d, "万亿");
        }

        private static string FormatWithUnit(double number, string unit)
        {
            // 保留一位小数，整数不显示 .0（如 12345 -> 1.2万，20000 -> 2万）
            return number.ToString("0.#", CultureInfo.InvariantCulture) + unit;
        }
        
        public static int GetDigitsCount(this int n)
        {
            if (n >= 0)
            {
                if (n < 10L) return 1;
                if (n < 100L) return 2;
                if (n < 1000L) return 3;
                if (n < 10000L) return 4;
                if (n < 100000L) return 5;
                if (n < 1000000L) return 6;
                if (n < 10000000L) return 7;
                if (n < 100000000L) return 8;
                if (n < 1000000000L) return 9;
                //if (n < 10000000000L) return 10;
                //if (n < 100000000000L) return 11;
                //if (n < 1000000000000L) return 12;
                //if (n < 10000000000000L) return 13;
                //if (n < 100000000000000L) return 14;
                //if (n < 1000000000000000L) return 15;
                //if (n < 10000000000000000L) return 16;
                //if (n < 100000000000000000L) return 17;
                //if (n < 1000000000000000000L) return 18;
                return 19;
            }
            else
            {
                if (n > -10L) return 2;
                if (n > -100L) return 3;
                if (n > -1000L) return 4;
                if (n > -10000L) return 5;
                if (n > -100000L) return 6;
                if (n > -1000000L) return 7;
                if (n > -10000000L) return 8;
                if (n > -100000000L) return 9;
                if (n > -1000000000L) return 10;
                //if (n > -10000000000L) return 11;
                //if (n > -100000000000L) return 12;
                //if (n > -1000000000000L) return 13;
                //if (n > -10000000000000L) return 14;
                //if (n > -100000000000000L) return 15;
                //if (n > -1000000000000000L) return 16;
                //if (n > -10000000000000000L) return 17;
                //if (n > -100000000000000000L) return 18;
                //if (n > -1000000000000000000L) return 19;
                return 20;
            }
        }
    }
}
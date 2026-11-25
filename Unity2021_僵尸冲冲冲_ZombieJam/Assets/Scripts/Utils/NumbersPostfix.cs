namespace Utils
{
    /// <summary>
    /// 数字单位后缀。界面要求全部简体中文，禁止出现 K / M / B 这类英文字母。
    /// 主流程已改为按中文习惯的 万 / 亿 换算（见 IntExtension.GetFormattedInt），
    /// 这里按每 3 位一级给出中文单位，仅作兼容保留。
    /// </summary>
    public static class NumbersPostfix
    {
        private static readonly string[] _postfixes =
        {
            "",
            "千",
            "百万",
            "十亿",
            "万亿",
            "亿亿",
            "万亿亿",
            "亿亿亿",
            "万亿亿亿",
            "亿亿亿亿",
        };

        public static string GetPostfix(int index)
        {
            if (index < 0)
            {
                return string.Empty;
            }

            return index < _postfixes.Length ? _postfixes[index] : _postfixes[_postfixes.Length - 1];
        }
    }
}

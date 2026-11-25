namespace TTSDK
{
    [System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
    public static class TTSDKType
    {
        public static bool IsMixEngine()
        {
#if TTSDK_MIX_ENGINE
            return true;
#endif
            return false;
        }
    }
}
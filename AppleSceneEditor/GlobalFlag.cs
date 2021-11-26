namespace AppleSceneEditor
{
    public static class GlobalFlag
    {
        private static ulong _flag;

        public static void SetFlag(GlobalFlags flag, bool value)
        {
            ulong longFlag = (ulong) flag;

            _flag = value ? _flag | longFlag : _flag & ~longFlag;
        }
        
        public static bool IsFlagRaised(GlobalFlags flag)
        {
            ulong longFlag = (ulong) flag;

            return (_flag & longFlag) == longFlag;
        }
        
        public static void ToggleFlag(GlobalFlags flag) => _flag ^= (ulong) flag;
    }
}
namespace YANF.Script
{
    public static class YANConstant
    {
        public const int W_UPDATE_SCR = 360;

        public enum AnimateWindowFlags
        {
            AW_HOR_POSITIVE = 0x00000001,
            AW_HOR_NEGATIVE = 0x00000002,
            AW_VER_POSITIVE = 0x00000004,
            AW_VER_NEGATIVE = 0x00000008,
            AW_CENTER = 0x00000010,
            AW_HIDE = 0x00010000,
            AW_ACTIVATE = 0x00020000,
            AW_SLIDE = 0x00040000,
            AW_BLEND = 0x00080000
        }

        public enum PrgTextPosition
        {
            Left,
            Right,
            Center,
            Sliding,
            None
        }

        public enum MsgBoxLang
        {
            VIE,
            JAP
        }
    }
}
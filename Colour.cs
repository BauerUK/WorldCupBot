using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldCupBot
{
    public static class Colour
    {
        public const String COLOR = "\u0003";
        public const String NORMAL = "\u000f";
        public const String BOLD = "\u0002";
        public const String UNDERLINE = "\u001f";
        public const String REVERSE = "\u0016";
        public const String WHITE = "00";
        public const String BLACK = "01";
        public const String DARK_BLUE = "02";
        public const String DARK_GREEN = "03";
        public const String RED = "04";
        public const String BROWN = "05";
        public const String PURPLE = "06";
        public const String OLIVE = "07";
        public const String YELLOW = "08";
        public const String GREEN = "09";
        public const String TEAL = "10";
        public const String CYAN = "11";
        public const String BLUE = "12";
        public const String MAGENTA = "13";
        public const String DARK_GRAY = "14";
        public const String LIGHT_GRAY = "15";

        public static string MakeColour(string colour)
        {
            return String.Concat(Colour.COLOR,colour);
        }

        public static string MakeColour(string colour, string background)
        {
            return MakeColour(string.Concat(colour, ",", background));
        }

    }
}

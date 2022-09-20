namespace SimpleButton
{
    public struct HsvColor
    {
        public byte B;
        public byte G;
        public byte R;

        /// <summary>
        /// Creates a color depending on Hue Saturation and Value.
        /// </summary>
        /// <param name="hue">Value between 0 and 260</param>
        /// <param name="saturation">Value between 0 and 1</param>
        /// <param name="value">Value between 0 and 1</param>
        /// <returns>The HsvColor matching the input values.</returns>
        public static HsvColor FromHsv(double hue, double saturation, float value)
        {
            HsvToRgb(hue, saturation, value, out var red, out var green, out var blue);
            return new HsvColor { R = red, G = green, B = blue };
        }

        public static void HsvToRgb(double hue, double saturation, double value, out byte red, out byte green, out byte blue)
        {
            hue = GetSanitizedHue(hue);

            HsvToRgbDoubles(hue, saturation, value, out var redDouble, out var greenDouble, out var blueDouble);

            red = CheckLimitValues((int)(redDouble * 255));
            green = CheckLimitValues((int)(greenDouble * 255));
            blue = CheckLimitValues((int)(blueDouble * 255));
        }

        private static void HsvToRgbDoubles(double hue, double saturation, double value, out double red, out double green, out double blue)
        {
            if (value <= 0)
            {
                red = green = blue = 0;
            }
            else if (saturation <= 0)
            {
                red = green = blue = value;
            }
            else
            {
                var hf = hue / 60.0;
                var i = (int)hf;
                var f = hf - i;
                var pv = value * (1 - saturation);
                var qv = value * (1 - saturation * f);
                var tv = value * (1 - saturation * (1 - f));
                switch (i)
                {
                    case 0:
                        red = value;
                        green = tv;
                        blue = pv;
                        break;

                    case 1:
                        red = qv;
                        green = value;
                        blue = pv;
                        break;
                    case 2:
                        red = pv;
                        green = value;
                        blue = tv;
                        break;

                    case 3:
                        red = pv;
                        green = qv;
                        blue = value;
                        break;
                    case 4:
                        red = tv;
                        green = pv;
                        blue = value;
                        break;

                    case 5:
                        red = value;
                        green = pv;
                        blue = qv;
                        break;

                    case 6:
                        red = value;
                        green = tv;
                        blue = pv;
                        break;
                    case -1:
                        red = value;
                        green = pv;
                        blue = qv;
                        break;

                    default:
                        red = green = blue = value;
                        break;
                }
            }
        }

        private static double GetSanitizedHue(double hue)
        {
            while (hue < 0) hue += 360;
            while (hue >= 360) hue -= 360;
            return hue;
        }

        /// <summary>
        ///     Clamp a value to 0-255
        /// </summary>
        private static byte CheckLimitValues(int i)
        {
            return i switch
            {
                < 0 => 0,
                > 255 => 255,
                _ => (byte)i
            };
        }
    }
}
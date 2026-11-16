using System.Globalization;

namespace Assets._Project.Develop.Runtime.Utilities.Text
{
    // Число для текста: не больше двух знаков после разделителя, без лишних нулей — 2, 1,5, 0,75 (или 1.5, 0.75).
    // Разделитель задаёт язык текста, поэтому его передают снаружи.
    public static class DecimalText
    {
        private const string TwoDecimalsFormat = "0.##";

        public static string Format(float value, string decimalSeparator)
        {
            NumberFormatInfo format = new() { NumberDecimalSeparator = decimalSeparator };

            return value.ToString(TwoDecimalsFormat, format);
        }
    }
}

namespace Assets._Project.Develop.Runtime.Utilities.Text
{
    // Форма слова после числа по правилам английского языка: 1 level, 0 levels, 2 levels, 21 levels.
    public static class EnglishPlural
    {
        private const int Single = 1;

        public static string Select(int number, string one, string other) => number == Single ? one : other;
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Assets._Project.Develop.Runtime.Utilities.Text;
using TMPro;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.Localization
{
    // Язык игры и перевод строк. Исходный язык текстов — русский: префабы, конфиги и код хранят русский текст, он же —
    // ключ таблицы перевода. Язык определяется один раз при создании сервиса и до перезапуска не меняется, поэтому
    // открытые тексты обновлять не нужно. Строка без перевода остаётся русской; в редакторе о ней — одно
    // предупреждение, кроме исключений таблицы и строк без русских букв (числа, значки).
    public class LocalizationService
    {
        private const string SourceDecimalSeparator = ",";
        private const string EnglishDecimalSeparator = ".";
        private const int OneFormIndex = 0;
        private const int OtherFormIndex = 1;

        private static readonly Regex _sourceLanguageLetter = new("[А-Яа-яЁё]");

        private readonly TranslationTable _table;
        private readonly HashSet<string> _reportedMissing = new();

        public LocalizationService(ILanguageSource languageSource, TranslationTable table)
        {
            _table = table;

            CurrentLanguage = languageSource.GetLanguage();
        }

        public Language CurrentLanguage { get; }

        public bool IsSourceLanguage => CurrentLanguage == Language.Russian;

        public string DecimalSeparator => IsSourceLanguage ? SourceDecimalSeparator : EnglishDecimalSeparator;

        public string Translate(string source)
        {
            if (IsSourceLanguage || string.IsNullOrEmpty(source))
                return source;

            if (_table.TryGetTranslation(source, out string translation))
                return translation;

            ReportMissing(source);

            return source;
        }

        public string FormatDecimal(float value) => DecimalText.Format(value, DecimalSeparator);

        // Составная строка по шаблону: «Волна {0}/{1}» → «Wave {0}/{1}». У каждого языка в шаблоне свой порядок слов и
        // единицы. Аргументы — готовый текст: дробные числа уже отформатированы FormatDecimal.
        public string FormatTemplate(string sourceTemplate, params object[] arguments)
            => string.Format(CultureInfo.InvariantCulture, TranslateTemplate(sourceTemplate), arguments);

        // Форма слова после числа. Формы исходного языка передаёт код, перевод ищется в таблице по первой из них.
        public string SelectPlural(int number, string one, string few, string many)
        {
            if (IsSourceLanguage)
                return RussianPlural.Select(number, one, few, many);

            if (_table.TryGetPluralForms(one, out string[] forms))
                return EnglishPlural.Select(number, forms[OneFormIndex], forms[OtherFormIndex]);

            ReportMissing(one);

            return RussianPlural.Select(number, one, few, many);
        }

        // Тексты TMP объекта и всех его частей, в том числе выключенных
        public void TranslateTexts(GameObject root)
        {
            if (IsSourceLanguage)
                return;

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                string translated = Translate(text.text);

                if (translated != text.text)
                    text.text = translated;
            }
        }

        private string TranslateTemplate(string sourceTemplate)
        {
            if (IsSourceLanguage)
                return sourceTemplate;

            if (_table.TryGetTemplate(sourceTemplate, out string template))
                return template;

            ReportMissing(sourceTemplate);

            return sourceTemplate;
        }

        private void ReportMissing(string source)
        {
            #if UNITY_EDITOR
                if (_table.IsException(source) || _sourceLanguageLetter.IsMatch(source) == false)
                    return;

                if (_reportedMissing.Add(source))
                    Debug.LogWarning($"Нет перевода строки: «{source}»");
            #endif
        }
    }
}

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Assets._Project.Develop.Runtime.Utilities.Localization
{
    // Таблица перевода из JSON: ключ — точный текст на исходном языке. Раздел strings — целые строки с переводом,
    // templates — шаблоны составных строк с {0}, {1}…, plurals — формы слова после числа (ключ — первая форма исходного
    // языка, перевод — массив форм), exceptions — строки, которые переводить не нужно (заглушки, которые перезаписывает
    // код). Разбор — через JToken, без классов-носителей: им не нужна рефлексия, которую может срезать сборка.
    public class TranslationTable
    {
        private const string StringsSection = "strings";
        private const string TemplatesSection = "templates";
        private const string PluralsSection = "plurals";
        private const string ExceptionsSection = "exceptions";
        private const string SourceField = "ru";
        private const string TranslationField = "en";
        private const int FirstForm = 0;

        private readonly Dictionary<string, string> _translations = new();
        private readonly Dictionary<string, string> _templates = new();
        private readonly Dictionary<string, string[]> _pluralForms = new();
        private readonly HashSet<string> _exceptions = new();

        public TranslationTable(string json)
        {
            JObject root = JObject.Parse(json);

            foreach (JToken entry in GetSection(root, StringsSection))
                _translations[(string)entry[SourceField]] = (string)entry[TranslationField];

            foreach (JToken entry in GetSection(root, TemplatesSection))
                _templates[(string)entry[SourceField]] = (string)entry[TranslationField];

            foreach (JToken entry in GetSection(root, PluralsSection))
                _pluralForms[(string)entry[SourceField][FirstForm]] = ReadForms(entry[TranslationField]);

            foreach (JToken entry in GetSection(root, ExceptionsSection))
                _exceptions.Add((string)entry[SourceField]);
        }

        public bool TryGetTranslation(string source, out string translation)
            => _translations.TryGetValue(source, out translation);

        public bool TryGetTemplate(string source, out string template) => _templates.TryGetValue(source, out template);

        public bool TryGetPluralForms(string sourceFirstForm, out string[] forms)
            => _pluralForms.TryGetValue(sourceFirstForm, out forms);

        public bool HasTranslation(string source) => _translations.ContainsKey(source);

        public bool IsException(string source) => _exceptions.Contains(source);

        private static IEnumerable<JToken> GetSection(JObject root, string name)
        {
            if (root[name] is JArray section)
                return section;

            return new JArray();
        }

        private static string[] ReadForms(JToken formsToken)
        {
            JArray formsArray = (JArray)formsToken;
            string[] forms = new string[formsArray.Count];

            for (int i = 0; i < forms.Length; i++)
                forms[i] = (string)formsArray[i];

            return forms;
        }
    }
}

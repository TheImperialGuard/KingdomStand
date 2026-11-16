using System.Collections.Generic;
using YG;

namespace Assets._Project.Develop.Runtime.Utilities.Localization
{
    // Язык от SDK Яндекс Игр (модуль EnvirData плагина). Русский — для языков, у которых Яндекс сам берёт русский
    // резервным, остальным — английский. В редакторе плагин подставляет сюда Simulation.language своих настроек.
    public class YGLanguageSource : ILanguageSource
    {
        private static readonly HashSet<string> _russianFallbackLanguages = new() { "ru", "be", "kk", "uk", "uz" };

        public Language GetLanguage()
        {
            string languageCode = YG2.envir.language;

            if (languageCode != null && _russianFallbackLanguages.Contains(languageCode))
                return Language.Russian;

            return Language.English;
        }
    }
}

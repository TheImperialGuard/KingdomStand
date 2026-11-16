using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.Localization
{
    // Язык системы, вне площадок с собственным SDK языка. В WebGL Unity берёт его из браузера.
    public class SystemLanguageSource : ILanguageSource
    {
        public Language GetLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian:
                case SystemLanguage.Belarusian:
                case SystemLanguage.Ukrainian:
                    return Language.Russian;

                default:
                    return Language.English;
            }
        }
    }
}

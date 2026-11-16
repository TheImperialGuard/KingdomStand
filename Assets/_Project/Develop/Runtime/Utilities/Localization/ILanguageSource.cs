namespace Assets._Project.Develop.Runtime.Utilities.Localization
{
    // Откуда берётся язык игры: SDK площадки или настройки системы. Читается один раз при старте проекта.
    public interface ILanguageSource
    {
        Language GetLanguage();
    }
}

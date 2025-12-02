using YG;

namespace _Project.Scripts.Localization
{
    public class YgLanguageInfo : ILanguageInfo
    {
        public ILanguageInfo.LanguageType GetCurrentLanguage()
        {
            switch (YG2.envir.language)
            {
                case "ru":
                    return ILanguageInfo.LanguageType.Russian;
                case "en":
                    return ILanguageInfo.LanguageType.English;
                case "tr":
                    return ILanguageInfo.LanguageType.Turkish;
                case "es":
                    return ILanguageInfo.LanguageType.Spanish;
                case "fr":
                    return ILanguageInfo.LanguageType.French;
                default:
                    return ILanguageInfo.LanguageType.English;
            }
        }
    }
}

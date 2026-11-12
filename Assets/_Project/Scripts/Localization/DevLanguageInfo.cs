using UnityEngine;

namespace _Project.Scripts.Localization
{
    public class DevLanguageInfo : ILanguageInfo
    {
        public ILanguageInfo.LanguageType GetCurrentLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian:
                    return ILanguageInfo.LanguageType.Russian;
                case SystemLanguage.English:
                    return ILanguageInfo.LanguageType.English;
                case SystemLanguage.Spanish:
                    return ILanguageInfo.LanguageType.Spanish;
                case SystemLanguage.French:
                    return ILanguageInfo.LanguageType.French;
                case SystemLanguage.Turkish:
                    return ILanguageInfo.LanguageType.Turkish;
                case SystemLanguage.German:
                    return ILanguageInfo.LanguageType.German;
                default:
                    return ILanguageInfo.LanguageType.English;
            }
        }
    }
}

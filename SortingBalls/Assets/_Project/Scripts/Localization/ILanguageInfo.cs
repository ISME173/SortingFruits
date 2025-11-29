namespace _Project.Scripts.Localization
{
    public interface ILanguageInfo
    {
        public enum LanguageType
        {
            Russian, English, Spanish, Turkish, French
        }

        public LanguageType GetCurrentLanguage();
    }
}

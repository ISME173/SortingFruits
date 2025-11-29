using Reflex.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static _Project.Scripts.Localization.ILanguageInfo;

namespace _Project.Scripts.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizationText : MonoBehaviour
    {
        [SerializeField] private List<TextByLanguage> _translations = new List<TextByLanguage>();

        private ILanguageInfo _languageInfo;
        private TMP_Text _text;

        [Inject]
        private void Initialize(ILanguageInfo languageInfo)
        {
            _languageInfo = languageInfo;
            _text = GetComponent<TMP_Text>();

            ValidateTranslitions();

            LanguageType currentLanguage = _languageInfo.GetCurrentLanguage();
            TextByLanguage? textBylanguage = _translations.FirstOrDefault(x => x.LanguageType == currentLanguage);

            if (textBylanguage.HasValue == false)
            {
                Debug.LogWarning($"Not founded translation by language: {currentLanguage} in {gameObject.name}");
                return;
            }

            _text.font = textBylanguage.Value.TMP_FontAsset;
            _text.color = textBylanguage.Value.Color;
            _text.text = textBylanguage.Value.Text;
        }

        private void ValidateTranslitions()
        {
            if (_translations == null || _translations.Count <= 1)
                return;

            var seen = new HashSet<LanguageType>();
            for (int i = 0; i < _translations.Count; i++)
            {
                var lang = _translations[i].LanguageType;
                if (seen.Contains(lang))
                {
                    _translations.RemoveAt(i);
                    i--;

                    Debug.LogWarning($"In {nameof(_translations)}, in {nameof(LocalizationText)} '{gameObject.name}' there were elements with a recurring language type '{lang}'. One of them was removed!");
                }
                else
                {
                    seen.Add(lang);
                }
            }
        }

        [Serializable]
        private struct TextByLanguage
        {
            [SerializeField] private LanguageType _languageType;
            [Space]
            [SerializeField] private TMP_FontAsset _fontAsset;
            [SerializeField] private Color _textColor;
            [Space]
            [SerializeField, TextArea] private string _text;

            public LanguageType LanguageType => _languageType;
            public TMP_FontAsset TMP_FontAsset => _fontAsset;
            public Color Color => _textColor;
            public string Text => _text;
        }
    }
}

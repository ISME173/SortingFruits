using LitMotion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimationsUI.CoreScripts
{
    public class PopupAnimationPanelsSequence : MonoBehaviour
    {
        [Tooltip("Панели для анимации показа.\n'Append' - присоеденить анимацию панели после окончания предыдущей.\n'Join' - начать анимацию панели вместе с предыдущей.\nДля первой панели выбранный вариант не имеет значения.")]
        [SerializeField] private List<PopupElement> _showPopupElements = new List<PopupElement>();
        [Space]
        [Tooltip("Панели для анимации сокрытия.\n'Append' - присоеденить анимацию панели после окончания предыдущей.\n'Join' - начать анимацию панели вместе с предыдущей.\nДля первой панели выбранный вариант не имеет значения.")]
        [SerializeField] private List<PopupElement> _hidePopupElements = new List<PopupElement>();

        private MotionHandle _showMotionHandle;
        private MotionHandle _hideMotionHandle;

        [ContextMenu("AnimationMethods/Show")]
        private void Show()
        {
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("Нельзя включать анимацию пока не запущена сцена!");
                return;
            }

            gameObject.SetActive(true);
            Show(null);
        }

        [ContextMenu("AnimationMethods/hide")]
        private void Hide()
        {
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("Нельзя включать анимацию пока не запущена сцена!");
                return;
            }

            Hide(() => gameObject.SetActive(false));
        }

        public void Show(Action callback)
        {
            _hideMotionHandle.TryCancel();

            MotionSequenceBuilder motionSequenceBuilder = LSequence.Create();

            for (int i = 0; i < _showPopupElements.Count; i++)
            {
                PopupElement popupElement = _showPopupElements[i];
                if (popupElement.TransitionType == TransitionType.Append || i == 0)
                {
                    motionSequenceBuilder.Append(popupElement.PopupPanelForAnimate.ShowPanel(i == _showPopupElements.Count - 1 ? callback : null));
                }
                else
                {
                    motionSequenceBuilder.Join(popupElement.PopupPanelForAnimate.ShowPanel(i == _showPopupElements.Count - 1 ? callback : null));
                }
            }

            _showMotionHandle = motionSequenceBuilder.Run();
        }

        public void Hide(Action callback)
        {
            _showMotionHandle.TryCancel();

            MotionSequenceBuilder motionSequenceBuilder = LSequence.Create();

            for (int i = 0; i < _hidePopupElements.Count; i++)
            {
                PopupPanelForAnimate popupPanel = _hidePopupElements[i].PopupPanelForAnimate;
                if (_hidePopupElements[i].TransitionType == TransitionType.Append || i == 0)
                {
                    motionSequenceBuilder.Append(popupPanel.HidePanel(i == _hidePopupElements.Count - 1 ? callback : null));
                }
                else
                {
                    motionSequenceBuilder.Join(popupPanel.HidePanel(i == _hidePopupElements.Count - 1 ? callback : null));
                }
            }

            _hideMotionHandle = motionSequenceBuilder.Run();
        }

        [Serializable]
        private class PopupElement
        {
            [SerializeField] private PopupPanelForAnimate _popupPanel;
            [SerializeField] private TransitionType _transitionType;

            public PopupPanelForAnimate PopupPanelForAnimate => _popupPanel;
            public TransitionType TransitionType => _transitionType;
        }
    }

    public enum TransitionType
    {
        Append, Join
    }
}

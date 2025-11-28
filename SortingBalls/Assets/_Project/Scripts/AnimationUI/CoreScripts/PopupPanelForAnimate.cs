using LitMotion;
using NaughtyAttributes;
using System;
using UnityEngine;

namespace AnimationsUI.CoreScripts
{
    public abstract class PopupPanelForAnimate : MonoBehaviour
    {
        [SerializeField] private RectTransform _panelForAnimate;

        protected RectTransform PanelForAnimate => _panelForAnimate;

        public abstract MotionHandle ShowPanel(Action callback);
        public abstract MotionHandle HidePanel(Action callback);


        [Serializable]
        public struct AnimationSettings
        {
            [SerializeField, Min(0)] private float _time;
            [SerializeField] private Ease _ease;
            [Space]
            [SerializeField] private bool _withLoop;
            [SerializeField, Min(-1)] private int _loopCount;
            [SerializeField] private LoopType _loopType;
            
            public float Time => _time;
            public Ease Ease => _ease;
            public LoopType LoopType => _loopType;
            public bool WithLoop => _withLoop;
            public int LoopCount => _loopCount;
        }
    }
}

using _Project.Scripts.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Scripts.FlaskSequence
{
    [RequireComponent(typeof(Collider2D))]
    public class Flask : MonoBehaviour
    {
        [SerializeField] private Transform _slotForSelectItems;
        [SerializeField] private Transform _center;
        [SerializeField] private List<ItemSlot> _itemSlots = new List<ItemSlot>();

        [Header("VFX")]
        [SerializeField] private ParticleSystem _onFilledVisualEffect;

        [Header("SFX")]
        [SerializeField] private AudioEvent _onFilledSoundEffect;
        [SerializeField] private AudioEvent _onClickToFlaskSoundEffect;
        [SerializeField] private AudioEvent _onMovedItemInFlaskEffect;

        private IAudioService _audioService;
        private Collider2D _collider2D;

        public event Action OnFilled;

        public Collider2D Collider2D => _collider2D ?? GetComponent<Collider2D>();
        public bool IsFilled { get; private set; } = false;
        public Transform SlotForSelectItems => _slotForSelectItems;
        public Transform Center => _center;
        public int FreeSlotsCount => _itemSlots.Where(slot => slot.Item == null).Count();
        public bool IsEmpty => _itemSlots.All(slot => slot.Item == null);

        private void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
        }

        public void InjectAudioService(IAudioService audioService)
        {
            _audioService = audioService;
        }

        public bool TryAddItem(Item item)
        {
            if (IsFilled)
                return false;

            ItemSlot slot = _itemSlots.FirstOrDefault(x => x.Item == null);

            if (slot != null)
            {
                slot.Item = item;

                if (FreeSlotsCount == 0 && _itemSlots.All(s => s.Item.ItemName == slot.Item.ItemName))
                {
                    IsFilled = true;
                    OnFilled?.Invoke();
                }

                return true;
            }

            return false;
        }

        public void PlayVfxOnFilledEffect()
        {
            _onFilledVisualEffect.Play();
        }

        public void PlaySfxOnFilledEffect()
        {
            _audioService.PlayOneShot(_onFilledSoundEffect);
        }

        public void PlaySfxOnClickedToFlask()
        {
            _audioService.PlayOneShot(_onClickToFlaskSoundEffect, transform.position);
        }

        public void PlaySfxOnMovedItemInFlask()
        {
            _audioService.PlayOneShot(_onMovedItemInFlaskEffect);
        }

        public Item GetFirstItem()
        {
            ItemSlot slot = _itemSlots.LastOrDefault(slot => slot.Item != null);
            Item itemForReturn = null;

            if (slot != null)
            {
                itemForReturn = slot.Item;
                slot.Item = null;
            }

            return itemForReturn;
        }

        public Item PeekFirstItem()
        {
            ItemSlot slot = _itemSlots.LastOrDefault(slot => slot.Item != null);
            return slot == null ? null : slot.Item;
        }

        public Transform GetFirstEmptySlotTransform()
        {
            ItemSlot itemSlot = _itemSlots.FirstOrDefault(slot => slot.Item == null);
            return itemSlot != null ? itemSlot.SlotTransform : null;
        }

        [Serializable]
        public class ItemSlot
        {
            public Item Item;

            [SerializeField] private Transform _slotTransform;

            public Transform SlotTransform => _slotTransform;
        }
    }
}

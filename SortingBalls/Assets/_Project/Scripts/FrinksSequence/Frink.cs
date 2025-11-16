using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Scripts.FruitsSequence
{
    [RequireComponent(typeof(Collider2D))]
    public class Frink : MonoBehaviour
    {
        [SerializeField] private List<ItemSlot> _itemSlots = new List<ItemSlot>();

        public bool TryAddItem(Item item)
        {
            if (_itemSlots[0].Item != null)
            {
                return false;
            }

            _itemSlots[0].Item = item;

            return true;
        }

        public Item GetFirstItem()
        {
            ItemSlot slot = _itemSlots.FirstOrDefault(slot => slot.Item != null);
            Item itemForReturn = slot == null ? null : slot.Item;

            if (slot != null)
                slot.Item = null;

            return itemForReturn;
        }

        public Item PeekFirstItem()
        {
            ItemSlot slot = _itemSlots.FirstOrDefault(slot => slot.Item != null);
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
            [HideInInspector] public Item Item;

            [SerializeField] private Transform _slotTransform;

            public Transform SlotTransform => _slotTransform;
        }
    }
}

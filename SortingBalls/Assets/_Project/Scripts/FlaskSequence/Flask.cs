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
        [SerializeField] private List<ItemSlot> _itemSlots = new List<ItemSlot>();

        public Transform SlotForSelectItems => _slotForSelectItems;

        public bool TryAddItem(Item item)
        {
            ItemSlot slot = _itemSlots.FirstOrDefault(x => x.Item == null);

            if (slot != null)
            {
                slot.Item = item;
                return true;
            }

            return false;
        }

        public Item GetFirstItem()
        {
            ItemSlot slot = _itemSlots.FirstOrDefault(slot => slot.Item != null);
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
            public Item Item;

            [SerializeField] private Transform _slotTransform;

            public Transform SlotTransform => _slotTransform;
        }
    }
}

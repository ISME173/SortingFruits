using UnityEngine;

namespace _Project.Scripts.FruitsSequence
{
    public class Item : MonoBehaviour
    {
        [SerializeField, TextArea] private string _itemName;

        public string ItemName => _itemName;
    }
}

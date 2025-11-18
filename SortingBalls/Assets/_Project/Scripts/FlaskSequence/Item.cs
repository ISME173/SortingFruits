using UnityEngine;

namespace _Project.Scripts.FlaskSequence
{
    public class Item : MonoBehaviour
    {
        [SerializeField, TextArea] private string _itemName;

        public string ItemName => _itemName;
    }
}

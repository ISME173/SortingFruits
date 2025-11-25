using NaughtyAttributes;
using Reflex.Attributes;
using UnityEngine;

namespace _Project.Scripts.Saves
{
    public class EditorClearSaves : MonoBehaviour
    {
        [Inject] private readonly ISaves Saves;

        [Button]
        private void ClearSaves()
        {
            if (Saves == null)
            {
                Debug.LogError($"{nameof(Saves)} is empty!");
                return;
            }

            Saves.ClearAllSaves();
        }
    }
}

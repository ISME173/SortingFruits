using UnityEngine;

namespace _Project.Scripts.GameEvents
{
    public class DevGameEvents : IGameEvents
    {
        public void GameReadyApi()
        {
            Debug.Log("GameReadyApi");
        }

        public void GameStart()
        {
            Debug.Log("GameStart");
        }

        public void GameStop()
        {
            Debug.Log("GameStop");
        }
    }
}

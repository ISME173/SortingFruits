using YG;

namespace _Project.Scripts.GameEvents
{
    public class YgGameEvents : IGameEvents
    {
        public void GameReadyApi()
        {
            YG2.GameReadyAPI();
        }

        public void GameStart()
        {
            YG2.GameplayStart();
        }

        public void GameStop()
        {
            YG2.GameplayStop();
        }
    }
}

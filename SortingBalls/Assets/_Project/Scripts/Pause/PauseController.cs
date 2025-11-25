using System;

namespace _Project.Scripts.Pause
{
    public class PauseController : IDisposable
    {
        private readonly PauseView PauseView;

        public PauseController(PauseView pauseView)
        {
            PauseView = pauseView;

            PauseView.OnClosePauseViewClicked += OnCloseButtonClicked;
            PauseView.OnOpenPauseViewClicked += OnOpenButtonClicked;
            PauseView.OnOpenLevelsViewClicked += OnOpenLevelsViewButtonClicked;
        }

        public void Dispose()
        {
            PauseView.OnClosePauseViewClicked -= OnCloseButtonClicked;
            PauseView.OnOpenPauseViewClicked -= OnOpenButtonClicked;
            PauseView.OnOpenLevelsViewClicked -= OnOpenLevelsViewButtonClicked;
        }

        private void OnCloseButtonClicked()
        {
            PauseView.Hide();
        }

        private void OnOpenButtonClicked()
        {
            PauseView.Show();
        }

        private void OnOpenLevelsViewButtonClicked()
        {
            PauseView.Hide();
        }
    }
}

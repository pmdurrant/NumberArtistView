using NumberArtist.View.Views;

namespace NumberArtistView
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new LoginPage());

            window.Created += (s, e) =>
            {
#if WINDOWS
                if (window.Handler?.PlatformView is MauiWinUIWindow nativeWindow)
                {
                    var appWindow = nativeWindow.AppWindow;
                    if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    {
                        presenter.IsMaximizable = true;
                        presenter.IsMinimizable = true;
                        presenter.IsResizable = true;
                    }
                }
#endif
            };

            return window;
        }
    }
}
using NumberArtist.View.Views;

namespace NumberArtistView
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new LoginPage();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            window.Created += (s, e) =>
            {
#if WINDOWS
                var nativeWindow = window.Handler.PlatformView as MauiWinUIWindow;
                var appWindow = nativeWindow.AppWindow;
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    presenter.IsMaximizable = false;
                    presenter.IsMinimizable = false;
                    presenter.IsResizable = false;
                }
#endif
            };

            return window;
        }
    }
}
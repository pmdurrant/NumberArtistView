using Microsoft.Extensions.Logging;
using NumberArtist.View;
using NumberArtist.View.ViewModels;
using NumberArtistView.Services;

namespace NumberArtistView
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // Register Services
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<AuthService>();

            // Register ViewModels
            builder.Services.AddTransient<LoginViewModel>();

            // Register Pages
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<NumberArtist.View.Views.LoginPage>();


            return builder.Build();
        }
    }
}

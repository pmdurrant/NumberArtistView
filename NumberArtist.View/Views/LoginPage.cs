csharp NumberArtistView\NumberArtist.View\Views\LoginPage.cs
using Microsoft.Maui.Controls;
using NumberArtist.View.ViewModels;

namespace NumberArtist.View.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Initialize the ViewModel asynchronously without blocking the UI or throwing during XAML load.
        if (BindingContext is LoginViewModel vm)
        {
            _ = vm.InitializeAsync(); // fire-and-forget; errors handled inside InitializeAsync
        }
    }
}
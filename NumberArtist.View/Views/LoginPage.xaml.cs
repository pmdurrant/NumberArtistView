using System;
using Microsoft.Maui.Controls;

namespace NumberArtist.View.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void OnTogglePasswordVisibilityClicked(object? sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        }
    }
}
namespace NumberArtist.View.ViewModels
{
    public partial class LoginViewModel
    {
        private class TokenResponse
        {
            public string Token { get; set; }
            public string UserId { get; set; } = string.Empty;
        }
    }
}
namespace API.Helpers
{
    public class AuthenticationResult
    {
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public IEnumerable<string> Errors { get; set; } = null!;
    }
}


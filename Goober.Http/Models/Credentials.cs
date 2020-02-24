using Goober.Http.Enums;

namespace Goober.Http.Models
{
    public class Credentials
    {
        public string Login { get; set; }

        public string Password { get; set; }

        public string Token { get; set; }

        public CredentialsTypeEnum Type { get; set; }
    }
}

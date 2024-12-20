namespace PasswordManager.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Additional { get; set; }
        public string MaskedPassword { get { return new string('*', Password.Length); } }
    }
}

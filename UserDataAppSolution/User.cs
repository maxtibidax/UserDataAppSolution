namespace UserDataLibrary.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; } // В реальном приложении используйте хэширование!
    }
}
namespace YamHUB.AuthService.Model
{
    public class RegistrationMdl
    {
        #region Properties
        public string Name { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string Gender { get; set; }

        public int Age { get; set; }

        public List<byte>? Photo { get; set; }

        public string? Bio { get; set; }

        public string HashPassword { get; set; } 
        #endregion
    }
}

using System.ComponentModel.DataAnnotations;
using BCrypt.Net;
namespace YamHUB.AuthService.Model
{
    public class RegisterationViewMdl
    {
        #region Properties
        [RegularExpression(@"^\w{6,}$")]
        public string Username { get; set; }

        [RegularExpression(@"^[A-Z][a-z]{2,}$", ErrorMessage = "Invalid First Name")]
        public string FirstName { get; set; }

        [RegularExpression(@"^[A-Z][a-z]{2,}$", ErrorMessage = "Invlaid Last Name")]
        public string LastName { get; set; }

        [RegularExpression(@"^Male|Female$", ErrorMessage = "Invalid Gender")]
        public string Gender { get; set; }

        [RegularExpression(@"[1-9][0-9]")]
        public int Age { get; set; }

        [RegularExpression(@"^[\w\-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Invalid Gender Input")]
        public string Email { get; set; }

        public List<byte>? Photo { get; set; }

        [RegularExpression(@"^\w*$")]
        public string? Bio { get; set; }

        [RegularExpression(@"^.*(?=.{8,})(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*?_ ]).*$", ErrorMessage = "Invalid Password")]
        public string? Password { get; set; }
        #endregion

        #region CastingOperators
        public static explicit operator RegistrationMdl(RegisterationViewMdl mdl)
        {
            var FullName = mdl.FirstName + " " + mdl.LastName;
            var password = BCrypt.Net.BCrypt.HashPassword(mdl.Password);
            return new RegistrationMdl
            {
                Name = FullName,
                Username = mdl.Username,
                Gender = mdl.Gender,
                Age = mdl.Age,
                Photo = mdl.Photo,
                Bio = mdl.Bio,
                Email = mdl.Email,
                HashPassword = password
            };
        }

        public static explicit operator RegisterationViewMdl(RegistrationMdl mdl)
        {
            var names = mdl.Name.Split(' ');
            return new RegisterationViewMdl
            {
                FirstName = names[0],
                LastName = names[1],
                Email = mdl.Email,
                Photo = mdl.Photo,
                Bio = mdl.Bio,
                Username = mdl.Email,
                Gender = mdl.Gender,
                Age = mdl.Age,
                Password = null
            };

        } 
        #endregion
    }
}

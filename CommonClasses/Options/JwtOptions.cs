namespace CommonClasses.Options
{
    public class JwtOptions
    {

        #region Properties
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int Lifetime { get; set; }
        public string SecretKey { get; set; }
        #endregion

    }
}

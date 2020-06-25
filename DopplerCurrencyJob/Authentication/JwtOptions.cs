namespace Doppler.Currency.Job.Authentication
{
    public class JwtOptions
    {
        public string RsaParametersFilePath { get; set; }
        public long TokenLifeTime { get; set; }
    }
}

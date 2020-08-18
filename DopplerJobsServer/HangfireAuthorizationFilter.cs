using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

namespace Doppler.Jobs.Server
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly IConfiguration _configuration;
        private readonly IFileProvider _fileProvider;

        public HangfireAuthorizationFilter(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _fileProvider = webHostEnvironment.ContentRootFileProvider;
        }

        public bool Authorize([NotNull] DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            var token = httpContext.Request.Query["auth"][0];

            var securityOptions = Configure();

            var validationParameters = new TokenValidationParameters
            {
                IssuerSigningKeys = securityOptions.SigningKeys,
                ValidateIssuer = false,
                ValidateLifetime = !securityOptions.SkipLifetimeValidation,
                ValidateAudience = false
            };

            var hand = new JwtSecurityTokenHandler();
            var claims = hand.ValidateToken(token, validationParameters, out var validatedToken);

            return claims.HasClaim(c => c.Type.Equals("isSU"));
        }

        private static string ReadToEnd(IFileInfo fileInfo)
        {
            using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static RsaSecurityKey ParseXmlString(string xmlString)
        {
            using var rsaProvider = new RSACryptoServiceProvider();
            rsaProvider.FromXmlString(xmlString);
            var rsaParameters = rsaProvider.ExportParameters(false);
            return new RsaSecurityKey(RSA.Create(rsaParameters));
        }

        private DopplerSecurityOptions Configure()
        {
            var options = new DopplerSecurityOptions();
            var path = _configuration.GetValue("PublicKeysFolder", "public-keys");
            var files = _fileProvider.GetDirectoryContents(path).Where(x => !x.IsDirectory);
            var publicKeys = files
                .Select(ReadToEnd)
                .Select(ParseXmlString)
                .ToArray();

            options.SkipLifetimeValidation = false;
            options.SigningKeys = publicKeys;

            return options;
        }
    }
}
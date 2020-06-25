using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Doppler.Currency.Job.Authentication
{
    public static class TokenValidatorsServiceCollectionExtensions
    {
        public static IServiceCollection AddJwtTokenValidator([NotNull] this IServiceCollection services)
        {
            // TODO: consider to get the path from somewhere else
            var basePath = Directory.GetCurrentDirectory();
            var rsaParametersFilePath = services.BuildServiceProvider()
                .GetService<IOptions<JwtOptions>>()
                .Value
                .RsaParametersFilePath;

            var absolutePath = Path.Combine(basePath, rsaParametersFilePath);
            RsaSecurityKey key;
            using (var textReader = File.OpenText(absolutePath))
            {
                RSACryptoServiceProvider publicAndPrivate = new RSACryptoServiceProvider();
                publicAndPrivate.FromXmlString(textReader.ReadToEnd());

                key = new RsaSecurityKey(publicAndPrivate.ExportParameters(true));
            }

            return services.AddSingleton(new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        }
    }
}

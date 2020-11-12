using CrossCutting.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.Jobs.Test
{
    public class JwtTokenGeneratorTests
    {

        [Fact]
        public async Task JwtTokenGenerator_ShouldBeOk_WhenIsSuTagIsPresentAndIsTrue()
        {
            // Arrange
            var options = Options.Create(new JwtOptions()
            {
                TokenLifeTime = 1
            });

            var publicAndPrivate = new RSACryptoServiceProvider(2048);
            var key = new RsaSecurityKey(publicAndPrivate.ExportParameters(true));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
            var handler = new JwtSecurityTokenHandler();
            var sut = new JwtTokenGenerator(options, signingCredentials, handler);

            // Act
            var jwtToken = sut.CreateJwtToken();

            // Assert
            var jsonToken = handler.ReadToken(jwtToken);
            var tokens = handler.ReadToken(jwtToken) as JwtSecurityToken;
            var isSuClaimValue = tokens.Claims.First(claim => claim.Type == "isSU").Value;
            Assert.True(isSuClaimValue == "true");
        }
    }
}

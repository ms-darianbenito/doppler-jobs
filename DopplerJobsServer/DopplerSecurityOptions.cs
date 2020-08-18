using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Doppler.Jobs.Server
{
    public class DopplerSecurityOptions
    {
        public bool SkipLifetimeValidation { get; set; }
        public IEnumerable<SecurityKey> SigningKeys { get; set; } = new SecurityKey[0];
    }
}

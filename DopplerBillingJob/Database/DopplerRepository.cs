using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService.Entities;
using Dapper;
using Doppler.Billing.Job.Settings;
using Microsoft.Extensions.Options;

namespace Doppler.Billing.Job.Database
{
    public class DopplerRepository : IDopplerRepository
    {
        private readonly DopplerRepositorySettings _dopplerSapServiceSettings;

        public DopplerRepository(IOptionsMonitor<DopplerRepositorySettings> dopplerSapServiceSettings)
        {
            _dopplerSapServiceSettings = dopplerSapServiceSettings.CurrentValue;
        }

        public async Task<IList<UserBilling>> GetUserBillingInformation()
        {
            await using var conn = new SqlConnection(_dopplerSapServiceSettings.ConnectionString);
            
            //TODO: Add sql sentence to get data for SAP
            const string query = "SELECT * FROM City";

            var result = await conn.QueryAsync(query);

            return new List<UserBilling>();
        }
    }
}

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService.Entities;
using Dapper;
using Doppler.Billing.Job.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Doppler.Billing.Job.Database
{
    public class DopplerRepository : IDopplerRepository
    {
        private readonly ILogger<DopplerRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly DopplerBillingJobSettings _dopplerBillingJobSettings;

        public DopplerRepository(
            IConfiguration configuration,
            ILogger<DopplerRepository> dopplerRepositoryLogger,
            DopplerBillingJobSettings dopplerBillingJobSettings)
        {
            _logger = dopplerRepositoryLogger;
            _configuration = configuration;
            _dopplerBillingJobSettings = dopplerBillingJobSettings;
        }

        public async Task<IList<UserBilling>> GetUserBillingInformation()
        {
            _logger.LogInformation("Getting database connection.");
            var result = new List<UserBilling>();

            try
            {
                await using var conn = new SqlConnection(_configuration.GetConnectionString("DopplerDatabase"));

                var query = new StringBuilder();
                foreach (var storeProcedureName in _dopplerBillingJobSettings.StoredProcedureNames)
                {
                    query.Append(storeProcedureName);
                }

                _logger.LogInformation("Sending SQL sentence to database server.");
                using var multiResult = await conn.QueryMultipleAsync(query.ToString());

                while (!multiResult.IsConsumed)
                {
                    result.AddRange(multiResult.Read<UserBilling>().ToList());
                }

                return result;
            }
            catch (SqlException e)
            {
                _logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }
    }
}

using System.Data.Common;

namespace Doppler.Billing.Job.Database
{
    public interface IDbConnectionFactory
    {
        DbConnection GetConnection();
    }
}
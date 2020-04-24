using System.Data.Common;

namespace Doppler.Database
{
    public interface IDbConnectionFactory
    {
        DbConnection GetConnection();
    }
}
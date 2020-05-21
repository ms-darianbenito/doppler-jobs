using System.Collections.Generic;

namespace Doppler.Currency.Job.Settings
{
    public class DopplerCurrencyServiceSettings
    {
        public string Url { get; set; }
        public List<string> CurrencyCodeList { get; set; }
        public string InsertCurrencyQuery { get; set; }
        public int HolidayRetryCountLimit { get; set; }
    }
}

using Newtonsoft.Json;
using System.Collections.Generic;

namespace CrossCutting.DopplerSapService.Entities
{
    public class DopplerCurrencyJobResponse
    {
        public string Message { get; set; }
        public IList<CurrencyResponse> CurrencyList { get; set; }
    }
}

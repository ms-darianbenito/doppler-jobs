using Newtonsoft.Json;

namespace CrossCutting.DopplerSapService.Entities
{
    public class CurrencyResponse
    {
        public string Date { get; set; }
        public decimal SaleValue { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? BuyValue { get; set; }
        public string CurrencyName { get; set; }
        public string CurrencyCode { get; set; }
        public bool CotizationAvailable { get; set; }
    }
}

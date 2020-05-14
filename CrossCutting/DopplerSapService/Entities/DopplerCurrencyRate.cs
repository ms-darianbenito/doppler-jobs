namespace CrossCutting.DopplerSapService.Entities
{
    public class DopplerCurrencyRate
    {
        public decimal Rate { get; set; }
        public int IdCurrencyTypeFrom { get; set; }
        public int IdCurrencyTypeTo { get; set; }
    }
}

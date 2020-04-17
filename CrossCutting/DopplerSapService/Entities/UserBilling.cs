namespace CrossCutting.DopplerSapService.Entities
{
    public class UserBilling
    {
        public string Id { get; set; }
        public string PlanType { get; set; }
        public string CreditsOrSubscribersQuantity { get; set; }
        public string IsCustomPlan { get; set; }
        public string IsPlanUpgrade { get; set; }
        public string Currency { get; set; }
        public string Periodicity { get; set; }
        public string PeriodMonth { get; set; }
        public string PeriodYear { get; set; }
        public string PlanFee { get; set; }
        public string Discount { get; set; }
        public string ExtraEmails { get; set; }
        public decimal? ExtraEmailsFeePerUnit { get; set; }
        public string ExtraEmailsPeriodMonth { get; set; }
        public string ExtraEmailsPeriodYear { get; set; }
        public string ExtraEmailsFee { get; set; }
        public string PurchaseOrder { get; set; }
    }
}

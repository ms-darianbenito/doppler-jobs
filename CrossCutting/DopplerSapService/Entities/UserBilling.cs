namespace CrossCutting.DopplerSapService.Entities
{
    public class UserBilling
    {
        public int Id { get; set; }
        public int PlanType { get; set; }
        public string CreditsOrSubscribersQuantity { get; set; }
        public bool IsCustomPlan { get; set; }
        public bool IsPlanUpgrade { get; set; }
        public int Currency { get; set; }
        public int Periodicity { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string PlanFee { get; set; }
        public int Discount { get; set; }
        public int ExtraEmails { get; set; }
        public decimal ExtraEmailsFeePerUnit { get; set; }
        public int ExtraEmailsPeriodMonth { get; set; }
        public int ExtraEmailsPeriodYear { get; set; }
        public int ExtraEmailsFee { get; set; }
        public string PurchaseOrder { get; set; }
    }
}

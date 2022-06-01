namespace BalanceReconciliationService.Models
{
    public class ConstraintsSettings
    {
        public enum ConstraintsSettingsType
        {
            TECHNOLOGIC,
            METROLOGIC
        }
        public ConstraintsSettingsType Type { get; set; }
    }
}

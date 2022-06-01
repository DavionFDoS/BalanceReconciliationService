namespace BalanceReconciliationService.Models
{
    public class ConstraintsSettings
    {
        public enum ConstraintsSettingsType
        {
            TECHNOLOGIC,
            METROLOGIC
        }
        public ConstraintsSettingsType balanceSettingsConstraints { get; set; }
    }
}

namespace BalanceReconciliationService.Models
{
    public class ConstraintsSettings
    {
        public enum ConstraintsSettingsType
        {
            Technological,
            Metrological
        }
        public ConstraintsSettingsType Type { get; set; }
    }
}

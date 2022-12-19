namespace BalanceReconciliationService.Models
{
    public class ReconciledFlowData : FlowData
    {
        public double ReconciledValue { get; set; }

        public double Correction { get; set; }
    }
}

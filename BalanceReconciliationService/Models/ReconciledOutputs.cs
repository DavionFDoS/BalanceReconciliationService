namespace BalanceReconciliationService.Models
{
    public class ReconciledOutputs
    {
        public double CalculationTime { get; set; }
        public double MeasuredDataDisbalance { get; set; }
        public double ReconciledDataDisbalance { get; set; }
        public double GlobaltestValue { get; set; }
        public string? Status { get; set; }

        public List<ReconciledFlowData>? BalanceOutputVariables { get; set; }
    }
}

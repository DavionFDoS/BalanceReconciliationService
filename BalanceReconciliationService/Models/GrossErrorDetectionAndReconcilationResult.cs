namespace BalanceReconciliationService.Models;

public class GrossErrorDetectionAndReconcilationResult
{
    public IEnumerable<ReconciledFlowWithErrorType> Flows { get; set; } = new List<ReconciledFlowWithErrorType>();

    public double MeasuredDataDisbalance { get; set; }

    public double ReconciledDataDisbalance { get; set; }

}

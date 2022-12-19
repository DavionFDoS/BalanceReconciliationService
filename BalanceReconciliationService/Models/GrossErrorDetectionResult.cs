namespace BalanceReconciliationService.Models;

public class GrossErrorDetectionResult
{
    public IEnumerable<FlowData> FlowsWithErrors { get; set; } = new List<FlowData>();
}

using BalanceReconciliationService.Enums;

namespace BalanceReconciliationService.Models
{
    public class ReconciledFlowWithErrorType : ReconciledFlowData
    {
        public GrossErrorType GrossErrorType { get; set; }
    }
}

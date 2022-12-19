using BalanceReconciliationService.Enums;

namespace BalanceReconciliationService.Interfaces
{
    public interface IGrossErrorDetectionService
    {
        IEnumerable<GrossErrorDetectionResult> GrossErrorDetectionByTree(IEnumerable<FlowData> flows, int branchCount, int maxTreeHeight, int maxSolutionsCount, ICollection<GrossErrorType> errorTypes);
    }
}
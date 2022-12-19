using BalanceReconciliationService.Enums;

namespace BalanceReconciliationService.Models
{
    public class MeasuredInputs
    {
        public  ConstraintsType ConstraintsType { get; set; }
        public IList<FlowData> FlowsData { get; set; }
        
    }
}

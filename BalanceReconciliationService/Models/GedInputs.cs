using BalanceReconciliationService.Enums;

namespace BalanceReconciliationService.Models;

public class GedInputs
{
    public GedSettings GedSettings { get; set; }

    public ICollection<GrossErrorType> Errors { get; set; } = new List<GrossErrorType> { GrossErrorType.Measure };

    public ConstraintsType ConstraintsType { get; set; }

    public IList<FlowData> FlowsData { get; set; }

}

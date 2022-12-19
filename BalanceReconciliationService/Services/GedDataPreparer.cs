using BalanceReconciliationService.Models;

namespace BalanceReconciliationService.Services;

public class GedDataPreparer
{
    public IList<FlowData> FlowsData { get; private set; }

    public int Branching { get; private set; }
    public int MaxTreeHeight { get; private set; }
    public int MaxSolutionsCount { get; set; }

    public GedDataPreparer(GedInputs gedInputs)
    {
        ArgumentNullException.ThrowIfNull(gedInputs, nameof(gedInputs));

        FlowsData = gedInputs.FlowsData;
        Branching = gedInputs.GedSettings.Branching;
        MaxTreeHeight = gedInputs.GedSettings.MaxTreeHeight;
        MaxSolutionsCount = gedInputs.GedSettings.MaxSolutionsCount;
    }
}

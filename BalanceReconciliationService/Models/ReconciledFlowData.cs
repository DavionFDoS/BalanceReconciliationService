namespace BalanceReconciliationService.Models
{
    public class ReconciledFlowData
    {
        public Guid Id { get; set; }
        public string? SourceId { get; set; }
        public string? DestinationId { get; set; }
        public string? Name { get; set; }
        public double ReconciliatedValue { get; set; }

        public double ChosenConstraintUpperBound { get; set; }

        public double ChosenConstraintLowerBound { get; set; }
    }
}

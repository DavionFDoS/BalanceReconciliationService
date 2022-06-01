namespace BalanceReconciliationService.Models
{
    public class FlowData
    {
        public Guid Id { get; set; }
        public string? SourceId { get; set; }
        public string? DestinationId { get; set; }
        public string? Name { get; set; }
        public double Measured { get; set; }
        public double UpperMetrologicalBound { get; set; }
        public double LowerMetrologicalBound { get; set; }
        public double UpperTechnologicalBound { get; set; }
        public double LowerTechnologicalBound { get; set; }
        public double Tolerance { get; set; }
        public bool IsMeasured { get; set; }
        public bool IsExcluded { get; set; }
    }
}

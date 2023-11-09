namespace BalanceReconciliationService.Models
{
    public class ToleranceAnalysisResult
    {
        public double RelativeTolerance { get; set; }

        public string SigmaWithStar { get; set; }

        public double RelativeToleranceOfReconciledValues { get; set; }

        public string RelativeToleranceArray { get; set; }

        public string RelativeToleranceReconciledArray { get; set; }
    }
}

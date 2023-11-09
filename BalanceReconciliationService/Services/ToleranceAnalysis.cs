using Accord.Math;
using Accord.Statistics;
using MathNet.Numerics.LinearAlgebra.Double;
using Newtonsoft.Json;

namespace BalanceReconciliationService.Services
{
    public class ToleranceAnalysis
    {
        private readonly MatrixDataPreparer _matrixDataPreparer;

        public string RelativeToleranceArray { get; set; }

        public string RelativeToleranceReconciledArray { get; set; }

        public ToleranceAnalysis(MatrixDataPreparer matrixDataPreparer)
        {
            _matrixDataPreparer = matrixDataPreparer;
        }

        public string GetSigmaDiagonalToJSON()
        {
            var sigmaWithStar = GetSigmaWithStar();
            return JsonConvert.SerializeObject(sigmaWithStar, Formatting.Indented);
        }

        public double GetRelativeMeasurementError()
        {
            var relativeTolerance = SparseVector.OfEnumerable(_matrixDataPreparer.Tolerance) * 100 / _matrixDataPreparer.MeasuredValues;
            RelativeToleranceArray = JsonConvert.SerializeObject(relativeTolerance, Formatting.Indented);

            return Math.Sqrt(relativeTolerance.Count) / relativeTolerance.Sum(x => 1 / x);
        }

        public double GetRelativeMeasurementErrorOfReconciledValues()
        {
            var relativeToleranceOfReconciledValues = SparseVector.OfEnumerable(GetSigmaWithStar()) * 100 / _matrixDataPreparer.MeasuredValues;
            RelativeToleranceReconciledArray = JsonConvert.SerializeObject(relativeToleranceOfReconciledValues, Formatting.Indented);

            return Math.Sqrt(relativeToleranceOfReconciledValues.Count) / relativeToleranceOfReconciledValues.Sum(x => 1 / x);
        }

        private MathNet.Numerics.LinearAlgebra.Double.Vector GetSigmaWithStar()
        {
            var B = GetB();
            //var sigma = GetCovarianceMatrix();
            var sigma = CalculateCovarianceMatrix();
            var BTransposed = SparseMatrix.OfMatrix(B.Transpose());

            //return SparseMatrix.OfArray((B * sigma * BTransposed).ToArray().PseudoInverse());
            return (MathNet.Numerics.LinearAlgebra.Double.Vector)(B * sigma * BTransposed).Diagonal().PointwisePower(0.5);
        }

        private SparseMatrix GetCovarianceMatrix()
        {
            var x0Vector = _matrixDataPreparer.MeasuredValues;
            var measurability = _matrixDataPreparer.MeasureIndicator.Diagonal().ToArray();
            var xStd = SparseVector.OfEnumerable(_matrixDataPreparer.Tolerance) / 1.96;

            for (var i = 0; i < xStd.Count; i++)
            {
                if (measurability[i] == 0.0)
                {
                    xStd[i] = Math.Pow(10, 2) * x0Vector.Maximum();
                }
            }

            return SparseMatrix.OfDiagonalVector(xStd.PointwisePower(2));
        }

        private SparseMatrix GetB()
        {
            var count = _matrixDataPreparer.MeasuredValues.Count;
            //var sigma = GetCovarianceMatrix();
            var sigma = CalculateCovarianceMatrix();
            var A = _matrixDataPreparer.IncidenceMatrix;
            var ATransposed = SparseMatrix.OfMatrix(A.Transpose());

            var insideBrackets = SparseMatrix.OfMatrix((A * sigma * ATransposed).Inverse());

            var rightPart = sigma * ATransposed * insideBrackets * A;

            var I = SparseMatrix.Create(count, count, 0);

            for (int i = 0; i < count; i++)
            {
                I[i, i] = 1.0;
            }

            return I - rightPart;
        }

        private double GetDispersion()
        {
            var mean = _matrixDataPreparer.Tolerance.Mean();
            return _matrixDataPreparer.Tolerance.Sum(x => Math.Pow(x - mean, 2)) / _matrixDataPreparer.Tolerance.Length;
        }

        private double GetStandardDeviation()
        {
            return Math.Sqrt(GetDispersion());
        }

        private SparseMatrix CalculateCovarianceMatrix()
        {
            var count = _matrixDataPreparer.Tolerance.Length;
            var tolerance = _matrixDataPreparer.Tolerance;
            var covarianceMatrix = SparseMatrix.Create(count, count, 0);

            for (var i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    if (i == j)
                    {
                        covarianceMatrix[i, j] = Math.Pow(tolerance[i], 2);
                    }
                    else
                    {
                        covarianceMatrix[i, j] = 0;
                    }
                }
            }

            return covarianceMatrix;
        }
    }
}

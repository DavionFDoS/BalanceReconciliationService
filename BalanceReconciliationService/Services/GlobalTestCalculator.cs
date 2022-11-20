using Accord.Math;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Double;

namespace BalanceReconciliationService.Services
{
    public class GlobalTestCalculator
    {
        private readonly MatrixDataPreparer _dataPreparer;
        public GlobalTestCalculator(MatrixDataPreparer dataPreparer)
        {
            _dataPreparer = dataPreparer;
        }

        public double GetGlobalTest()
        {
            var x0 = _dataPreparer.MeasuredValues.ToArray();
            var a = _dataPreparer.IncidenceMatrix.ToArray();
            var countOfThreads = _dataPreparer.MeasuredValues.Count;
            var measurability = new double[countOfThreads];
            var tolerance = new double[countOfThreads];

            for (int i = 0; i < countOfThreads; i++)
            {
                measurability[i] = _dataPreparer.MeasureIndicator[i, i];
                tolerance[i] = _dataPreparer.MeasuredInputs.FlowsData[i].Tolerance;
            }

            return StartGlobalTest(x0, a, measurability, tolerance);
        }
        private static double StartGlobalTest(double[] x0, double[,] a, double[] measurability, double[] tolerance)
        {
            var aMatrix = SparseMatrix.OfArray(a);
            var aTransposedMatrix = SparseMatrix.OfMatrix(aMatrix.Transpose());
            var x0Vector = SparseVector.OfEnumerable(x0);

            // Введение погрешностей по неизмеряемым потокам
            var xStd = SparseVector.OfEnumerable(tolerance) / 1.96;

            for (var i = 0; i < xStd.Count; i++)
            {
                if (measurability[i] == 0.0)
                {
                    xStd[i] = Math.Pow(10, 2) * x0Vector.Maximum();
                }
            }

            var sigma = SparseMatrix.OfDiagonalVector(xStd.PointwisePower(2));
            // Вычисление вектора дисбалансов
            var r = aMatrix * x0Vector;
            var v = aMatrix * sigma * aTransposedMatrix;
            var vv = v.ToArray();
            vv = vv.PseudoInverse();
            v = SparseMatrix.OfArray(vv);
            var result = r * v * r.ToColumnMatrix();
            var chi = ChiSquared.InvCDF(aMatrix.RowCount, 1 - 0.05);

            Log.Information("Calculated global test of the given system is: {globalTestResult}", result[0] / chi);

            return result[0] / chi;
        }
    }
}

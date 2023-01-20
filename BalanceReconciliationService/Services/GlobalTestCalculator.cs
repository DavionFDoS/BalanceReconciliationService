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

        /// <summary>
        /// Use this method if you need to get Global Test of the source system
        /// </summary>
        /// <returns></returns>
        public double GetSourceSystemGlobalTest()
        {
            var x0 = _dataPreparer.MeasuredValues.ToArray();
            var a = _dataPreparer.IncidenceMatrix.ToArray();
            var measurability = _dataPreparer.MeasureIndicator.Diagonal().ToArray();
            var tolerance = _dataPreparer.Tolerance;

            return GlobalTest(x0, a, measurability, tolerance);
        }

        /// <summary>
        /// Use this method if you need to get Global Test with custom parameters 
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="a"></param>
        /// <param name="measurability"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public double GlobalTest(double[] x0, double[,] a, double[] measurability, double[] tolerance)
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
            var vv = v.ToArray().PseudoInverse();
            v = SparseMatrix.OfArray(vv);
            var result = r * v * r.ToColumnMatrix();
            var chi = ChiSquared.InvCDF(aMatrix.RowCount, 1 - 0.05);

            Log.Information("Calculated global test of the given system is: {globalTestResult}", result[0] / chi);

            return result[0] / chi;
        }
    }
}

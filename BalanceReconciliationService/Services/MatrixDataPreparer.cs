using Accord.Statistics;
using MathNet.Numerics.LinearAlgebra.Double;

namespace BalanceReconciliationService.Services
{
    /// <summary>
    /// Class used to prepare data in matrix form
    /// </summary>
    public class MatrixDataPreparer
    {
        public GraphBuilder GraphBuilder { get; private set; }
        public IList<FlowData> FlowsData { get; private set; }
        public SparseVector MeasuredValues { get; private set; }            // Вектор измеренных значений (x0)
        public SparseMatrix MeasureIndicator { get; private set; }          // Матрица измеряемости (I)
        public SparseMatrix StandardDeviation { get; private set; }         // Матрица метрологической погрешности (W)
        public double[] Tolerance { get; private set; }
        public SparseMatrix IncidenceMatrix { get; private set; }           // Матрица инцидентности / связей
        public SparseVector ReconciledValues { get; private set; }          // Вектор b
        public DenseVector UpperMetrologicalBound { get; private set; }     // Вектор верхних ограничений вектора x
        public DenseVector LowerMetrologicalBound { get; private set; }     // Вектор нижних ограничений вектора x
        public DenseVector UpperTechnologicalBound { get; private set; }    // Вектор верхних ограничений вектора x
        public DenseVector LowerTechnologicalBound { get; private set; }    // Вектор нижних ограничений вектора x
        public SparseMatrix H { get; private set; }                         // H = I * W
        public SparseVector DVector { get; private set; }                   // d = H * x0

        public MatrixDataPreparer(IList<FlowData> flowsData)
        {
            ArgumentNullException.ThrowIfNull(flowsData, nameof(flowsData));

            FlowsData = flowsData;

            GraphBuilder = new GraphBuilder(FlowsData);

            QuadraticProgrammingPreparations();
        }
        private void QuadraticProgrammingPreparations()
        {
            IncidenceMatrix = SparseMatrix.OfArray(GraphBuilder.GetIncidenceMatrix());
            MeasuredValues = SparseVector.OfEnumerable(FlowsData.Select(x => x.Measured));
            MeasureIndicator = SparseMatrix.OfDiagonalArray(FlowsData.Select(x => x.IsMeasured ? 1.0 : 0.0).ToArray());
            StandardDeviation = SparseMatrix.OfDiagonalArray(FlowsData.Select(x =>
            {
                if (!x.IsMeasured)
                {
                    return 1.0;
                }
                else
                {
                    double tolerance = 1.0 / Math.Pow(x.Tolerance, 2);
                    if (Double.IsInfinity(tolerance))
                    {
                        return 1.0;
                    }
                    if (Double.IsNaN(tolerance))
                    {
                        throw new ArgumentException("The value was NaN");
                    }

                    return tolerance;
                }
            }).ToArray());
            Tolerance = DenseVector.OfEnumerable(FlowsData.Select(x => x.Tolerance)).ToArray();
            UpperMetrologicalBound = DenseVector.OfEnumerable(FlowsData.Select(x => x.UpperMetrologicalBound));
            LowerMetrologicalBound = DenseVector.OfEnumerable(FlowsData.Select(x => x.LowerMetrologicalBound));
            UpperTechnologicalBound = DenseVector.OfEnumerable(FlowsData.Select(x => x.UpperTechnologicalBound));
            LowerTechnologicalBound = DenseVector.OfEnumerable(FlowsData.Select(x => x.LowerTechnologicalBound));
            ReconciledValues = new SparseVector(IncidenceMatrix.RowCount);
            H = MeasureIndicator * StandardDeviation;
            DVector = H * (-1) * MeasuredValues;

            Log.Information("Matrix data prepared");
        }
    }
}

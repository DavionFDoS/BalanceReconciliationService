using MathNet.Numerics.LinearAlgebra.Double;

namespace BalanceReconciliationService.Services
{
    /// <summary>
    /// Class used to prepare data in matrix form
    /// </summary>
    public class MatrixDataPreparer
    {
        public GraphBuilder GraphBuilder { get; private set; }
        public MeasuredInputs MeasuredInputs { get; private set; }
        public SparseVector MeasuredValues { get; private set; }            // Вектор измеренных значений (x0)
        public SparseMatrix MeasureIndicator { get; private set; }          // Матрица измеряемости (I)
        public SparseMatrix StandardDeviation { get; private set; }         // Матрица метрологической погрешности (W)
        public SparseMatrix IncidenceMatrix { get; private set; }           // Матрица инцидентности / связей
        public SparseVector ReconciledValues { get; private set; }          // Вектор b
        public DenseVector UpperMetrologicalBound { get; private set; }     // Вектор верхних ограничений вектора x
        public DenseVector LowerMetrologicalBound { get; private set; }     // Вектор нижних ограничений вектора x
        public DenseVector UpperTechnologicalBound { get; private set; }    // Вектор верхних ограничений вектора x
        public DenseVector LowerTechnologicalBound { get; private set; }    // Вектор нижних ограничений вектора x
        public SparseMatrix H { get; private set; }                         // H = I * W
        public SparseVector DVector { get; private set; }                   // d = H * x0

        private readonly ILogger<MatrixDataPreparer> _logger;

        public MatrixDataPreparer(MeasuredInputs measuredInputs, ILogger<MatrixDataPreparer> logger = null)
        {
            ArgumentNullException.ThrowIfNull(measuredInputs, nameof(measuredInputs));

            MeasuredInputs = measuredInputs;
            GraphBuilder = new GraphBuilder(MeasuredInputs);

            QuadraticProgrammingPreparations();
            _logger = logger;
        }
        private void QuadraticProgrammingPreparations()
        {
            IncidenceMatrix = SparseMatrix.OfArray(GraphBuilder.GetIncidenceMatrix());
            MeasuredValues = SparseVector.OfEnumerable(MeasuredInputs.FlowsData.Select(x => x.Measured));
            MeasureIndicator = SparseMatrix.OfDiagonalArray(MeasuredInputs.FlowsData.Select(x => x.IsMeasured ? 1.0 : 0.0).ToArray());
            StandardDeviation = SparseMatrix.OfDiagonalArray(MeasuredInputs.FlowsData.Select(x =>
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
            UpperMetrologicalBound = DenseVector.OfEnumerable(MeasuredInputs.FlowsData.Select(x => x.UpperMetrologicalBound));
            LowerMetrologicalBound = DenseVector.OfEnumerable(MeasuredInputs.FlowsData.Select(x => x.LowerMetrologicalBound));
            UpperTechnologicalBound = DenseVector.OfEnumerable(MeasuredInputs.FlowsData.Select(x => x.UpperTechnologicalBound));
            LowerTechnologicalBound = DenseVector.OfEnumerable(MeasuredInputs.FlowsData.Select(x => x.LowerTechnologicalBound));
            ReconciledValues = new SparseVector(IncidenceMatrix.RowCount);
            H = MeasureIndicator * StandardDeviation;
            DVector = H * (-1) * MeasuredValues;

            _logger.LogInformation("Matrix data prepared");
        }
    }
}

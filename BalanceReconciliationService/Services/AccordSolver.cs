using Accord.Math;
using Accord.Math.Optimization;
using MathNet.Numerics.LinearAlgebra.Double;

namespace BalanceReconciliationService.Services
{
    public class AccordSolver : ISolver
    {
        private readonly GraphBuilder graphBuilder;
        private readonly MeasuredInputs measuredInputs;
        private SparseVector measuredValues;            // Вектор измеренных значений (x0)
        private SparseMatrix measureIndicator;          // Матрица измеряемости (I)
        private SparseMatrix standardDeviation;         // Матрица метрологической погрешности (W)
        private SparseMatrix incidenceMatrix;           // Матрица инцидентности / связей
        private SparseVector reconciledValues;          // Вектор b
        private DenseVector upperMetrologicalBound;     // Вектор верхних ограничений вектора x
        private DenseVector lowerMetrologicalBound;     // Вектор нижних ограничений вектора x
        private DenseVector upperTechnologicalBound;    // Вектор верхних ограничений вектора x
        private DenseVector lowerTechnologicalBound;    // Вектор нижних ограничений вектора x
        private SparseMatrix H;                         // H = I * W
        private SparseVector dVector;                   // d = H * x0

        public AccordSolver(MeasuredInputs measuredInputs)
        {
            ArgumentNullException.ThrowIfNull(measuredInputs, nameof(measuredInputs));
            
            this.measuredInputs = measuredInputs;
            graphBuilder = new GraphBuilder(measuredInputs);
            QuadraticProgrammingPreparations();
        }

        private void QuadraticProgrammingPreparations()
        {
            incidenceMatrix = SparseMatrix.OfArray(graphBuilder.GetIncidenceMatrix());
            measuredValues = SparseVector.OfEnumerable(measuredInputs.FlowsData.Select(x=>x.Measured));
            measureIndicator = SparseMatrix.OfDiagonalArray(measuredInputs.FlowsData.Select(x => x.IsMeasured ? 1.0 : 0.0).ToArray());
            standardDeviation = SparseMatrix.OfDiagonalArray(measuredInputs.FlowsData.Select(x =>
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
            upperMetrologicalBound = DenseVector.OfEnumerable(measuredInputs.FlowsData.Select(x => x.UpperMetrologicalBound));
            lowerMetrologicalBound = DenseVector.OfEnumerable(measuredInputs.FlowsData.Select(x => x.LowerMetrologicalBound));
            upperTechnologicalBound = DenseVector.OfEnumerable(measuredInputs.FlowsData.Select(x => x.UpperTechnologicalBound));
            lowerTechnologicalBound = DenseVector.OfEnumerable(measuredInputs.FlowsData.Select(x => x.LowerTechnologicalBound));
            reconciledValues = new SparseVector(incidenceMatrix.RowCount); reconciledValues = new SparseVector(incidenceMatrix.RowCount);
            H = measureIndicator * standardDeviation;
            dVector = H * (-1) * measuredValues;           
        }

        public ReconciledOutputs Solve()
        {
            var func = new QuadraticObjectiveFunction(H.ToArray(), dVector.ToArray());
            var constraints = new List<LinearConstraint>();
            //добавление ограничений узлов
            for (var j = 0; j < measuredValues.ToArray().Length; j++)
            {
                if (measuredInputs.ConstraintsSettings.Type == 0 || measureIndicator[j, j] == 0.0)
                {

                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.LesserThanOrEqualTo,
                        Value = measuredInputs.FlowsData[j].UpperTechnologicalBound
                    });

                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                        Value = measuredInputs.FlowsData[j].LowerTechnologicalBound
                    });                  
                }
                else
                {
                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                        Value = measuredInputs.FlowsData[j].UpperMetrologicalBound
                    });

                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.LesserThanOrEqualTo,
                        Value = measuredInputs.FlowsData[j].LowerMetrologicalBound
                    });
                }
            }

            for (var j = 0; j < reconciledValues.ToArray().Length; j++)
            {
                var notNullElements = Array.FindAll(incidenceMatrix.ToArray().GetRow(j), x => Math.Abs(x) > 0.0000001);
                var notNullElementsIndexes = new List<int>();
                for (var k = 0; k < measuredValues.ToArray().Length; k++)
                {
                    if (Math.Abs(incidenceMatrix[j, k]) > 0.0000001)
                    {
                        notNullElementsIndexes.Add(k);
                    }
                }

                constraints.Add(new LinearConstraint(notNullElements.Length)
                {
                    VariablesAtIndices = notNullElementsIndexes.ToArray(),
                    CombinedAs = notNullElements,
                    ShouldBe = ConstraintType.EqualTo,
                    Value = reconciledValues[j]
                });
            }

            var solver = new GoldfarbIdnani(func, constraints);

            DateTime calculationTimeStart = DateTime.Now;
            if (!solver.Minimize())
            {
                throw new ApplicationException("Failed to reconcile");
            }
            DateTime calculationTimeFinish = DateTime.Now;

            double measuredDataDisbalance = incidenceMatrix.Multiply(measuredValues).Subtract(reconciledValues).ToArray().Euclidean();
            double reconciledDataDisbalance = incidenceMatrix.Multiply(SparseVector.OfVector(new DenseVector(solver.Solution))).Subtract(reconciledValues).ToArray().Euclidean();

            var reconciledOutputs = new ReconciledOutputs();
            var reconciledFlowDatas = new List<ReconciledFlowData>();
            for (int i = 0; i < solver.Solution.Length; i++)
            {
                reconciledFlowDatas.Add(new ReconciledFlowData()
                {
                    Id = measuredInputs.FlowsData[i].Id,
                    Name = measuredInputs.FlowsData[i].Name,
                    ReconciliatedValue = solver.Solution[i],
                    SourceId = measuredInputs.FlowsData[i].SourceId,
                    DestinationId = measuredInputs.FlowsData[i].DestinationId,
                    ChosenConstraintUpperBound = (measuredInputs.ConstraintsSettings.Type == 0 || measureIndicator[i, i] == 0.0) // 0 = ConstraintsSettings.ConstraintsSettingsType.Technological
                        ? upperTechnologicalBound[i] : upperMetrologicalBound[i],
                    ChosenConstraintLowerBound = (measuredInputs.ConstraintsSettings.Type == 0 || measureIndicator[i, i] == 0.0) // 0 = ConstraintsSettings.ConstraintsSettingsType.Technological
                        ? lowerTechnologicalBound[i] : lowerMetrologicalBound[i]
                });
            }
            reconciledOutputs.CalculationTime = (calculationTimeFinish - calculationTimeStart).TotalSeconds;
            reconciledOutputs.ReconciledFlowDatas = reconciledFlowDatas;
            reconciledOutputs.MeasuredDataDisbalance = measuredDataDisbalance;
            reconciledOutputs.ReconciledDataDisbalance = reconciledDataDisbalance;
            reconciledOutputs.Status = "Success";

            return reconciledOutputs;
        }
    }
}

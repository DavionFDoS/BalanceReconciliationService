using Accord.Math;
using Accord.Math.Optimization;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Diagnostics;

namespace BalanceReconciliationService.Services
{
    /// <summary>
    /// QPSolver using the Accord.NET library
    /// </summary>
    public class AccordSolver : ISolver
    {
        private readonly MatrixDataPreparer dataPreparer;
        private readonly ILogger<AccordSolver> _logger;
        public AccordSolver(MatrixDataPreparer matrixDataPreparer, ILogger<AccordSolver> logger = null)
        {
            dataPreparer = matrixDataPreparer;
            _logger = logger;
        }

        private List<LinearConstraint> InitializeConstraints()
        {
            var constraints = new List<LinearConstraint>();

            for (var j = 0; j < dataPreparer.MeasuredValues.ToArray().Length; j++)
            {
                if (dataPreparer.MeasuredInputs.ConstraintsSettings.Type == 0 || dataPreparer.MeasureIndicator[j, j] == 0.0)
                {

                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.LesserThanOrEqualTo,
                        Value = dataPreparer.UpperTechnologicalBound[j]
                    });

                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                        Value = dataPreparer.LowerTechnologicalBound[j]
                    });
                }
                else
                {
                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.LesserThanOrEqualTo,
                        Value = dataPreparer.UpperMetrologicalBound[j]
                    });

                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                        Value = dataPreparer.LowerMetrologicalBound[j]
                    });
                }
            }

            for (var j = 0; j < dataPreparer.ReconciledValues.ToArray().Length; j++)
            {
                var notNullElements = Array.FindAll(dataPreparer.IncidenceMatrix.ToArray().GetRow(j), x => Math.Abs(x) > 0.0000001);
                var notNullElementsIndexes = new List<int>();
                for (var k = 0; k < dataPreparer.MeasuredValues.ToArray().Length; k++)
                {
                    if (Math.Abs(dataPreparer.IncidenceMatrix[j, k]) > 0.0000001)
                    {
                        notNullElementsIndexes.Add(k);
                    }
                }

                constraints.Add(new LinearConstraint(notNullElements.Length)
                {
                    VariablesAtIndices = notNullElementsIndexes.ToArray(),
                    CombinedAs = notNullElements,
                    ShouldBe = ConstraintType.EqualTo,
                    Value = dataPreparer.ReconciledValues[j]
                });
            }

            _logger.LogInformation("List of linear constraints has been recieved");

            return constraints;
        }

        public ReconciledOutputs Solve()
        {
            var func = new QuadraticObjectiveFunction(dataPreparer.H.ToArray(), dataPreparer.DVector.ToArray());

            _logger.LogInformation("Quadratic function has been initialized");

            var constraints = InitializeConstraints();

            var solver = new GoldfarbIdnani(func, constraints);

            _logger.LogInformation("Accord solver has been initialized");

            Stopwatch sw = Stopwatch.StartNew();
            if (!solver.Minimize())
            {
                throw new Exception("Exception while trying to reconcile");
            }
            sw.Stop();

            _logger.LogInformation("Function has been minimized in {time} ms", sw.ElapsedMilliseconds);

            var measuredDataDisbalance = dataPreparer.IncidenceMatrix.Multiply(dataPreparer.MeasuredValues)
                .Subtract(dataPreparer.ReconciledValues).ToArray().Euclidean();
            var reconciledDataDisbalance = dataPreparer.IncidenceMatrix.Multiply(SparseVector.OfVector(new DenseVector(solver.Solution)))
                .Subtract(dataPreparer.ReconciledValues).ToArray().Euclidean();

            var reconciledOutputs = new ReconciledOutputs();
            var reconciledFlowDatas = new List<ReconciledFlowData>();

            for (int i = 0; i < solver.Solution.Length; i++)
            {
                reconciledFlowDatas.Add(new ReconciledFlowData()
                {
                    Id = dataPreparer.MeasuredInputs.FlowsData[i].Id,
                    Name = dataPreparer.MeasuredInputs.FlowsData[i].Name,
                    ReconciledValue = solver.Solution[i],
                    SourceId = dataPreparer.MeasuredInputs.FlowsData[i].SourceId,
                    DestinationId = dataPreparer.MeasuredInputs.FlowsData[i].DestinationId,
                    ChosenConstraintUpperBound = (dataPreparer.MeasuredInputs.ConstraintsSettings.Type == 0 || dataPreparer.MeasureIndicator[i, i] == 0.0) 
                        ? dataPreparer.UpperTechnologicalBound[i] : dataPreparer.UpperMetrologicalBound[i],
                    ChosenConstraintLowerBound = (dataPreparer.MeasuredInputs.ConstraintsSettings.Type == 0 || dataPreparer.MeasureIndicator[i, i] == 0.0) 
                        ? dataPreparer.LowerTechnologicalBound[i] : dataPreparer.LowerMetrologicalBound[i]  // 0 = ConstraintsSettings.ConstraintsSettingsType.Technological
                });
            }

            reconciledOutputs.CalculationTime = sw.ElapsedMilliseconds;
            reconciledOutputs.ReconciledFlowDatas = reconciledFlowDatas;
            reconciledOutputs.MeasuredDataDisbalance = measuredDataDisbalance;
            reconciledOutputs.ReconciledDataDisbalance = reconciledDataDisbalance;
            reconciledOutputs.Status = "Success";

            _logger.LogInformation("Calculations has been completed with status: {status}", reconciledOutputs.Status);

            return reconciledOutputs;
        }
    }
}

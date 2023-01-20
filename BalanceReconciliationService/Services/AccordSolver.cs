using Accord.Math;
using Accord.Math.Optimization;
using BalanceReconciliationService.Enums;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Diagnostics;

namespace BalanceReconciliationService.Services
{
    /// <summary>
    /// QPSolver using the Accord.NET library
    /// </summary>
    public class AccordSolver : ISolver
    {
        private readonly MatrixDataPreparer _matrixDataPreparer;

        public ConstraintsType ConstraintsType { get; }

        public AccordSolver(MatrixDataPreparer matrixDataPreparer, ConstraintsType constraintsType)
        {
            _matrixDataPreparer = matrixDataPreparer;
            ConstraintsType = constraintsType;
        }

        private List<LinearConstraint> InitializeConstraints()
        {
            var constraints = new List<LinearConstraint>();

            for (var j = 0; j < _matrixDataPreparer.MeasuredValues.ToArray().Length; j++)
            {
                if (ConstraintsType == 0 || _matrixDataPreparer.MeasureIndicator[j, j] == 0.0)
                {

                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.LesserThanOrEqualTo,
                        Value = _matrixDataPreparer.UpperTechnologicalBound[j]
                    });

                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                        Value = _matrixDataPreparer.LowerTechnologicalBound[j]
                    });
                }
                else
                {
                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.LesserThanOrEqualTo,
                        Value = _matrixDataPreparer.UpperMetrologicalBound[j]
                    });

                    constraints.Add(new LinearConstraint(1)
                    {
                        VariablesAtIndices = new[] { j },
                        ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                        Value = _matrixDataPreparer.LowerMetrologicalBound[j]
                    });
                }
            }

            for (var j = 0; j < _matrixDataPreparer.ReconciledValues.ToArray().Length; j++)
            {
                var notNullElements = Array.FindAll(_matrixDataPreparer.IncidenceMatrix.ToArray().GetRow(j), x => Math.Abs(x) > 0.0000001);
                var notNullElementsIndexes = new List<int>();
                for (var k = 0; k < _matrixDataPreparer.MeasuredValues.ToArray().Length; k++)
                {
                    if (Math.Abs(_matrixDataPreparer.IncidenceMatrix[j, k]) > 0.0000001)
                    {
                        notNullElementsIndexes.Add(k);
                    }
                }

                constraints.Add(new LinearConstraint(notNullElements.Length)
                {
                    VariablesAtIndices = notNullElementsIndexes.ToArray(),
                    CombinedAs = notNullElements,
                    ShouldBe = ConstraintType.EqualTo,
                    Value = _matrixDataPreparer.ReconciledValues[j]
                });
            }

            Log.Information("List of linear constraints has been recieved");

            return constraints;
        }

        public ReconciledOutputs Solve()
        {
            var func = new QuadraticObjectiveFunction(_matrixDataPreparer.H.ToArray(), _matrixDataPreparer.DVector.ToArray());

            Log.Information("Quadratic function has been initialized");

            var constraints = InitializeConstraints();

            var solver = new GoldfarbIdnani(func, constraints);

            Log.Information("Accord solver has been initialized");

            Stopwatch sw = Stopwatch.StartNew();
            if (!solver.Minimize())
            {
                throw new Exception("Exception while trying to reconcile");
            }
            sw.Stop();

            Log.Information("Function has been minimized in {time} ms", sw.ElapsedMilliseconds);

            var measuredDataDisbalance = _matrixDataPreparer.IncidenceMatrix.Multiply(_matrixDataPreparer.MeasuredValues)
                .Subtract(_matrixDataPreparer.ReconciledValues).ToArray().Euclidean();
            var reconciledDataDisbalance = _matrixDataPreparer.IncidenceMatrix.Multiply(SparseVector.OfVector(new DenseVector(solver.Solution)))
                .Subtract(_matrixDataPreparer.ReconciledValues).ToArray().Euclidean();

            var reconciledOutputs = new ReconciledOutputs();
            var reconciledFlowDatas = new List<ReconciledFlowData>();

            for (int i = 0; i < solver.Solution.Length; i++)
            {
                reconciledFlowDatas.Add(new ReconciledFlowData
                {
                    Id = _matrixDataPreparer.FlowsData[i].Id,
                    Name = _matrixDataPreparer.FlowsData[i].Name,
                    ReconciledValue = solver.Solution[i],
                    SourceId = _matrixDataPreparer.FlowsData[i].SourceId,
                    DestinationId = _matrixDataPreparer.FlowsData[i].DestinationId,
                    UpperTechnologicalBound = _matrixDataPreparer.UpperTechnologicalBound[i],
                    LowerTechnologicalBound = _matrixDataPreparer.LowerTechnologicalBound[i],
                    UpperMetrologicalBound = _matrixDataPreparer.UpperMetrologicalBound[i],
                    LowerMetrologicalBound = _matrixDataPreparer.LowerMetrologicalBound[i],
                    IsExcluded = _matrixDataPreparer.FlowsData[i].IsExcluded,
                    IsArtificial = _matrixDataPreparer.FlowsData[i].IsArtificial,
                    IsMeasured = _matrixDataPreparer.FlowsData[i].IsMeasured,
                    Tolerance = _matrixDataPreparer.FlowsData[i].Tolerance,
                    Measured = _matrixDataPreparer.FlowsData[i].Measured,
                    Correction = solver.Solution[i] - _matrixDataPreparer.FlowsData[i].Measured
                });
            }

            reconciledOutputs.CalculationTime = sw.ElapsedMilliseconds;
            reconciledOutputs.ReconciledFlowDatas = reconciledFlowDatas;
            reconciledOutputs.MeasuredDataDisbalance = measuredDataDisbalance;
            reconciledOutputs.ReconciledDataDisbalance = reconciledDataDisbalance;
            reconciledOutputs.Status = "Success";

            Log.Information("Calculations has been completed with status: {status}", reconciledOutputs.Status);

            return reconciledOutputs;
        }
    }
}
using BalanceReconciliationService.Enums;
using BalanceReconciliationService.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Resources;

namespace BalanceReconciliationService.Services;

public class GrossErrorDetectionService : IGrossErrorDetectionService
{
    private readonly GlobalTestCalculator _globalTestCalculator;

    public GrossErrorDetectionService(GlobalTestCalculator globalTestCalculator)
    {
        _globalTestCalculator = globalTestCalculator;
    }

    public IEnumerable<GrossErrorDetectionAndReconcilationResult> GrossErrorDetectionAndReconcilationByTree(
        ICollection<FlowData> flows, ConstraintsType constraintsType,
        int branchCount, int maxTreeHeight, int maxSolutionsCount, ICollection<GrossErrorType> errorTypes)
    {
        ArgumentNullException.ThrowIfNull(flows, nameof(flows));

        var grossErrorDetectionResults =
            GrossErrorDetectionByTree(flows, branchCount, maxTreeHeight, maxSolutionsCount, errorTypes)
                .ToList();

        if (!grossErrorDetectionResults.Any())
        {
            return new List<GrossErrorDetectionAndReconcilationResult>();
        }

        var result = new List<GrossErrorDetectionAndReconcilationResult>();

        foreach (var grossErrorDetectionResult in grossErrorDetectionResults)
        {
            var scenario = new List<FlowData>(flows);
            foreach (var flowWithError in grossErrorDetectionResult.FlowsWithErrors)
            {
                scenario.Add(flowWithError);
            }

            var dataPreparer = new MatrixDataPreparer(scenario);

            var solver = new AccordSolver(dataPreparer, constraintsType);

            var solution = solver.Solve();

            var solutionFlows = new List<ReconciledFlowData>(solution.ReconciledFlowDatas);

            var reducedFlows = HandleReconciledFlows(solutionFlows);

            result.Add(new GrossErrorDetectionAndReconcilationResult()
            {
                Flows = reducedFlows,
                MeasuredDataDisbalance = solution.MeasuredDataDisbalance,
                ReconciledDataDisbalance = solution.ReconciledDataDisbalance
            });
        }

        return result;
    }

    public IEnumerable<GrossErrorDetectionResult> GrossErrorDetectionByTree(IEnumerable<FlowData> flows,
        int branchCount, int maxTreeHeight, int maxSolutionsCount, ICollection<GrossErrorType> errorTypes)
    {
        ArgumentNullException.ThrowIfNull(flows, nameof(flows));

        var thresholdValue = 1.0;

        var treeStorage = new GedTreeNode<GlrResult>(null, null, 0);

        var scenаrio = flows.ToList();

        if (!scenаrio.Any())
        {
            throw new Exception(ErrorsResources.EmptyFlowsError);
        }

        var dataPreparer = new MatrixDataPreparer(scenаrio);

        var measured = dataPreparer.MeasuredValues.ToArray();
        var incedenceMatrix = dataPreparer.IncidenceMatrix.ToArray();
        var measurability = dataPreparer.MeasureIndicator.Diagonal().ToArray();
        var tolerance = dataPreparer.Tolerance;

        var globalTestResult = _globalTestCalculator.GlobalTest(measured, incedenceMatrix, measurability, tolerance);

        if (globalTestResult >= thresholdValue)
        {
            var solutions = GLRTest(scenаrio, globalTestResult, errorTypes)
                .Take(branchCount);

            foreach (var solution in solutions)
            {
                treeStorage.AddChild(solution);
            }

            for (var treeHeight = 1; treeHeight < maxTreeHeight; treeHeight++)
            {
                var solutionsCount = treeStorage.GetLeaves(treeStorage)
                    .Count(x => x.Data?.GlobalTestResult < thresholdValue);

                if (solutionsCount >= maxSolutionsCount)
                {
                    break;
                }

                var treeNodesAtHeight = treeStorage.GetChildrenOfTreeAtHeight(treeHeight);

                var tasks = new List<Task>();

                foreach (var treeNode in treeNodesAtHeight)
                {
                    if (treeNode == null)
                    {
                        throw new Exception(ErrorsResources.GrossErrorDetectionWithTreeError);
                    }

                    var task = Task.Run(() =>
                    {
                        var treeNodeData = treeNode.Data;
                        if (treeNodeData == null)
                        {
                            throw new Exception(ErrorsResources.GedTreeNodeDataError);
                        }

                        var flowWithError = treeNodeData.Flow;
                        if (flowWithError == null)
                        {
                            throw new Exception(ErrorsResources.GrossErrorDetectionWithTreeError);
                        }

                        if (treeNodeData.GlobalTestResult < thresholdValue)
                        {
                            solutionsCount++;
                            return;
                        }

                        var scenarioFlows = new List<FlowData>(scenаrio);
                        scenarioFlows.AddRange(treeNode.GetParentHierarchy()
                            .Where(x => x.Parent != null && x.Data != null && x.Data.Flow != null)
                            .Select(x =>
                                x.Data?.Flow ?? throw new Exception(ErrorsResources.GrossErrorDetectionWithTreeError)));

                        var treeNodeDataFlow = treeNodeData.Flow;
                        if (treeNodeDataFlow == null)
                        {
                            throw new Exception(ErrorsResources.GrossErrorDetectionWithTreeError);
                        }

                        scenarioFlows.Add(treeNodeDataFlow);

                        solutions = GLRTest(scenarioFlows, globalTestResult, errorTypes)
                            .Take(branchCount);

                        if (solutions == null)
                        {
                            throw new Exception(ErrorsResources.GrossErrorDetectionWithTreeError);
                        }

                        foreach (var solution in solutions)
                        {
                            treeNode.AddChild(solution);
                        }
                    });

                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray());
            }

        }

        var leaves = treeStorage.GetLeaves(treeStorage);
        ArgumentNullException.ThrowIfNull(leaves);

        var sceneries = new List<IEnumerable<GlrResult>>();
        foreach (var leaf in leaves)
        {
            if (leaf == null)
            {
                throw new Exception(ErrorsResources.GrossErrorDetectionWithTreeError);
            }

            if (leaf.Parent == null)
            {
                continue;
            }

            if (leaf.Data?.GlobalTestResult >= thresholdValue)
            {
                continue;
            }

            var leafData = leaf.Data;
            if (leafData == null)
            {
                throw new Exception(ErrorsResources.GrossErrorDetectionWithTreeError);
            }

            var glrIterationResults = new List<GlrResult> { leafData };
            var parentNodes = leaf.GetParentHierarchy().Where(node => node.Parent != null).Select(node => node.Data);

            if (parentNodes == null)
            {
                throw new Exception(ErrorsResources.GrossErrorDetectionWithTreeError);
            }

            glrIterationResults.AddRange(parentNodes!);
            sceneries.Add(glrIterationResults);
        }

        // Выполняем группировку решений по количеству ошибок,
        // а затем каждую группу сортируем по увеличению значения глобального теста
        var orderedSceneries = sceneries
            .GroupBy(solution => solution.Count())
            .SelectMany(group => group.OrderBy(x => x.FirstOrDefault()?.GlobalTestResult)).ToList();

        return orderedSceneries.Select(glrIterationResults => new GrossErrorDetectionResult()
        {
            FlowsWithErrors = glrIterationResults.Select(glrIterationResult =>
                glrIterationResult.Flow ?? throw new Exception(ErrorsResources.GrossErrorDetectionWithTreeError))
        });
    }

    private IEnumerable<GlrResult> GLRTest(ICollection<FlowData> flows,
         double globalTestValue, ICollection<GrossErrorType> errorTypes, bool async = true)
    {
        ArgumentNullException.ThrowIfNull(flows, nameof(flows));

        var artificialFlows = CreateArtificialFlows(flows, errorTypes);

        var glrResult = new List<GlrResult>();

        var GlrIteration = (FlowData artificialFlow) =>
        {
            var scenario = new List<FlowData>(flows)
            {
                artificialFlow
            };

            var dataPreparer = new MatrixDataPreparer(scenario);

            var measured = dataPreparer.MeasuredValues.ToArray();
            var incedenceMatrix = dataPreparer.IncidenceMatrix.ToArray();
            var measurability = dataPreparer.MeasureIndicator.Diagonal().ToArray();
            var tolerance = dataPreparer.Tolerance;

            var globalTest = _globalTestCalculator.GlobalTest(measured, incedenceMatrix, measurability, tolerance);

            var delta = globalTestValue - globalTest;

            return new GlrResult()
            {
                Value = delta,
                GlobalTestResult = globalTest,
                Flow = artificialFlow
            };
        };

        if (async)
        {
            var tasks = new List<Task>();

            foreach (var artificialFlow in artificialFlows)
            {
                tasks.Add(Task.Run(() => GlrIteration(artificialFlow)));
            }

            Task.WaitAll(tasks.ToArray());

            var glrIterationResults = tasks.Select(task => (task as Task<GlrResult>)?.Result).ToList();
            if (glrIterationResults == null || glrIterationResults.Any(res => res == null))
            {
                throw new Exception(ErrorsResources.GeneralizedLikelihoodRatioTestError);
            }

            return glrIterationResults.OrderByDescending(glr => glr!.Value)!;
        }

        foreach (var artificialFlow in artificialFlows)
        {
            glrResult.Add(GlrIteration(artificialFlow));
        }

        return glrResult.OrderByDescending(glr => glr.Value);
    }

    private IEnumerable<FlowData> CreateArtificialFlows(ICollection<FlowData> flows, ICollection<GrossErrorType> errorTypes)
    {
        var sourceNodes = flows.GroupBy(fl => fl.SourceId).Select(gr => gr.Key).ToArray();

        var destinationNodes = flows.GroupBy(fl => fl.DestinationId).Select(gr => gr.Key).ToArray();

        var artificialFlows = new List<FlowData>();

        foreach (var sourceNode in sourceNodes)
        {
            foreach (var destinationNode in destinationNodes)
            {
                if (sourceNode == destinationNode)
                {
                    continue;
                }

                var flow = flows.FirstOrDefault(flow =>
                    flow.SourceId == sourceNode && flow.DestinationId == destinationNode);

                if (flow != null && (!errorTypes.Any() || errorTypes.Contains(GrossErrorType.Measure)))
                {
                    if (flow.IsExcluded)
                    {
                        continue;
                    }

                    artificialFlows.Add(new FlowData()
                    {
                        Id = flow.Id,
                        Name = flow.Name,
                        SourceId = flow.SourceId,
                        DestinationId = flow.DestinationId,
                        Measured = flow.Measured,
                        Tolerance = flow.Tolerance,
                        IsMeasured = false,
                        IsExcluded = flow.IsExcluded,
                        UpperMetrologicalBound = (flow?.UpperMetrologicalBound ?? double.MaxValue) - flow.Measured,
                        LowerMetrologicalBound = (flow?.LowerMetrologicalBound ?? 0) - flow.Measured,
                        UpperTechnologicalBound = (flow?.UpperTechnologicalBound ?? double.MaxValue) - flow.Measured,
                        LowerTechnologicalBound = (flow?.LowerTechnologicalBound ?? 0) - flow.Measured,
                        IsArtificial = true,
                    });

                    continue;
                }

                if (!errorTypes.Contains(GrossErrorType.Leak) && !errorTypes.Contains(GrossErrorType.Unaccounted))
                {
                    continue;
                }

                var id = Guid.NewGuid();

                artificialFlows.Add(new FlowData()
                {
                    Id = id,
                    Name = string.Format($"Artificial flow from {sourceNode} to {destinationNode} ", id),
                    SourceId = sourceNode,
                    DestinationId = destinationNode,
                    Measured = 0.0,
                    Tolerance = 0.0,
                    IsMeasured = false,
                    IsExcluded = false,
                    UpperMetrologicalBound = double.MaxValue,
                    LowerMetrologicalBound = 0,
                    UpperTechnologicalBound = double.MaxValue,
                    LowerTechnologicalBound = 0,
                    IsArtificial = true,
                });
            }
        }

        return artificialFlows;
    }

    private IEnumerable<ReconciledFlowWithErrorType> HandleReconciledFlows(ICollection<ReconciledFlowData> flows)
    {
        var artificialFlows = new List<ReconciledFlowWithErrorType>();

        var resultedFlows = new List<ReconciledFlowWithErrorType>();

        //use Mapper

        foreach (var flow in flows)
        {
            if (flow.IsArtificial)
            {
                artificialFlows.Add(new ReconciledFlowWithErrorType
                {
                    Id = flow.Id,
                    SourceId = flow.SourceId,
                    DestinationId = flow.DestinationId,
                    Name = flow.Name,
                    Measured = flow.Measured,
                    UpperMetrologicalBound = flow.UpperMetrologicalBound,
                    LowerMetrologicalBound = flow.LowerMetrologicalBound,
                    UpperTechnologicalBound = flow.UpperTechnologicalBound,
                    LowerTechnologicalBound = flow.LowerTechnologicalBound,
                    Tolerance = flow.Tolerance,
                    IsMeasured = flow.IsMeasured,
                    IsExcluded = flow.IsExcluded,
                    IsArtificial = flow.IsArtificial,
                });
            }
            else
            {
                resultedFlows.Add(new ReconciledFlowWithErrorType
                {
                    Id = flow.Id,
                    SourceId = flow.SourceId,
                    DestinationId = flow.DestinationId,
                    Name = flow.Name,
                    Measured = flow.Measured,
                    UpperMetrologicalBound = flow.UpperMetrologicalBound,
                    LowerMetrologicalBound = flow.LowerMetrologicalBound,
                    UpperTechnologicalBound = flow.UpperTechnologicalBound,
                    LowerTechnologicalBound = flow.LowerTechnologicalBound,
                    Tolerance = flow.Tolerance,
                    IsMeasured = flow.IsMeasured,
                    IsExcluded = flow.IsExcluded,
                    IsArtificial = flow.IsArtificial,
                });
            }
        }

        foreach (var artificialFlow in artificialFlows)
        {
            var identicalFlow = resultedFlows.FirstOrDefault(flow => flow.Id == artificialFlow.Id);

            if (identicalFlow == null)
            {
                artificialFlow.GrossErrorType = artificialFlow.DestinationId == null
                    ? GrossErrorType.Leak
                    : GrossErrorType.Unaccounted;

                resultedFlows.Add(artificialFlow);
                continue;
            }

            var index = resultedFlows.IndexOf(identicalFlow);
            resultedFlows[index].ReconciledValue += artificialFlow.ReconciledValue;
            resultedFlows[index].Correction = resultedFlows[index].ReconciledValue - resultedFlows[index].Measured;
            resultedFlows[index].GrossErrorType = GrossErrorType.Measure;
        }

        return resultedFlows;
    }

}
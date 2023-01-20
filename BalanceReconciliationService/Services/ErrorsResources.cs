namespace BalanceReconciliationService.Services;

public static class ErrorsResources
{
    public static string GedTreeNodeParentHierarchyError => "The node was null";

    public static string EmptyFlowsError => "The flows was empty";

    public static string GrossErrorDetectionWithTreeError => "The error occured while detecting gross errors";

    public static string GedTreeNodeDataError => "Node data was null";

    public static string GeneralizedLikelihoodRatioTestError => "The error occured while performing generalized likelihood ratio test";

    public static string GrossErrorDetectionAndReconcilation => "The error occured while trying to detect an error and reconcile it's measure";

}
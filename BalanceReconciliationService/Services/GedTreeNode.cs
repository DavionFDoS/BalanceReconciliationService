using System.Collections.Concurrent;

namespace BalanceReconciliationService.Services
{
    public class GedTreeNode<T>
    {
        public T Data { get; }

        public BlockingCollection<GedTreeNode<T>> Children { get; }

        public int Height { get; }

        public GedTreeNode<T> Parent { get; }

        public GedTreeNode(T data, GedTreeNode<T> parent, int height)
        {
            Data = data;
            Children = new BlockingCollection<GedTreeNode<T>>();
            Parent = parent;
            Height = height;
        }

        public void AddChild(T data)
        {
            var node = new GedTreeNode<T>(data, this, Height + 1);
            Children.Add(node);
        }

        public GedTreeNode<T> GetChild(int i)
        {
            return Children.ElementAtOrDefault(i);
        }

        public IEnumerable<GedTreeNode<T>> GetChildrenOfTreeAtHeight(int height)
        {
            return GetChildrenOfHeight(GetMainNode(), height);
        }

        public IEnumerable<GedTreeNode<T>> GetLeaves(GedTreeNode<T> node)
        {
            var children = new List<GedTreeNode<T>>();

            if (!node.Children.Any())
            {
                children.Add(node);

                return children;
            }

            foreach (var child in node.Children)
            {
                children.AddRange(GetLeaves(child));
            }

            return children;
        }
        public GedTreeNode<T> GetMainNode()
        {
            return GetParentHierarchy().Last();
        }

        public ICollection<GedTreeNode<T>> GetParentHierarchy()
        {
            var node = Parent ?? this;

            if (node == null)
            {
                throw new Exception(ErrorsResources.GedTreeNodeParentHierarchyError);
            }

            var result = new List<GedTreeNode<T>> { node };

            while (node.Parent != null)
            {
                node = node.Parent!;
                result.Add(node);
            }

            return result;
        }

        private static IEnumerable<GedTreeNode<T>> GetChildrenOfHeight(GedTreeNode<T> node, int height)
        {
            if (height > node.Height)
            {
                return node.Children.SelectMany(x => GetChildrenOfHeight(x, height));
            }

            if (height == node.Height)
            {
                return new List<GedTreeNode<T>> { node };
            }

            return new List<GedTreeNode<T>>();
        }

    }
}
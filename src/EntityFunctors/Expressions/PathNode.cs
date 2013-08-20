namespace EntityFunctors.Expressions
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;

    public class PathNode
    {
        private IList<PathNode> _children = new List<PathNode>();

        public IEnumerable<PathNode> Children
        {
            get
            {
                if (_children == null)
                    yield break;

                foreach (var child in _children)
                    yield return child;
            }
        }

        public PathNode Parent { get; set; }

        private readonly Expression _expression;

        private Expression _replacement;

        public PathNode(Expression expression)
        {
            _expression = expression;
        }

        public PathNode(Expression expression, PathNode parent)
            : this(expression)
        {
            Contract.Assert(parent != null);

            Parent = parent;
        }

        public PathNode AddChild(Expression node)
        {
            var child = new PathNode(node, this);
            (_children ?? (_children = new List<PathNode>())).Add(child);
            return child;
        }

        public void SetReplacement(Expression replacement)
        {
            Contract.Assert(_expression != replacement);

            _replacement = replacement;
        }

        public bool IsCurrentOrChild(PathNode node)
        {
            var current = this;

            while (current != null)
            {
                if (current == node)
                    return true;

                current = current.Parent;
            }

            return false;
        }

        public override string ToString()
        {
            return string.Join("->", GetSegments().Reverse());
        }

        public IEnumerable<PathNode> Flatten()
        {
            return Flatten(this);
        }

        private static IEnumerable<PathNode> Flatten(PathNode item)
        {
            return Pluralize(item).Concat(item.Children.SelectMany(Flatten));
        }

        private static IEnumerable<T> Pluralize<T>(T item)
        {
            yield return item;
        }

        private IEnumerable<string> GetSegments()
        {
            var current = this;

            while (current != null)
            {
                yield return current._expression != null ? current._expression.NodeType.ToString() : "[x]";
                current = current.Parent;
            }
        }
    }
}
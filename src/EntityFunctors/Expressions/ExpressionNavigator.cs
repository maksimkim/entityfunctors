namespace EntityFunctors.Expressions
{
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public class ExpressionNavigator
    {
        private PathNode _current;

        public PathNode Current
        {
            get { return _current; }
        }

        public bool Revisit
        {
            get { return _revisit.Count > 0; }
        }

        private readonly Stack<object> _revisit = new Stack<object>();

        public void EnterRevisit()
        {
            _revisit.Push(null);
        }

        public void ExitRevisit()
        {
            _revisit.Pop();
        }

        public void Down(Expression node)
        {
            if (!Revisit)
                _current = _current == null ? new PathNode(node) : _current.AddChild(node);
        }

        public void Up()
        {
            if (!Revisit)
                _current = _current.Parent;
        }

        public void SetReplacement(Expression replacement)
        {
            _current.SetReplacement(replacement);
        }
    }
}
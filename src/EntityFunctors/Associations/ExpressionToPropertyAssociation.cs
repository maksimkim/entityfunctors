namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;

    public class ExpressionToPropertyAssociation<TSource, TTarget> : IMappingAssociation, IAccessable
    {
        public LambdaExpression Source { get; private set; }

        public PropertyPart Target { get; private set; }

        public MappingDirection Direction { get; private set; }

        public ExpressionToPropertyAssociation(LambdaExpression source, PropertyPart target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);

            Contract.Assert(source.ReturnType == target.Property.PropertyType);

            Source = source;

            Target = target;

            Direction = MappingDirection.Read;
        }

        public Expression BuildMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression propertyKeys, IMappingRegistry registry)
        {
            Contract.Assert(@from.Type == typeof(TSource) || @from.Type == typeof(TTarget));
            Contract.Assert(to.Type == typeof(TSource) || to.Type == typeof(TTarget));

            var direction = @from.Type == typeof(TTarget) ? MappingDirection.Write : MappingDirection.Read;

            if ((Direction & direction) != direction)
                return Expression.Empty();

            var donor =
                direction == MappingDirection.Read
                ? Rewrite(null, @from)
                : Expression.Property(from, Target.Property);

            var aceptor =
                direction == MappingDirection.Read
                ? Expression.Property(to, Target.Property)
                : Rewrite(null, to);

            return Expression.Assign(aceptor, donor);
        }

        public PropertyInfo TargetProperty
        {
            get { return Target.Property; }
        }

        public Expression Rewrite(Expression original, ParameterExpression parameter)
        {
            var rewriter = new ParameterRewriter(Source.Parameters[0], parameter);

            var rewriten = rewriter.Visit(Source.Body);

            return rewriten;
        }

        public IEnumerable<KeyValuePair<PropertyInfo, Delegate>> ValueConverters
        {
            get { yield break; }
        }

        public void ReadOnly()
        {
            Direction = MappingDirection.Read;
        }

        public void WriteOnly()
        {
            if (!IsPropertyChain(Source))
                throw new InvalidOperationException("Only property chain expression can be set writable");
            
            Direction = MappingDirection.Write;
        }

        public void Read()
        {
            AddDirection(MappingDirection.Read);
        }

        public void Write()
        {
            if (!IsPropertyChain(Source))
                throw new InvalidOperationException("Only property chain expression can be set writable");

            AddDirection(MappingDirection.Write);
        }

        private void AddDirection(MappingDirection val)
        {
            if ((Direction & val) != val)
                Direction |= val;
        }

        private bool IsPropertyChain(LambdaExpression expression)
        {
            var current = expression.Body;

            while (current != null)
            {
                PropertyInfo p;

                if (current.NodeType == ExpressionType.Parameter)
                    return true;
                
                if (!current.TryGetProperty(out p))
                    return false;

                current = (current as MemberExpression).Expression;
            }

            return true;
        }

        private class ParameterRewriter : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;

            public ParameterRewriter(ParameterExpression from, ParameterExpression to)
            {
                Contract.Assert(@from != null);
                Contract.Assert(to != null);
                _from = @from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (_from != _to && node == _from)
                    return _to;
                
                return base.VisitParameter(node);
            }
        }
    }
}
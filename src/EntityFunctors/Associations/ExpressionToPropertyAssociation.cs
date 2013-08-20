namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;

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

        public Expression BuildMapper(ParameterExpression @from, ParameterExpression to, IMappingRegistry registry, ParameterExpression expands)
        {
            if (!(@from.Type == typeof(TSource) && to.Type == typeof(TTarget)))
                //return nothing, cause ExpressionToPropertyAssociation allows only one way mapping from TSource to TTarget
                return Expression.Empty();

            return Expression.Assign(
                Expression.Property(to, Target.Property),
                Expression.Invoke(Source, @from)
            );
        }

        public PropertyInfo RewritableProperty
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
            Direction = MappingDirection.Write;
        }

        public void Read()
        {
            AddDirection(MappingDirection.Read);
        }

        public void Write()
        {
            AddDirection(MappingDirection.Write);
        }

        private void AddDirection(MappingDirection val)
        {
            if ((Direction & val) != val)
                Direction |= val;
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
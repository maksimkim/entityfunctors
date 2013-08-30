namespace EntityFunctors.Associations.Impl
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;
    using EntityFunctors.Extensions;
    using EntityFunctors.Associations.Fluent;

    public class ExpressionToPropertyAssociation<TSource, TTarget, TProperty> 
        : IMappingAssociation, IAccessable
        where TSource : class
        where TTarget : class, new()
    {
        public string Key { get; private set; }

        public MappingDirection Direction { get; private set; }

        public LambdaExpression Source { get; private set; }

        public LambdaExpression Target { get; private set; }

        public ExpressionToPropertyAssociation(Expression<Func<TSource, TProperty>> source, Expression<Func<TTarget, TProperty>> target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);
            Contract.Assert(source.ReturnType == target.ReturnType);
            
            PropertyInfo prop;
            Contract.Assert(target.Body.TryGetProperty(out prop));

            Source = source;
            Target = target;

            Key = Target.GetProperty().GetName();

            Direction = MappingDirection.Read;
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
    }
}
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

            PropertyInfo prop;
            Contract.Assert(target.Body.TryGetProperty(out prop));

            if (source.Body is MemberExpression && (source.Body as MemberExpression).Expression == source.Parameters[0])
                throw new ArgumentException("ExpressionToProperty association cannot be used for mapping properties");

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
    }
}
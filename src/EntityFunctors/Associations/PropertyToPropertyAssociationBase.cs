namespace EntityFunctors.Associations
{
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;
    using Fluent;

    public abstract class PropertyToPropertyAssociationBase<TSource, TTarget> 
        : IMappingAssociation, IAccessable
        where TSource : class
        where TTarget : class, new()
    {
        public string Key { get; private set; }

        public MappingDirection Direction { get; private set; }

        public LambdaExpression Source { get; private set; }

        public LambdaExpression Target { get; private set; }

        protected PropertyToPropertyAssociationBase(LambdaExpression source, LambdaExpression target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);
            
            PropertyInfo prop;
            Contract.Assert(source.Body.TryGetProperty(out prop));
            Contract.Assert(target.Body.TryGetProperty(out prop));

            Source = source;
            Target = target;

            Key = Target.GetProperty().GetName();

            Direction = MappingDirection.All;
        }

        public Expression BuildMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression propertyKeys, IMappingRegistry registry)
        {
            Contract.Assert(@from.Type == typeof(TSource) || @from.Type == typeof(TTarget));
            Contract.Assert(to.Type == typeof(TSource) || to.Type == typeof(TTarget));

            var direction = @from.Type == typeof(TTarget) ? MappingDirection.Write : MappingDirection.Read;

            var donorAccessor = direction == MappingDirection.Write ? Target : Source;
            var acceptorAccessor = direction == MappingDirection.Write ? Source : Target;

            if ((Direction & direction) != direction)
                return Expression.Empty();

            var acceptor = acceptorAccessor.Apply(to);
            var donor = (MemberExpression)donorAccessor.Apply(@from);

            Expression result = Expression.Assign(acceptor, BuildDonor(donor, direction));

            if (direction == MappingDirection.Write && propertyKeys != null)
                result = Expression.IfThen(
                    Expression.OrElse(
                        propertyKeys.CreateCheckForDefault(),
                        propertyKeys.CreateContains(Expression.Constant(Key, typeof(string)))
                    ),
                    result
                );

            return result;
        }

        protected abstract Expression BuildDonor(MemberExpression donorAccessor, MappingDirection direction);

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
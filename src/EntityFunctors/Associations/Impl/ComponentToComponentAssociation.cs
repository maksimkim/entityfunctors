namespace EntityFunctors.Associations.Impl
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;
    using EntityFunctors.Extensions;
    using EntityFunctors.Associations.Fluent;

    public class ComponentToComponentAssociation<TSource, TSourceComponent, TTarget, TTargetComponent> 
        : IMappingAssociation, IComponentAssociation, IAccessable, IExpandable
        where TSource : class 
        where TSourceComponent : class
        where TTarget : class, new()
        where TTargetComponent : class, new()
    {
        public string Key { get; private set; }

        public MappingDirection Direction { get; private set; }

        public LambdaExpression Source { get; private set; }

        public LambdaExpression Target { get; private set; }

        public bool Expand { get; private set; }

        public TypeMapKey ComponentKey { get; private set; }

        public ComponentToComponentAssociation(Expression<Func<TSource, TSourceComponent>> source, Expression<Func<TTarget, TTargetComponent>> target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);
            
            PropertyInfo prop;
            Contract.Assert(source.Body.TryGetProperty(out prop));
            Contract.Assert(target.Body.TryGetProperty(out prop));

            Source = source;
            Target = target;

            target.GetProperty();

            Key = Target.GetProperty().GetName();

            Direction = MappingDirection.All;

            ComponentKey = new TypeMapKey(Source.ReturnType, Target.ReturnType);
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

        public void Expandable()
        {
            Expand = true;
        }
    }
}
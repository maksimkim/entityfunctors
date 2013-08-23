namespace EntityFunctors.Associations
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;

    [ContractClass(typeof (ContactForMappingAssociation))]
    public interface IMappingAssociation
    {
        Expression BuildMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression propertyKeys, IMappingRegistry registry);

        Expression Build(Expression arg);

        PropertyInfo TargetProperty { get; }

        MappingDirection Direction { get; }

        Expression Rewrite(Expression original, Expression replacement);

        IEnumerable<TypeMapKey> ChildMapKeys { get; }
    }

    [ContractClassFor(typeof(IMappingAssociation))]
    public abstract class ContactForMappingAssociation : IMappingAssociation
    {
        public Expression BuildMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression propertyKeys, IMappingRegistry registry)
        {
            Contract.Assert(@from != null);
            Contract.Assert(@to != null);
            Contract.Assert(registry != null);

            return null;
        }

        public Expression Build(Expression arg)
        {
            Contract.Assert(arg != null);

            return null;
        }

        public PropertyInfo TargetProperty
        {
            get { return null; }
        }

        public Expression Rewrite(Expression original, Expression replacement)
        {
            Contract.Assert(original != null);
            Contract.Assert(replacement != null);
            
            return null;
        }

        public IEnumerable<TypeMapKey> ChildMapKeys
        {
            get { yield break; }
        }

        public MappingDirection Direction { get; private set; }
    }
}
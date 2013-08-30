namespace EntityFunctors.Associations
{
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;

    [ContractClass(typeof (ContactForMappingAssociation))]
    public interface IMappingAssociation
    {
        Expression BuildMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression propertyKeys, IMappingRegistry registry);

        string Key { get; }

        MappingDirection Direction { get; }

        LambdaExpression Source { get; }

        LambdaExpression Target { get; }
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

        public string Key
        {
            get { return null; }
        }

        public MappingDirection Direction { get; private set; }

        public LambdaExpression Source
        {
            get { return null; }
        }

        public LambdaExpression Target
        {
            get { return null; }
        }
    }
}
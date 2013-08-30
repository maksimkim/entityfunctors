namespace EntityFunctors.Associations
{
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;

    [ContractClass(typeof (ContactForMappingAssociation))]
    public interface IMappingAssociation
    {
        string Key { get; }

        MappingDirection Direction { get; }

        LambdaExpression Source { get; }

        LambdaExpression Target { get; }
    }

    [ContractClassFor(typeof(IMappingAssociation))]
    public abstract class ContactForMappingAssociation : IMappingAssociation
    {
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
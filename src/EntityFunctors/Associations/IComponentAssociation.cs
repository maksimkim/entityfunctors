namespace EntityFunctors.Associations
{
    public interface IComponentAssociation : IExpandableAssociation
    {
        TypeMapKey ComponentKey { get; }
    }
}
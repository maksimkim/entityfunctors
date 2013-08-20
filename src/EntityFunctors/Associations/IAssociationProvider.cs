namespace EntityFunctors.Associations
{
    using System.Collections.Generic;

    public interface IAssociationProvider
    {
        TypeMapKey Key { get; }

        IEnumerable<IMappingAssociation> Associations { get; }
    }
}
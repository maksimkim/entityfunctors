namespace EntityFunctors.Associations
{
    using System;

    public interface ICollectionAssociation : IExpandableAssociation
    {
        Type SourceItemType { get; }

        Type TargetItemType { get; }
    }
}
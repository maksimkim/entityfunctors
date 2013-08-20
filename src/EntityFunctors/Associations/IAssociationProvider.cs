namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;

    public interface IAssociationProvider
    {
        Type Source { get; }

        Type Target { get; }

        IEnumerable<IMappingAssociation> Associations { get; }
    }
}
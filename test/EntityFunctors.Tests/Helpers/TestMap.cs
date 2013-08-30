namespace EntityFunctors.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using Associations;

    public class TestMap : IAssociationProvider
    {
        public TypeMapKey Key { get; private set; }

        public IEnumerable<IMappingAssociation> Associations { get; private set; }

        public TestMap(Type from, Type to, params IMappingAssociation[] associations)
        {
            Associations = associations;

            Key = new TypeMapKey(from, to);
        }
    }
}
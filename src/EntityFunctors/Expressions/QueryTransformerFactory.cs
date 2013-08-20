namespace EntityFunctors.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Associations;

    public class QueryTransformerFactory : IQueryTransformerFactory
    {
        private readonly IDictionary<TypeMapKey, IEnumerable<IMappingAssociation>> _maps;

        public QueryTransformerFactory(IEnumerable<IAssociationProvider> maps)
        {
            Contract.Assert(maps != null);

            //Support only Expression of DTO -> Expression of Entity rewriting
            _maps = maps.ToDictionary(
                m => new TypeMapKey(m.Target, m.Source),
                m => m.Associations
            );
        }

        public IQueryTransformer<TFrom, TTo> Create<TFrom, TTo>()
        {
            var key = new TypeMapKey(typeof(TFrom), typeof(TTo));

            IEnumerable<IMappingAssociation> assocs;

            if (!_maps.TryGetValue(key, out assocs))
                throw new InvalidOperationException(
                    string.Format("Unable to convert filter of {0} to filter of {1} cause respective mapping from {0} to {1} wasn't found", typeof(TFrom), typeof(TTo))
                );

            return new QueryTransformer<TFrom, TTo>(assocs);
        }
    }
}
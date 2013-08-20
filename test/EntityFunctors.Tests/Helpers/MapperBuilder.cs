namespace EntityFunctors.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using EntityFunctors.Associations;

    public class MapperBuilder
    {
        public Action<TFrom, TTo> BuildMapper<TFrom, TTo>(IMappingAssociation association)
        {
            return BuildMapper<TFrom, TTo>(association, null);
        }

        public Action<TFrom, TTo> BuildMapper<TFrom, TTo>(IMappingAssociation association, IMappingRegistry registry)
        {
            var pFrom = Expression.Parameter(typeof(TFrom), "from");
            var pTo = Expression.Parameter(typeof(TTo), "to");
            var pExpands = Expression.Parameter(typeof(IEnumerable<string>), "expands");

            var mapper = association.BuildMapper(pFrom, pTo, registry, pExpands);

            var expression = Expression.Lambda<Action<TFrom, TTo>>(Expression.Block(mapper), pFrom, pTo);

            return expression.Compile();
        }
    }
}
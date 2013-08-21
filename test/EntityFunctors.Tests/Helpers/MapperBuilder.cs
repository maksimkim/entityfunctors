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
            var pPropertyKeys = Expression.Parameter(typeof(IEnumerable<string>), "propertyKeys");

            var mapper = association.BuildMapper(pFrom, pTo, pPropertyKeys, registry);

            var expression = Expression.Lambda<Action<TFrom, TTo>>(Expression.Block(new[] {pPropertyKeys}, mapper), pFrom, pTo);

            return expression.Compile();
        }
    }
}
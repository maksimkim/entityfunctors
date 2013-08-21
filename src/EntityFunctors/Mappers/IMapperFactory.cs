namespace EntityFunctors.Mappers
{
    using System;
    using System.Collections.Generic;

    public interface IMapperFactory
    {
        Func<TSource, IEnumerable<string>, TTarget> GetCreator<TSource, TTarget>()
            where TTarget : class, new();

        Action<TSource, TTarget, IEnumerable<string>> GetAssigner<TSource, TTarget>();
    }
}
namespace EntityFunctors.Mappers
{
    using System;
    using System.Collections.Generic;

    public interface IMapperFactory
    {
        Action<TSource, TTarget, IEnumerable<string>> GetAssigner<TSource, TTarget>();
    }
}
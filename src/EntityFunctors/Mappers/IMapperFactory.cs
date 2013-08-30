namespace EntityFunctors.Mappers
{
    using System;
    using System.Collections.Generic;

    public interface IMapperFactory
    {
        Func<TSource, IEnumerable<string>, TTarget> GetReader<TSource, TTarget>()
            where TTarget : class, new();

        Action<TSource, TTarget, IEnumerable<string>> GetWriter<TSource, TTarget>()
            where TSource: class, new();
    }
}
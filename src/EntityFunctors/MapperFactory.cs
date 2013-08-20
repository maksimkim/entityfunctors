namespace EntityFunctors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Associations;
    using Common.Logging;
    using Extensions;
    using Mappers;

    public class MapperFactory : IMapperFactory, IMappingRegistry
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        
        private readonly IDictionary<TypeMapKey, IEnumerable<IMappingAssociation>> _maps;

        private readonly ConcurrentDictionary<Tuple<Type, Type>, Delegate> _creatorCache = new ConcurrentDictionary<Tuple<Type, Type>, Delegate>();

        private readonly ConcurrentDictionary<Tuple<Type, Type>, Delegate> _assignerCache = new ConcurrentDictionary<Tuple<Type, Type>, Delegate>();

        public MapperFactory(params IAssociationProvider[] maps)
            : this(maps.AsEnumerable())
        {
            
        }

        public MapperFactory(IEnumerable<IAssociationProvider> maps)
        {
            _maps = maps.ToDictionary(
                   m => m.Key,
                   m => m.Associations
            );
        }

        public Func<TSource, IEnumerable<string>, TTarget> GetCreator<TSource, TTarget>()
            where TTarget: class, new()
        {
            var val = _creatorCache.GetOrAdd(
                Tuple.Create(typeof(TSource), typeof(TTarget)),
                key =>
                {
                    var from = Expression.Parameter(key.Item1, "from");
                    var to = Expression.Variable(key.Item2, "to");
                    var expands = Expression.Parameter(typeof(IEnumerable<string>), "expands");

                    var body = BuildMapperBody(@from, to, expands);

                    if (body == null)
                        throw new NotImplementedException(
                            string.Format("Unable to create mapper from {0} to {1}. Check appropriate mappings exist and loaded.", from.Type, to.Type)
                        );

                    var creation = Expression.Assign(
                        to,
                        ((Expression<Func<TTarget>>)(() => new TTarget())).Body
                    );

                    var mapper = Expression.Lambda<Func<TSource, IEnumerable<string>, TTarget>>(
                        Expression.Block(new[] { to }, creation, body, to), 
                        from,
                        expands
                    );

                    Logger.Debug(m => m("{0} --> {1} mapper built:", from.Type, to.Type));
                    Logger.Debug(m => m("{0}", mapper.Stringify()));

                    return mapper.Compile();
                }
            );

            return (Func<TSource, IEnumerable<string>, TTarget>)val;
        }

        public Action<TSource, TTarget> GetAssigner<TSource, TTarget>()
        {
            var val = _assignerCache.GetOrAdd(
                Tuple.Create(typeof(TSource), typeof(TTarget)), 
                key =>
                {
                    var from = Expression.Parameter(key.Item1, "from");
                    var to = Expression.Parameter(key.Item2, "to");

                    var body = BuildMapperBody(@from, to, null);

                    if (body == null)
                        throw new NotImplementedException(
                            string.Format("Unable to create mapper from {0} to {1}. Check appropriate mappings exist and loaded.", from.Type, to.Type)
                        );

                    var mapper = Expression.Lambda<Action<TSource, TTarget>>(body, from, to);

                    Logger.Debug(m => m("{0} --> {1} mapper built:", from.Type, to.Type));
                    Logger.Debug(m => m("{0}", mapper.Stringify()));

                    return mapper.Compile();
                }
            );

            return (Action<TSource, TTarget>)val;
        }

        public Expression GetMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression expands)
        {
            return BuildMapperBody(@from, to, expands);
        }

        public Expression BuildMapperBody(ParameterExpression @from, ParameterExpression to, ParameterExpression expands)
        {
            var key = new TypeMapKey(@from.Type, to.Type);

            IEnumerable<IMappingAssociation> assocs;

            if (!_maps.TryGetValue(key, out assocs))
                return null;

            return 
                Expression.Block(
                    assocs.Select(a => a.BuildMapper(@from, to, this, expands))
                );
        }
    }
}
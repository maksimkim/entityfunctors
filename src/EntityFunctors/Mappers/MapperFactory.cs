namespace EntityFunctors.Mappers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using EntityFunctors.Associations;
    using Common.Logging;
    using EntityFunctors.Extensions;

    public class MapperFactory : IMapperFactory, IMappingRegistry
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private static readonly MethodInfo ToArray;

        private static readonly MethodInfo Select;
       
        private readonly IDictionary<TypeMapKey, IEnumerable<IMappingAssociation>> _maps;

        private readonly ConcurrentDictionary<Tuple<Type, Type>, Delegate> _readerCache = new ConcurrentDictionary<Tuple<Type, Type>, Delegate>();

        private readonly ConcurrentDictionary<Tuple<Type, Type>, Delegate> _assignerCache = new ConcurrentDictionary<Tuple<Type, Type>, Delegate>();
        
        private readonly IDictionary<TypeMapKey, IEnumerable<IMappingAssociation>> _registry;

        static MapperFactory()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<int>>> exp = xs => xs.Select(x => x).ToArray();

            ToArray = ((MethodCallExpression)exp.Body).Method.GetGenericMethodDefinition();

            Select = ((MethodCallExpression)((MethodCallExpression)exp.Body).Arguments[0]).Method.GetGenericMethodDefinition();
        }

        public MapperFactory(params IAssociationProvider[] maps)
            : this(maps.AsEnumerable())
        {
            
        }

        public MapperFactory(IEnumerable<IAssociationProvider> providers)
        {
            _maps = providers.ToDictionary(
                   m => m.Key,
                   m => m.Associations
            );

            _registry = providers.ToDictionary(p => p.Key, p => p.Associations);
        }


        public Func<TSource, IEnumerable<string>, TTarget> GetReader<TSource, TTarget>()
            where TTarget : class, new()
        {
            var val = _readerCache.GetOrAdd(
                Tuple.Create(typeof(TSource), typeof(TTarget)),
                key =>
                {
                    var from = Expression.Parameter(key.Item1, "from");
                    var to = Expression.Variable(key.Item2, "to");
                    var expands = Expression.Parameter(typeof(IEnumerable<string>), "expands");

                    var body = BuildReaderBody(from, to, expands);

                    body.Add(to);

                    var mapper = Expression.Lambda(Expression.Block(new[] { to }, body), @from, expands);
                    //var mapper = Expression.Lambda(body, @from, expands);

                    Logger.Debug(m => m("{0} --> {1} mapper built:", @from.Type, to.Type));
                    Logger.Debug(m => m("{0}", mapper.Stringify()));

                    return mapper.Compile();
                }
            );

            return (Func<TSource, IEnumerable<string>, TTarget>)val;
        }

        private IList<Expression> BuildReaderBody(ParameterExpression from, ParameterExpression to, ParameterExpression propertyKeys)
        {
            var mapKey = new TypeMapKey(from.Type, to.Type);

            IEnumerable<IMappingAssociation> associations;

            if (!_registry.TryGetValue(mapKey, out associations))
                throw new NotImplementedException(
                    string.Format("Unable to create mapper from {0} to {1}. Check appropriate mappings exist and loaded.", from.Type, to.Type)
                );

            var body = new List<Expression>();
            //var bindings = new List<MemberBinding>();

            //create target object (only direct mapping: entity -> dto)
            var ctor = to.Type.GetConstructor(Type.EmptyTypes);

            if (ctor == null)
                throw new InvalidOperationException(
                    string.Format("Type {0} must declare public parameterless constructor to be mapped from {1}", to.Type, from.Type)
                );

            body.Add(Expression.Assign(to, Expression.New(ctor)));

            foreach (var association in associations)
            {
                var item = BuildReaderItem(association, from, to, propertyKeys);
                
                if (item != null)
                {
                    body.Add(item);

                    //bindings.Add(Expression.Bind(
                    //    association.Target.GetProperty(),
                    //    item
                    //));
                }
            }

            return body;

            //return Expression.MemberInit(Expression.New(ctor), bindings);
        }

        private Expression BuildReaderItem(IMappingAssociation association, ParameterExpression from, ParameterExpression to, ParameterExpression propertyKeys)
        {
            if ((association.Direction & MappingDirection.Read) != MappingDirection.Read)
                return null;

            var converter = association as IConvertionAssociation;
            var collection = association as ICollectionAssociation;
            var component = association as IComponentAssociation;
            var expandable = association as IExpandableAssociation;

            Expression result;

            var acceptor = association.Target.Apply(to);
            var donor = association.Source.Apply(from);

            ParameterExpression componentFrom;
            ParameterExpression componentTo;

            if (collection != null)
            {
                LambdaExpression selector = null;
                
                if (component != null)
                {
                    componentFrom = Expression.Parameter(collection.SourceItemType);
                    componentTo = Expression.Variable(collection.TargetItemType);

                    //todo: probably should filterout expand somehow. E.g. 'foo.id' should become 'id'. Now simply not passing outer expand
                    var componentMapper = BuildReaderBody(componentFrom, componentTo, null);
                    componentMapper.Add(componentTo);

                    selector = Expression.Lambda(
                        Expression.Block(new[] { componentTo }, componentMapper),
                        componentFrom
                    );
                }

                if (converter != null)
                {
                    selector = converter.TargetConverter.Expression;
                }

                if (selector == null)
                    throw new InvalidOperationException();

                result = Expression.Assign(
                    acceptor,
                    Expression.Condition(
                        donor.CreateCheckForDefault(),
                        acceptor.Type.GetDefaultExpression(),
                        Expression.Convert(
                            Expression.Call(
                                ToArray.MakeGenericMethod(collection.TargetItemType),
                                Expression.Call(
                                    Select.MakeGenericMethod(collection.SourceItemType, collection.TargetItemType),
                                    donor,
                                    selector
                                )
                            ),
                            acceptor.Type
                        )
                    )
                );
            }
            else
            {
                if (component != null)
                {
                    componentFrom = Expression.Variable(association.Source.ReturnType);
                    componentTo = Expression.Variable(association.Target.ReturnType);

                    //todo: probably should filterout expand somehow. E.g. 'foo.id' should become 'id'. Now simply not passing outer expand
                    var componentMapper = BuildReaderBody(componentFrom, componentTo, null);
                    componentMapper.Insert(0, Expression.Assign(componentFrom, donor));
                    componentMapper.Add(Expression.Assign(acceptor, componentTo));

                    result = Expression.IfThenElse(
                        donor.CreateCheckForDefault(),
                        Expression.Assign(acceptor, acceptor.Type.GetDefaultExpression()),
                        Expression.Block(new[] { componentFrom, componentTo }, componentMapper)
                    );
                }
                else
                {
                    if (converter != null)
                    {
                        var conversion = converter.SourceConverter.Expression.Apply(donor);

                        donor =
                            donor.Type.IsValueType
                            ? conversion
                            : Expression.Condition(donor.CreateCheckForDefault(), acceptor.Type.GetDefaultExpression(), conversion);
                    }

                    result = Expression.Assign(acceptor, donor);
                }
            }

            //result =
            //    donor.Type.IsValueType
            //    ? result
            //    : Expression.Condition(
            //        donor.CreateCheckForDefault(), 
            //        acceptor.Type.GetDefaultExpression(), 
            //        result
            //    );

            if (result != null && propertyKeys != null && expandable != null && expandable.Expand)
            {
                result = Expression.IfThen(
                    propertyKeys.CreateContains(Expression.Constant(association.Key, typeof(string))),
                    result
                );

                //result = Expression.Condition(
                //    propertyKeys.CreateContains(Expression.Constant(association.Key, typeof(string))),
                //    result,
                //    acceptor.Type.GetDefaultExpression()
                //);
            }
                

            return result;
        }

        public Action<TSource, TTarget, IEnumerable<string>> GetAssigner<TSource, TTarget>()
        {
            var val = _assignerCache.GetOrAdd(
                Tuple.Create(typeof(TSource), typeof(TTarget)), 
                key =>
                {
                    var from = Expression.Parameter(key.Item1, "from");
                    var to = Expression.Parameter(key.Item2, "to");
                    var explicitProperties = Expression.Parameter(typeof(IEnumerable<string>), "explicitProperties");

                    var body = BuildMapperBody(@from, to, explicitProperties);

                    if (body == null)
                        throw new NotImplementedException(
                            string.Format("Unable to create mapper from {0} to {1}. Check appropriate mappings exist and loaded.", from.Type, to.Type)
                        );

                    var mapper = Expression.Lambda<Action<TSource, TTarget, IEnumerable<string>>>(body, from, to, explicitProperties);

                    Logger.Debug(m => m("{0} --> {1} mapper built:", from.Type, to.Type));
                    Logger.Debug(m => m("{0}", mapper.Stringify()));

                    return mapper.Compile();
                }
            );

            return (Action<TSource, TTarget, IEnumerable<string>>)val;
        }

        //public Action<TSource, TTarget, IEnumerable<string>> GetAssigner2<TSource, TTarget>()
        //{
        //    var key = new TypeMapKey(typeof(TSource), typeof(TTarget));

        //    Dictionary<PropertyInfo, IMappingAssociation> associations;

        //    if (!_associations.TryGetValue(key, out associations))
        //        throw new NotImplementedException(
        //            string.Format("Unable to create mapper from {0} to {1}. Check appropriate mappings exist and loaded.", typeof(TSource), typeof(TTarget))
        //            );

        //    //creator assumption
        //    const MappingDirection direction = MappingDirection.Write;

        //    var from = Expression.Parameter(typeof(TSource), "from");
        //    var to = Expression.Parameter(typeof(TTarget), "to");
        //    var explicitProperties = Expression.Parameter(typeof(IEnumerable<string>), "explicitProperties");

        //    var body = new List<Expression>();

        //    foreach (var association in associations.Values)
        //    {
        //        if ((association.Direction & direction) != direction)
        //            continue;

        //        var exp = Expression.Assign(association.Build(to), association.Build(@from));
        //        body.Add(exp);
        //    }

        //    body.Add(to);

        //    var mapper = Expression.Lambda<Action<TSource, TTarget, IEnumerable<string>>>(Expression.Block(body), from, to, explicitProperties);

        //    Logger.Debug(m => m("{0} --> {1} mapper built:", from.Type, to.Type));
        //    Logger.Debug(m => m("{0}", mapper.Stringify()));

        //    return mapper.Compile();
        //}

        public Expression GetMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression expands)
        {
            return BuildMapperBody(@from, to, expands);
        }

        public Expression BuildMapperBody(ParameterExpression @from, ParameterExpression to, ParameterExpression propertyKeys)
        {
            var key = new TypeMapKey(@from.Type, to.Type);

            IEnumerable<IMappingAssociation> assocs;

            if (!_maps.TryGetValue(key, out assocs))
                return null;

            var statements =
                assocs
                .Select(a => a.BuildMapper(@from, to, propertyKeys, this))
                .Where(e => !(e.NodeType == ExpressionType.Default && e.Type == typeof(void)))
                .ToArray();

            return 
                statements.Length == 0 
                ? (Expression)Expression.Empty()
                : Expression.Block(statements);
        }
    }
}
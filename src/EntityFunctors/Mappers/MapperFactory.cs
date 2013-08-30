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

    public class MapperFactory : IMapperFactory
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private static readonly MethodInfo ToArray;

        private static readonly MethodInfo Select;

        private readonly ConcurrentDictionary<Tuple<Type, Type>, Delegate> _readerCache = new ConcurrentDictionary<Tuple<Type, Type>, Delegate>();

        private readonly ConcurrentDictionary<Tuple<Type, Type>, Delegate> _writerCache = new ConcurrentDictionary<Tuple<Type, Type>, Delegate>();
        
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
            providers.ToDictionary(
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
                    var expands = Expression.Parameter(typeof(IEnumerable<string>), "expands");

                    var body = BuildReaderBody(@from, key.Item2, expands);

                    var mapper = Expression.Lambda(body, @from, expands);

                    Logger.Debug(m => m("{0} --> {1} mapper built:", key.Item1, key.Item2));
                    Logger.Debug(m => m("{0}", mapper.Stringify()));

                    return mapper.Compile();
                }
            );

            return (Func<TSource, IEnumerable<string>, TTarget>)val;
        }

        private Expression BuildReaderBody(Expression @from, Type targetType, ParameterExpression expands)
        {
            var mapKey = new TypeMapKey(from.Type, targetType);

            IEnumerable<IMappingAssociation> associations;

            if (!_registry.TryGetValue(mapKey, out associations))
                throw new NotImplementedException(
                    string.Format("Unable to create mapper from {0} to {1}. Check appropriate mappings exist and loaded.", from.Type, targetType)
                );

            var bindings = new List<MemberBinding>();

            var ctor = targetType.GetConstructor(Type.EmptyTypes);

            if (ctor == null)
                throw new InvalidOperationException(
                    string.Format("Type {0} must declare public parameterless constructor to be mapped from {1}", targetType, from.Type)
                );

            foreach (var association in associations)
            {
                var item = BuildReaderItem(association, from, expands);
                
                if (item != null)
                    bindings.Add(Expression.Bind(association.Target.GetProperty(), item));
            }

            return Expression.MemberInit(Expression.New(ctor), bindings);
        }

        private Expression BuildReaderItem(IMappingAssociation association, Expression from, ParameterExpression expands)
        {
            if ((association.Direction & MappingDirection.Read) != MappingDirection.Read)
                return null;

            var converter = association as IConvertionAssociation;
            var collection = association as ICollectionAssociation;
            var component = association as IComponentAssociation;
            var expandable = association as IExpandableAssociation;

            Expression result;

            var donor = association.Source.Apply(from);

            var targetType = association.Target.ReturnType;

            if (collection != null)
            {
                LambdaExpression selector;
                
                //selector = new component { ... }
                if (component != null)
                {
                    var param = Expression.Parameter(collection.SourceItemType);

                    //todo: probably should filterout expand somehow. E.g. 'foo.id' should become 'id'. Now simply not passing outer expand
                    var body = BuildReaderBody(param, collection.TargetItemType, null);

                    selector = Expression.Lambda(body, param);
                }
                //selector = converter
                else if (converter != null)
                {
                    selector = converter.TargetConverter.Expression;
                }
                else
                    throw new InvalidOperationException();

                // from.prop.select(selector).toarray().asenumerable()
                result = 
                    Expression.Convert(
                        Expression.Call(
                            ToArray.MakeGenericMethod(collection.TargetItemType),
                            Expression.Call(
                                Select.MakeGenericMethod(collection.SourceItemType, collection.TargetItemType),
                                donor,
                                selector
                            )
                        ),
                        targetType
                    );
            }
            else
            {
                // new component { .... }
                if (component != null)
                {
                    //todo: probably should filterout expand somehow. E.g. 'foo.id' should become 'id'. Now simply not passing outer expand
                    result = BuildReaderBody(donor, targetType, null);
                }
                //-or- converter(from.prop)
                else if (converter != null)
                {
                    result = converter.SourceConverter.Expression.Apply(donor);
                }
                // from.prop
                else
                {
                    result = donor;
                }
            }

            // expands.contains({key}) ? {exp} : null
            if (result != null && expands != null && expandable != null && expandable.Expand)
            {
                result = Expression.Condition(
                    expands.CreateContains(Expression.Constant(association.Key, typeof(string))),
                    result,
                    targetType.GetDefaultExpression()
                );
            }

            // from.prop != null ? null : {exp}
            if (!(donor.Type.IsValueType || result == donor))
            {
                result = Expression.Condition(
                    donor.CreateCheckForDefault(),
                    targetType.GetDefaultExpression(),
                    result
                );
            }

            return result;
        }

        public Action<TSource, TTarget, IEnumerable<string>> GetWriter<TSource, TTarget>()
            where TSource : class, new()
        {
            var val = _writerCache.GetOrAdd(
                Tuple.Create(typeof(TSource), typeof(TTarget)),
                key =>
                {
                    var from = Expression.Parameter(key.Item1, "from");
                    var to = Expression.Parameter(key.Item2, "to");
                    var explicitProperties = Expression.Parameter(typeof(IEnumerable<string>), "explicitProperties");

                    var body = BuildWriterBody(@from, to, explicitProperties);

                    var mapper = Expression.Lambda(body, from, to, explicitProperties);

                    Logger.Debug(m => m("{0} --> {1} mapper built:", from.Type, to.Type));
                    Logger.Debug(m => m("{0}", mapper.Stringify()));

                    return mapper.Compile();
                }
            );

            return (Action<TSource, TTarget, IEnumerable<string>>)val;
        }

        private Expression BuildWriterBody(Expression from, Expression to, ParameterExpression explicitProperties)
        {
            var mapKey = new TypeMapKey(@from.Type, to.Type);

            IEnumerable<IMappingAssociation> associations;

            if (!_registry.TryGetValue(mapKey, out associations))
                throw new NotImplementedException(
                    string.Format("Unable to create mapper from {0} to {1}. Check appropriate mappings exist and loaded.", @from.Type, to.Type)
                    );

            var body = new List<Expression>();

            foreach (var association in associations)
            {
                var item = BuildWriterItem(association, @from, to, explicitProperties);

                if (item != null)
                    body.Add(item);
            }

            return 
                body.Count > 0 
                ? (Expression)Expression.Block(body) 
                : Expression.Empty();
        }

        private Expression BuildWriterItem(IMappingAssociation association, Expression from, Expression to, ParameterExpression explicitProperties)
        {
            if ((association.Direction & MappingDirection.Write) != MappingDirection.Write)
                return null;

            var converter = association as IConvertionAssociation;
            var component = association as IComponentAssociation;

            var donor = association.Target.Apply(@from);
            var acceptor = association.Source.Apply(to);
            
            Expression result;

            if (component != null)
            {
                //todo: probably should filterout expand somehow. E.g. 'foo.id' should become 'id'. Now simply not passing outer expand
                result = BuildWriterBody(donor, acceptor, null);

                if (result.NodeType == ExpressionType.Default && result.Type == typeof(void))
                    return null;
            }
            else
            {
                result = Expression.Assign(
                    acceptor, 
                    converter == null ? donor : converter.TargetConverter.Expression.Apply(donor)
                );
            }

            if (!(donor.Type.IsValueType || (result.NodeType == ExpressionType.Assign && ((BinaryExpression)result).Right == donor)))
                result = Expression.IfThenElse(
                    donor.CreateCheckForDefault(),
                    Expression.Assign(acceptor, acceptor.Type.GetDefaultExpression()),
                    result
                );

            if (explicitProperties != null)
                result = Expression.IfThen(
                    Expression.OrElse(
                        explicitProperties.CreateCheckForDefault(),
                        explicitProperties.CreateContains(Expression.Constant(association.Key, typeof(string)))
                    ),
                    result
                );

            return result;
        }
    }
}
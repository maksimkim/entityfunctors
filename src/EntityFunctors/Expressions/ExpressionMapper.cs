namespace EntityFunctors.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Associations;
    using Extensions;

    public class ExpressionMapper : IExpressionMapper
    {
        private readonly IDictionary<TypeMapKey, Dictionary<PropertyInfo, IMappingAssociation>> _associations;

        public ExpressionMapper(IEnumerable<IAssociationProvider> providers)
        {
            _associations = providers.ToDictionary(
                p => p.Key, 
                p => p.Associations.ToDictionary(
                    a => a.TargetProperty, 
                    a => a
                    )
                );
        }

        public ExpressionMapper(params IAssociationProvider[] providers)
            : this(providers.AsEnumerable())
        {
            
        }
        
        public Expression<Func<TTarget, TTargetResult>> Map<TSource, TSourceResult, TTarget, TTargetResult>(Expression<Func<TSource, TSourceResult>> expression)
        {
            return (Expression<Func<TTarget, TTargetResult>>)Map(expression, typeof(TSource), typeof(TTarget));
        }

        public Expression<Func<TTarget, TResult>> Map<TSource, TTarget, TResult>(Expression<Func<TSource, TResult>> expression)
        {
            return Map<TSource, TResult, TTarget, TResult>(expression);
        }

        public LambdaExpression Map(LambdaExpression expression, Type sourceType, Type targetType)
        {
            return (LambdaExpression) Map((Expression)expression, sourceType, targetType);
        }

        public Expression Map(Expression expression, Type sourceType, Type targetType)
        {
            Dictionary<PropertyInfo, IMappingAssociation> assocs;

            if (!_associations.TryGetValue(new TypeMapKey(sourceType, targetType), out assocs))
                throw new InvalidOperationException(string.Format("Mapping from type {0} to type {1} is not configured", sourceType, targetType));

            var ctx = new MapContext
            {
                From = sourceType,
                To = targetType,
                Associations = assocs
            };

            var visitor = new MappingVisitor(ctx, this);

            var morphed = visitor.Visit(expression);

            return morphed;
        }

        private class MappingVisitor : ExpressionVisitor
        {
            private MapContext _ctx;

            private readonly ExpressionMapper _mapper;

            public MappingVisitor(MapContext ctx, ExpressionMapper mapper)
            {
                _ctx = ctx;
                _mapper = mapper;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                var body = Visit(node.Body);

                var parameters = VisitAndConvert(node.Parameters, "VisitLambda");

                if (body.Type == node.Body.Type && parameters.Select(p => p.Type).SequenceEqual(node.Parameters.Select(p => p.Type)))
                    return node.Update(body, parameters);

                return Expression.Lambda(body, parameters);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node.Type == _ctx.From ? _ctx.Parameter : base.VisitParameter(node);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var instance = base.Visit(node.Expression);

                PropertyInfo prop;

                IMappingAssociation assoc;
                
                if (!(node.TryGetProperty(out prop) && _ctx.Associations.TryGetValue(prop, out assoc)))
                    return node.Update(instance);

                if (assoc.ChildMapKeys.Any())
                    foreach (var pair in assoc
                        .ChildMapKeys
                        .Where(key => _mapper._associations.ContainsKey(key))
                        .SelectMany(key => _mapper._associations[key])
                        .Where(p => !_ctx.Associations.ContainsKey(p.Key))
                        )
                        _ctx.Associations.Add(pair.Key, pair.Value);

                return assoc.Build(instance);
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                var left = Visit(node.Left);
                var conversion = VisitAndConvert(node.Conversion, "VisitBinary");
                var right = Visit(node.Right);

                bool leftIsConstant;
                bool rightIsConstant;

                if (
                    left.Type == right.Type
                        ||
                        ((leftIsConstant = (left.NodeType == ExpressionType.Constant)) && right.Type.IsAssignableFrom(left.Type))
                        ||
                        ((rightIsConstant = (right.NodeType == ExpressionType.Constant)) && left.Type.IsAssignableFrom(right.Type))
                    )
                    return node.Update(left, conversion, right);

                PropertyInfo prop;
                IMappingAssociation assoc;

                if (!(
                    (
                        leftIsConstant && node.Right.TryGetProperty(out prop)
                            ||
                            rightIsConstant && node.Left.TryGetProperty(out prop)
                        )
                        &&
                        _ctx.Associations.TryGetValue(prop, out assoc)
                    ))
                    throw new InvalidOperationException();

                if (leftIsConstant)
                    left = assoc.Build(left);

                if (rightIsConstant)
                    right = assoc.Build(right);

                return Expression.MakeBinary(node.NodeType, left, right);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var instance = Visit(node.Object);

                PropertyInfo prop;
                IMappingAssociation assoc = null;
                TypeMapKey childMapKey = null;

                if (node.Arguments.Any(a => 
                    a.TryGetProperty(out prop) 
                        && _ctx.Associations.TryGetValue(prop, out assoc)
                        && assoc.ChildMapKeys.Any()
                    ))
                    childMapKey = assoc.ChildMapKeys.First();

                var args = node.Arguments.Select(a =>
                {
                    var lambda = a as LambdaExpression;

                    Type paramType;

                    if (lambda != null
                        && childMapKey != null
                        && lambda.Parameters.Count == 1
                        && ((paramType = lambda.Parameters[0].Type) != null)
                        && (paramType == childMapKey.Low || paramType == childMapKey.High)
                        )
                    {
                        var ctx = new MapContext
                        {
                            From = paramType,
                            To = paramType == childMapKey.Low ? childMapKey.High : childMapKey.Low,
                            Associations = _mapper._associations[childMapKey]
                        };

                        var preserved = _ctx;
                        _ctx = ctx;
                        var result = Visit(a);
                        _ctx = preserved;

                        return result;
                    }
                    
                    return Visit(a);
                }).ToArray();

                if (instance == node.Object && args.SequenceEqual(node.Arguments))
                    return node;

                var method = node.Method;
                PropertyInfo oldProp;
                PropertyInfo newProp;

                if (method.IsGenericMethod
                    && method.IsStatic
                    && method.DeclaringType == typeof(Enumerable)
                    && node.Arguments[0].TryGetProperty(out oldProp)
                    && args[0].TryGetProperty(out newProp)
                    && oldProp.PropertyType != newProp.PropertyType
                    )
                {
                    var baseMethod = method.GetGenericMethodDefinition();

                    method = baseMethod.MakeGenericMethod(newProp.PropertyType.GetItemType());

                    return Expression.Call(null, method, args);
                }

                return Expression.Call(instance, node.Method, args);
            }
        }

        private class MapContext
        {
            private ParameterExpression _parameter;

            public Type From { get; set; }

            public Type To { get; set; }

            public IDictionary<PropertyInfo, IMappingAssociation> Associations { get; set; }

            public ParameterExpression Parameter
            {
                get { return _parameter ?? (_parameter = Expression.Parameter(To, "_")); }
            }
        }
    }
}
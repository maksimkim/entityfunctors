namespace EntityFunctors.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Associations;
    using Extensions;
    using Remotion.Linq.Parsing;

    public class ExpressionTransformer<TFrom, TTo> : ExpressionTreeVisitor, IExpressionTransformer<TFrom, TTo>
    {
        private static readonly IDictionary<Type, Func<Expression, TransformationContext, Expression>> Transforms =
            new Dictionary<Type, Func<Expression, TransformationContext, Expression>>
            {
                {
                    typeof(ParameterExpression), 
                    (_, ctx) => Parameter((ParameterExpression) _, ctx)
                },
                {
                    typeof(MemberExpression), 
                    (_, ctx) => Property((MemberExpression) _, ctx)
                },
                {
                    typeof(BinaryExpression),
                    (_, ctx) => Binary((BinaryExpression)_, ctx)
                },
                {
                    typeof(MethodCallExpression),
                    (_, ctx) => Call((MethodCallExpression)_, ctx)
                },
                {
                    typeof(ConstantExpression),
                    (_, ctx) => Constant((ConstantExpression)_, ctx)
                },
                {
                    typeof(LambdaExpression),
                    (_, ctx) => Lambda((LambdaExpression)_, ctx)
                }
                
            };

        private TransformationContext _ctx;

        private readonly IDictionary<PropertyInfo, Delegate> _converters 
            = new Dictionary<PropertyInfo, Delegate>();

        private readonly IDictionary<PropertyInfo, Func<Expression, ParameterExpression, Expression>> _rewriters 
            = new Dictionary<PropertyInfo, Func<Expression, ParameterExpression, Expression>>();

        public ExpressionTransformer(IEnumerable<IMappingAssociation> maps)
        {
            Contract.Assert(maps != null);

            foreach (var pair in maps.SelectMany(m => m.ValueConverters))
                _converters.Add(pair.Key, pair.Value);

            foreach (var pair in maps)
                _rewriters.Add(pair.TargetProperty, pair.Rewrite);
        }

        public Expression<Func<TTo, TResult>> Transform<TResult>(Expression<Func<TFrom, TResult>> source)
        {
            return Transform<TResult, TResult>(source);
        }

        public Expression<Func<TTo, TToResult>> Transform<TFromResult, TToResult>(Expression<Func<TFrom, TFromResult>> source)
        {
            return (Expression<Func<TTo, TToResult>>)Transform((LambdaExpression)source);
        }

        public LambdaExpression Transform(LambdaExpression source)
        {
            Contract.Assert(source != null);
            
            _ctx = new TransformationContext
            {
                Parameter = Expression.Parameter(typeof(TTo), source.Parameters[0].Name),

                Transformers = _rewriters,

                Converters = _converters
            };

            var rewriten = (LambdaExpression)VisitExpression(source);

            return rewriten;
        }

        public Expression Transform(Expression source)
        {
            Contract.Assert(source != null);

            var lambda = source as LambdaExpression;

            Contract.Assert(source != null);

            return Transform(lambda);
        }

        public override Expression VisitExpression(Expression expression)
        {
            Contract.Assert(_ctx != null);

            _ctx.Navigator.Down(expression);

            var visited = base.VisitExpression(expression);

            if (visited == null)
            {
                _ctx.Navigator.Up();

                return null;
            }
                
            Func<Expression, TransformationContext, Expression> transformation = null;
             
            if (expression.GetExpressionTypes().Any(t => Transforms.TryGetValue(t, out transformation)))
            {
                var transformed = transformation(visited, _ctx);

                Contract.Assert(transformed != null);

                if (transformed != visited)
                {
                    _ctx.Navigator.EnterRevisit();

                    var revisited = VisitExpression(transformed);

                    _ctx.Navigator.ExitRevisit();

                    _ctx.Navigator.Up();

                    return revisited;
                }
            }
            _ctx.Navigator.Up();

            return visited;
        }

        protected override Expression VisitLambdaExpression(LambdaExpression expression)
        {
            Contract.Assert(expression != null);

            var newParameters = VisitAndConvert(expression.Parameters, "VisitLambdaExpression");

            var newBody = VisitExpression(expression.Body);

            if ((newBody != expression.Body) || (newParameters != expression.Parameters))
                //M.K.: changed only following instuction, cause original ExpressionTreeVisitor class passes old delegateType which turns parameter validation to fail
                return Expression.Lambda(newBody, newParameters);

            return expression;
        }

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            Contract.Assert(expression != null);

            var newLeft = VisitExpression(expression.Left);

            var newRight = VisitExpression(expression.Right);

            var newConversion = (LambdaExpression)VisitExpression(expression.Conversion);

            if (newLeft != expression.Left || newRight != expression.Right || newConversion != expression.Conversion)
                return Expression.MakeBinary(expression.NodeType, newLeft, newRight);

            return expression;
        }

        private static Expression Parameter(ParameterExpression node, TransformationContext ctx)
        {
            if (node.Type == typeof(TFrom))
            {
                if (!ctx.ParameterReplaced)
                {
                    ctx.ParameterReplaced = true;
                    return ctx.Parameter;
                }
            }

            if (!ctx.Navigator.Revisit)
            {
                //todo: modify for components
                if (node.Type == typeof(TFrom))
                {
                    ctx.Replacements.Add(node, Tuple.Create<Expression, PathNode>(ctx.Parameter, ctx.Navigator.Current));
                    ctx.Navigator.SetReplacement(ctx.Parameter);
                }

                return node;
            }

            return ReplaceIfExists(node, ctx);
        }

        private static Expression Lambda(LambdaExpression node, TransformationContext ctx)
        {
            if (!ctx.Navigator.Revisit)
            {
                if (ctx.Replacements.Any(r => r.Value.Item2.IsCurrentOrChild(ctx.Navigator.Current)))
                {
                    return Expression.Lambda(node.Body, node.Parameters);
                }
            }

            return node;
        }

        private static Expression Property(MemberExpression node, TransformationContext ctx)
        {
            var property = node.Member as PropertyInfo;

            if (property == null)
                return node;

            if (!ctx.Navigator.Revisit)
            {
                if (ctx.Replacements.Any(r => r.Value.Item2.IsCurrentOrChild(ctx.Navigator.Current)))
                {
                    Func<Expression, ParameterExpression, Expression> transformer;
                    Tuple<Expression, PathNode> replacement;

                    if (ctx.Replacements.TryGetValue(node.Expression, out replacement) && ctx.Transformers.TryGetValue(property, out transformer))
                    {
                        ctx.Replacements.Add(node, Tuple.Create(transformer(node, (ParameterExpression) replacement.Item1), ctx.Navigator.Current));
                        ctx.Replacements.Remove(node.Expression);

                        ctx.Navigator.SetReplacement(transformer(node, (ParameterExpression) replacement.Item1));
                    }
                }

                return node;
            }

            return ReplaceIfExists(node, ctx);
        }

        private static Expression Binary(BinaryExpression node, TransformationContext ctx)
        {
            if (!ctx.Navigator.Revisit)
            {
                //we come to logical binary
                if (node.Type == typeof(bool))
                {
                    //let's see if we met something we should modify
                    if (ctx.Replacements.Any(r => r.Value.Item2.IsCurrentOrChild(ctx.Navigator.Current)))
                    {
                        MemberExpression property;
                        ConstantExpression constant;

                        if ((((property = node.Left as MemberExpression) != null) && ((constant = node.Right as ConstantExpression) != null))
                            ||
                            (((property = node.Right as MemberExpression) != null) && ((constant = node.Left as ConstantExpression) != null)))
                        {
                            PropertyInfo pi;

                            if (property.TryGetProperty(out pi))
                            {
                                Delegate converter;

                                if (ctx.Converters.TryGetValue(pi, out converter))
                                {
                                    ctx.Replacements.Add(
                                        constant,
                                        Tuple.Create<Expression, PathNode>(
                                            Expression.Constant(
                                                converter.DynamicInvoke(constant.Value),
                                                converter.Method.ReturnType
                                            ),
                                            ctx.Navigator.Current
                                        )
                                     );
                                }
                            }
                        }

                        return Expression.MakeBinary(node.NodeType, node.Left, node.Right);
                    }
                }
            }

            return node;
        }

        private static Expression Call(MethodCallExpression node, TransformationContext ctx)
        {
            if (!ctx.Navigator.Revisit)
            {
                var method = node.Method;
                
                //if we come to bool method
                if (node.Method.ReturnType == typeof(bool))
                {
                    //let's see if we met something we should modify
                    if (ctx.Replacements.Any(r => r.Value.Item2.IsCurrentOrChild(ctx.Navigator.Current)))
                    {
                        Expression oldArg;
                        PropertyInfo propOld;
                        PropertyInfo propNew;
                        Tuple<Expression, PathNode> replacement;
                        
                        if (method.IsGenericMethod
                            && method.IsStatic
                            && method.DeclaringType == typeof(Enumerable)
                            && node.Arguments.Count == 1
                            && (oldArg = node.Arguments[0]).TryGetProperty(out propOld)
                            && ctx.Replacements.TryGetValue(oldArg, out replacement)
                            && replacement.Item1.TryGetProperty(out propNew)
                            && propOld.PropertyType != propNew.PropertyType
                        )
                        {
                            var baseMethod = method.GetGenericMethodDefinition();

                            method  = baseMethod.MakeGenericMethod(propNew.PropertyType.GetItemType());

                            ctx.Replacements.Remove(oldArg);
                            
                            return Expression.Call(null, method, replacement.Item1);
                        }
                            

                        //let's see if there's something 
                        return Expression.Call(node.Object, method, node.Arguments);
                    }
                }
            }

            return node;
        }

        private static Expression Constant(ConstantExpression node, TransformationContext ctx)
        {
            if (ctx.Navigator.Revisit)
            {
                return ReplaceIfExists(node, ctx);
            }

            return node;
        }

        private static Expression ReplaceIfExists(Expression node, TransformationContext ctx)
        {
            if (ctx.Replacements.ContainsKey(node))
            {
                var val = ctx.Replacements[node].Item1;
                ctx.Replacements.Remove(node);
                return val;
            }

            return node;
        }

    }
}
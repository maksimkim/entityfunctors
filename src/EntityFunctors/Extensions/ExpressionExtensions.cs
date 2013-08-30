namespace EntityFunctors.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Expressions;

    public static class ExpressionExtensions
    {
        private static readonly Func<LambdaExpression, string> ExpressionStringifier;

        private static readonly MethodInfo StringIsNullOrWhiteSpace = typeof(string).GetMethod("IsNullOrWhiteSpace", BindingFlags.Static | BindingFlags.Public);

        private static readonly MethodInfo ContainsBase;// = typeof(Enumerable).GetMethod("Contains",  )
        
        static ExpressionExtensions()
        {
            var proxyType = typeof(Expression).GetNestedType("LambdaExpressionProxy", BindingFlags.NonPublic);
            var debugViewProperty = proxyType.GetProperty("DebugView", BindingFlags.Instance | BindingFlags.Public);
            var ctor = proxyType.GetConstructor(new[] { typeof(LambdaExpression) });
            
            var ctorParam = Expression.Parameter(typeof(LambdaExpression), "node");

            ExpressionStringifier =
                Expression.Lambda<Func<LambdaExpression, string>>(
                    Expression.Property(
                        Expression.New(ctor, ctorParam),
                        debugViewProperty
                    ),
                    ctorParam
                ).Compile();

            Expression<Func<bool>> exContains = () => Enumerable.Empty<bool>().Contains(false);
            ContainsBase = (exContains.Body as MethodCallExpression).Method.GetGenericMethodDefinition();
        }

        public static Expression Apply(this LambdaExpression lamba, Expression parameter)
        {
            return new ParameterReplacer().Replace(lamba, parameter);
        }
        
        public static PropertyInfo GetProperty(this LambdaExpression expression)
        {
            Contract.Assert(expression != null);

            var member = expression.Body as MemberExpression;

            Contract.Assert(member != null, string.Format("Expected PropertyExpression but found {0}", member.GetType()));

            var property = member.Member as PropertyInfo;

            Contract.Assert(property != null, string.Format("Expected PropertyExpression but found {0}", member.GetType()));

            return property;
        }

        public static bool TryGetProperty(this Expression expression, out PropertyInfo result)
        {
            Contract.Assert(expression != null);

            result = null;

            var member = expression as MemberExpression;

            if (member != null)
            {
                result = member.Member as PropertyInfo;

                return result != null;
            }

            return false;
        }

        public static string Stringify(this LambdaExpression expression)
        {
            Contract.Assert(expression != null);
            
            return ExpressionStringifier(expression);
        }

        public static Expression CreateCheckForDefault(this Expression arg)
        {
            if (arg.Type == typeof(string))
                return Expression.Call(StringIsNullOrWhiteSpace, arg);

            return Expression.Equal(arg, arg.Type.GetDefaultExpression());
        }

        public static Expression CreateContains(this Expression enumerable, Expression arg)
        {
            Contract.Assert(enumerable != null);
            Contract.Assert(arg != null);

            //todo: compatibility checks

            return 
                Expression.AndAlso(
                    Expression.NotEqual(enumerable, Expression.Constant(null)),
                    Expression.Call(ContainsBase.MakeGenericMethod(arg.Type), enumerable, arg)
                );
        }

        public static IEnumerable<Type> GetExpressionTypes<T>(this T expression)
            where T : Expression
        {
            var current = expression.GetType();
            while (current != typeof(Expression))
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public class ParameterReplacer : ExpressionVisitor
        {
            private ParameterExpression _from;

            private Expression _to;

            public Expression Replace(LambdaExpression lamba, Expression replacement)
            {
                Contract.Assert(lamba != null);
                Contract.Assert(lamba.Parameters.Count == 1);
                Contract.Assert(replacement != null);

                _from = lamba.Parameters[0];
                _to = replacement;
                
                return Visit(lamba.Body);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _from ? _to : base.VisitParameter(node);
            }
        }
    }
}
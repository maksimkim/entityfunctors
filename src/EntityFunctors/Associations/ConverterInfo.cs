namespace EntityFunctors.Associations
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using Expressions;
    using Extensions;
    using Exp = System.Linq.Expressions.Expression;

    public class ConverterInfo
    {
        private Func<object, object> _lamba;

        public LambdaExpression Expression { get; private set; }

        public Func<object, object> UntypedLambda
        {
            get
            {
                if (_lamba != null)
                    return _lamba;

                var inputType = Expression.Parameters[0].Type;
                
                var param = Exp.Parameter(typeof(object), "_");
                //var typed = Exp.Variable(inputType);
                var typed = Expression.Parameters[0];
                var body = Expression.Apply(typed);
                
                var exp = Exp.Lambda<Func<object, object>>(
                    Exp.Block(
                        new[] { typed },
                        new[]
                        {
                            Exp.Assign(typed, inputType != typeof(object) ? (Exp)Exp.Convert(param, inputType) : param),
                            Expression.ReturnType != typeof(object) ? Exp.Convert(body, typeof(object)) : body
                        }
                    ), 
                    param
                );

                return (_lamba = exp.Compile());
            }
        }

        public ConverterInfo(LambdaExpression expression)
        {
            Contract.Assert(expression != null);
            Contract.Assert(expression.Parameters.Count == 1);
            
            Expression = expression;
        }
    }
}
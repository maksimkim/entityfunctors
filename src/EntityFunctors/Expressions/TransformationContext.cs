namespace EntityFunctors.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    public class TransformationContext
    {
        public ParameterExpression Parameter { get; set; }

        public bool ParameterReplaced { get; set; }

        public IDictionary<Expression, Tuple<Expression, PathNode>> Replacements { get; private set; }

        public IDictionary<PropertyInfo, Func<Expression, ParameterExpression, Expression>> Transformers { get; set; }

        public IDictionary<PropertyInfo, Delegate> Converters { get; set; }

        public ExpressionNavigator Navigator { get; private set; }
        
        public TransformationContext()
        {
            Replacements = new Dictionary<Expression, Tuple<Expression, PathNode>>();

            Navigator = new ExpressionNavigator();
        }

        public void SetReplacement(Expression replacement)
        {
            Navigator.SetReplacement(replacement);
        }

        public void SetReplacement(Expression original, Expression replacement)
        {
            
        }
    }
}
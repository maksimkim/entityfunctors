namespace EntityFunctors.Associations
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;

    public class PropertyPart
    {
        public PropertyInfo Property { get; private set; }

        public MethodInfo ConverterMethod { get; private set; }

        public Func<object, object> Converter { get; private set; }

        public PropertyPart(PropertyInfo property, Delegate converter = null)
        {
            Contract.Assert(property != null);

            Property = property;

            if (converter != null)
            {
                ConverterMethod = converter.Method;
                
                var param = Expression.Parameter(typeof(object), "_");

                Converter = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Call(
                            converter.Method,
                            Expression.Convert(
                                param, 
                                ConverterMethod.GetParameters()[0].ParameterType
                            )
                        ),
                        typeof(object)
                    ),
                    param
                ).Compile();
            }
        }
    }
}
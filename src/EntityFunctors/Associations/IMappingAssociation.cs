namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;

    [ContractClass(typeof (ContactForMappingAssociation))]
    public interface IMappingAssociation
    {
        Expression BuildMapper(ParameterExpression @from, ParameterExpression to, IMappingRegistry registry, ParameterExpression expands);

        PropertyInfo RewritableProperty { get; }

        MappingDirection Direction { get; }

        Expression Rewrite(Expression original, ParameterExpression parameter);

        IEnumerable<KeyValuePair<PropertyInfo, Delegate>> ValueConverters { get; }
    
    }

    [ContractClassFor(typeof(IMappingAssociation))]
    public abstract class ContactForMappingAssociation : IMappingAssociation
    {
        public Expression BuildMapper(ParameterExpression @from, ParameterExpression to, IMappingRegistry registry, ParameterExpression expands)
        {
            Contract.Assert(@from != null);
            Contract.Assert(@to != null);
            Contract.Assert(registry != null);

            return null;
        }

        public PropertyInfo RewritableProperty
        {
            get { return null; }
        }

        public Expression Rewrite(Expression original, ParameterExpression parameter)
        {
            Contract.Assert(original != null);
            Contract.Assert(parameter != null);
            
            return null;
        }

        public IEnumerable<KeyValuePair<PropertyInfo, Delegate>> ValueConverters 
        {
            get { yield break; }
        }

        public MappingDirection Direction { get; private set; }
    }
}
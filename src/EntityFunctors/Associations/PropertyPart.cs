namespace EntityFunctors.Associations
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Reflection;

    public class PropertyPart
    {
        public PropertyInfo Property { get; private set; }

        public Delegate Converter { get; private set; }

        public PropertyPart(PropertyInfo property, Delegate converter = null)
        {
            Contract.Assert(property != null);

            Property = property;

            Converter = converter;
        }
    }
}
namespace EntityFunctors
{
    using System;
    using System.Diagnostics.Contracts;

    public class TypeMapKey : IEquatable<TypeMapKey>
    {
        private Type Low { get; set; }

        private Type High { get; set; }

        public TypeMapKey(Type source, Type target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);
            Contract.Assert(source != target);

            Low = string.Compare(source.Name, target.Name, StringComparison.OrdinalIgnoreCase) < 0 ? source : target;
            High = string.Compare(source.Name, target.Name, StringComparison.OrdinalIgnoreCase) < 0 ? target : source;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TypeMapKey);
        }

        public bool Equals(TypeMapKey other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Low == other.Low && High == other.High;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return High.GetHashCode() * 397 ^ Low.GetHashCode();
            }
        }
    }
}
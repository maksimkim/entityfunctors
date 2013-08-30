namespace EntityFunctors.Associations.Fluent
{
    public interface IAccessable
    {
        void ReadOnly();

        void WriteOnly();
        
        void Read();

        void Write();
    }
}
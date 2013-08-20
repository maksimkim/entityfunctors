namespace EntityFunctors.Associations
{
    public interface IAccessable
    {
        void ReadOnly();

        void WriteOnly();
        
        void Read();

        void Write();
    }
}
namespace EntityFunctors.Associations
{
    public interface IConvertionAssociation
    {
        ConverterInfo SourceConverter { get; }

        ConverterInfo TargetConverter { get; }
    }
}
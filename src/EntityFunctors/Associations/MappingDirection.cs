namespace EntityFunctors.Associations
{
    using System;

    [Flags]
    public enum MappingDirection : byte
    {
        Read = 1, //Entity->Dto
        Write = 2, //Dto -> Entity
        All = Read | Write

    }
}
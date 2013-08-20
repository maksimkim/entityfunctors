namespace EntityFunctors.Tests.Helpers
{
    using System.Collections.Generic;

    public class Bar
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Qux Component { get; set; }

        public IEnumerable<Qux> Quxes { get; set; }

        public IEnumerable<string> Names { get; set; }

        public IEnumerable<int> Ids { get; set; }
    }
}
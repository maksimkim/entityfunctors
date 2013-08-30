namespace EntityFunctors.Tests.Helpers
{
    using System.Collections.Generic;

    public class Foo
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int OrderNo { get; set; }

        public Baz Component { get; set; }

        public IEnumerable<Baz> Bazes { get; set; }

        public IEnumerable<int> Ids { get; set; }
    }
}
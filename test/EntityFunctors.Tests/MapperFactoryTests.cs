namespace EntityFunctors.Tests
{
    using EntityFunctors;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class MapperFactoryTests
    {
        [Test]
        public void Test()
        {
            var factory = new MapperFactory(
                new FooBarMap(),
                new BazQuxMap()
            );

            var creator = factory.GetCreator<Foo, Bar>();

            var bar = 
                creator(
                    new Foo
                    {
                        Id = 10,
                        Name = "i am foo",
                        Component = new Baz
                        {
                            Id = 1
                        },
                        Bazes = new []
                        {
                            new Baz
                            {
                                Id = 2
                            },
                            new Baz
                            {
                                Id = 3
                            }
                        }
                    },
                    new string[0]
                );

        }
    }

    public class FooBarMap : TypeMap<Foo, Bar>
    {
        public FooBarMap()
        {
            MapProperties(_ => _.Id, _ => _.Id).Read();
            MapProperties(_ => _.Name, _ => _ + _, _ => _.Name, _ => _.Substring(0, 1)).Write();
            MapComponents(_ => _.Component, _ => _.Component).Expandable();
            MapComponentCollections(_ => _.Bazes, _ => _.Quxes).Expandable();
            MapCollections(_ => _.Bazes, _ => _.Names, _ => _.Id.ToString());
        }
    }

    public class BazQuxMap : TypeMap<Baz, Qux>
    {
        public BazQuxMap()
        {
            MapProperties(_ => _.Id, _ => _.Id);
        }
    }
}
namespace EntityFunctors.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EntityFunctors;
    using FluentAssertions;
    using Helpers;
    using Mappers;
    using NUnit.Framework;

    [TestFixture]
    public class CreatorMapperTests
    {
        [Test]
        public void TestProperty()
        {
            var creator = CreateSut();

            var foo = new Foo {Id = 10};
            var bar =  creator(foo, new string[0]);

            bar.Should().NotBeNull();
            bar.Id.Should().Be(foo.Id);
        }

        [Test]
        public void TestComponentProperty()
        {
            var foo = new Foo { Component = new Baz { Id = 1 } };

            var bar = CreateSut()(foo, new string[0]);

            bar.Should().NotBeNull();
            bar.Component.Should().NotBeNull();
            bar.Component.Id.Should().Be(foo.Component.Id);
        }

        [Test]
        public void TestCollectionProperty()
        {
            var foo = new Foo
            {
                Bazes = new[]
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
            };

            var bar = CreateSut()(foo, new string[0]);

            bar.Should().NotBeNull();
            bar.Quxes.Should().NotBeNull();
            bar.Quxes.Should().NotBeEmpty();
            bar.Quxes.Count().Should().Be(foo.Bazes.Count());
            bar.Quxes.Should().NotContainNulls();
            bar.Quxes.First().Id.Should().Be(foo.Bazes.First().Id);
            bar.Quxes.Last().Id.Should().Be(foo.Bazes.Last().Id);
        }

        [Test]
        public void TestComponentExpand()
        {
            var foo = new Foo
            {
                Bazes = new[]
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
            };

            var sut = CreateSut(true);

            var bar = sut(foo, new string[0]);

            bar.Should().NotBeNull();
            bar.Quxes.Should().BeNull();

            bar = sut(foo, new[] { "Quxes" });

            bar.Should().NotBeNull();
            bar.Quxes.Should().NotBeNull();
            bar.Quxes.Should().NotBeEmpty();
            bar.Quxes.Count().Should().Be(foo.Bazes.Count());
            bar.Quxes.Should().NotContainNulls();
            bar.Quxes.First().Id.Should().Be(foo.Bazes.First().Id);
            bar.Quxes.Last().Id.Should().Be(foo.Bazes.Last().Id);
        }

        [Test]
        public void TestCollectionExpand()
        {
            var foo = new Foo { Component = new Baz { Id = 1 } };

            var sut = CreateSut(true);

            var bar = sut(foo, new string[0]);

            bar.Should().NotBeNull();
            bar.Component.Should().BeNull();

            bar = sut(foo, new[] { "Component" });

            bar.Should().NotBeNull();
            bar.Component.Should().NotBeNull();
            bar.Component.Id.Should().Be(foo.Component.Id);
        }

        [Test]
        public void TestWriteOnly()
        {
            var foo = new Foo { Name = "i am foo" };

            var bar = CreateSut()(foo, new string[0]);

            bar.Should().NotBeNull();
            bar.Name.Should().BeNullOrEmpty();
        }

        private static Func<Foo, IEnumerable<string>, Bar> CreateSut(bool withExpands = false)
        {
            var factory = new MapperFactory(
                new FooBarMap(withExpands),
                new BazQuxMap()
            );

            //var sut = factory.GetCreator<Foo, Bar>();
            var sut = factory.GetReader<Foo, Bar>();

            return sut;
        }

        private class FooBarMap : TypeMap<Foo, Bar>
        {
            public FooBarMap(bool withExpands)
            {
                MapProperties(_ => _.Id, _ => _.Id).ReadOnly();
                MapProperties(_ => _.Name, _ => _ + _, _ => _.Name, _ => _.Substring(0, 1)).WriteOnly();

                var expandable = MapComponents(_ => _.Component, _ => _.Component);
                if (withExpands)
                    expandable.Expandable();

                expandable = MapComponentCollections(_ => _.Bazes, _ => _.Quxes);
                if (withExpands)
                    expandable.Expandable();

                MapCollections(_ => _.Bazes, _ => _.Names, _ => _.Id.ToString());
            }
        }

        private class BazQuxMap : TypeMap<Baz, Qux>
        {
            public BazQuxMap()
            {
                MapProperties(_ => _.Id, _ => _.Id);
            }
        }
    }


}
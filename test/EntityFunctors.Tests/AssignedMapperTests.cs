namespace EntityFunctors.Tests
{
    using System;
    using System.Collections.Generic;
    using FluentAssertions;
    using Helpers;
    using Mappers;
    using NUnit.Framework;

    [TestFixture]
    public class AssignedMapperTests
    {
        [Test]
        public void TestProperty()
        {
            var bar = new Bar {Name = "123"};

            var sut = CreateSut();

            var foo = new Foo();

            sut(bar, foo, null);

            foo.Name.Should().Be(bar.Name.Substring(0,1));
        }

        [Test]
        public void TestComponentProperty()
        {
            var bar = new Bar { Component = new Qux { Id = 1 } };

            var foo = new Foo {Component = new Baz()};

            var sut = CreateSut();

            sut(bar, foo, null);

            foo.Component.Id.Should().Be(bar.Component.Id);
        }

        [Test]
        public void TestReadonlyPropertyNotAssigned()
        {
            var bar = new Bar {Id = 10};

            var sut = CreateSut();
            
            var foo = new Foo();

            sut(bar, foo, null);

            foo.Id.Should().Be(0);
        }

        [Test]
        public void TestCollectionNotAssigned()
        {
            var bar = new Bar {Names = new[] {"1", "2", "3"}};

            var foo = new Foo();

            var sut = CreateSut();

            sut(bar, foo, null);

            foo.Bazes.Should().BeNull();
        }

        [Test]
        public void TestComponentCollectionNotAssigned()
        {
            var bar = new Bar { Quxes = new[] { new Qux{Id = 1}, new Qux {Id = 2} } };

            var foo = new Foo();

            var sut = CreateSut();

            sut(bar, foo, null);

            foo.Bazes.Should().BeNull();
        }

        [Test]
        public void TestPartialAssignment()
        {
            var bar = new Bar { Name = "123", Component = new Qux { Id = 1 } };

            var sut = CreateSut();

            var foo = new Foo { Component = new Baz() };
            sut(bar, foo, new[] {"Name"});

            foo.Name.Should().Be(bar.Name.Substring(0, 1));
            foo.Component.Id.Should().Be(0);

            foo = new Foo { Component = new Baz() };
            sut(bar, foo, new[] { "Component" });

            foo.Name.Should().BeNullOrEmpty();
            foo.Component.Id.Should().Be(bar.Component.Id);

            foo = new Foo { Component = new Baz() };
            sut(bar, foo, new[] {"Name", "Component"});
            
            foo.Name.Should().Be(bar.Name.Substring(0, 1));
            foo.Component.Id.Should().Be(bar.Component.Id);
        }

        private static Action<Bar, Foo, IEnumerable<string>> CreateSut()
        {
            var factory = new MapperFactory(
                new FooBarMap(),
                new BazQuxMap()
            );

            var sut = factory.GetAssigner<Bar, Foo>();

            return sut;
        }

        private class FooBarMap : TypeMap<Foo, Bar>
        {
            public FooBarMap()
            {
                MapProperties(_ => _.Id, _ => _.Id).ReadOnly();

                MapProperties(_ => _.Name, _ => _ + _, _ => _.Name, _ => _.Substring(0, 1)).WriteOnly();

                MapComponents(_ => _.Component, _ => _.Component);

                MapComponentCollections(_ => _.Bazes, _ => _.Quxes);

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
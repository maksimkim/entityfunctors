namespace EntityFunctors.Tests
{
    using System;
    using Associations.Impl;
    using EntityFunctors.Associations;
    using FluentAssertions;
    using Helpers;
    using Mappers;
    using NUnit.Framework;

    [TestFixture]
    public class ComponentAssociationTests
    {
        [Test]
        public void TestDirectMappingCreatesTargetComponent()
        {
            var foo = new Foo { Component = new Baz { Id = 15 } };

            var sut = new ComponentToComponentAssociation<Foo, Baz, Bar, Qux>(_ => _.Component, _ => _.Component);

            var bar = CreateReader(sut)(foo);
            bar.Should().NotBeNull();
            bar.Component.Should().NotBeNull();
            bar.Component.Id.Should().Be(0);
        }

        [Test]
        public void TestDirectMappingAppliesMapper()
        {
            var foo = new Foo { Component = new Baz { Id = 15 } };

            var sut = new ComponentToComponentAssociation<Foo, Baz, Bar, Qux>(_ => _.Component, _ => _.Component);
            var component = new PropertyToPropertyAssociation<Baz, Qux, int>(_ => _.Id, _ => _.Id);

            var bar = CreateReader(sut, component)(foo);
            bar.Should().NotBeNull();
            bar.Component.Should().NotBeNull();
            bar.Component.Id.Should().Be(foo.Component.Id);
        }

        [Test]
        public void TestInverseMappingLeavesExisitingTarget()
        {
            var foo = new Foo { Component = new Baz { Id = 15 } };
            var bar = new Bar { Component = new Qux { Id = 16 } };

            var before = foo.Component;

            var sut = new ComponentToComponentAssociation<Foo, Baz, Bar, Qux>(_ => _.Component, _ => _.Component);

            CreateWriter(sut)(bar, foo);

            bar.Component.Should().NotBeNull();
            foo.Component.Should().BeSameAs(before);
        }

        [Test]
        public void TestInverseMappingAppliesMapper()
        {
            var foo = new Foo { Component = new Baz { Id = 15 } };
            var bar = new Bar { Component = new Qux { Id = 16 } };

            var sut = new ComponentToComponentAssociation<Foo, Baz, Bar, Qux>(_ => _.Component, _ => _.Component);
            var component = new PropertyToPropertyAssociation<Baz, Qux, int>(_ => _.Id, _ => _.Id);

            CreateWriter(sut, component)(bar, foo);

            foo.Component.Id.Should().Be(bar.Component.Id);
        }

        [Test]
        public void TestMappingAssignsDefaultValue()
        {
            var sut = new ComponentToComponentAssociation<Foo, Baz, Bar, Qux>(_ => _.Component, _ => _.Component);
            var component = new PropertyToPropertyAssociation<Baz, Qux, int>(_ => _.Id, _ => _.Id);

            var foo = new Foo();
            var bar = CreateReader(sut, component)(foo);
            bar.Should().NotBeNull();
            bar.Component.Should().BeNull();

            foo = new Foo {Component = new Baz()};
            bar = new Bar();
            CreateWriter(sut, component)(bar, foo);
            foo.Component.Should().BeNull();
        }

        private static Func<Foo, Bar> CreateReader(IMappingAssociation association)
        {
            var factory = new MapperFactory(
                new TestMap(typeof(Foo), typeof(Bar), association),
                new TestMap(typeof(Baz), typeof(Qux))
            );

            return _ => factory.GetReader<Foo, Bar>()(_, null);
        }

        private static Func<Foo, Bar> CreateReader(IMappingAssociation association, IMappingAssociation componentAssociation)
        {
            var factory = new MapperFactory(
                new TestMap(typeof(Foo), typeof(Bar), association),
                new TestMap(typeof(Baz), typeof(Qux), componentAssociation)
            );

            return _ => factory.GetReader<Foo, Bar>()(_, null);
        }

        private static Action<Bar, Foo> CreateWriter(IMappingAssociation association)
        {
            var factory = new MapperFactory(
                new TestMap(typeof(Foo), typeof(Bar), association),
                new TestMap(typeof(Baz), typeof(Qux))
            );

            return (source, target) => factory.GetWriter<Bar, Foo>()(source, target, null);
        }

        private static Action<Bar, Foo> CreateWriter(IMappingAssociation association, IMappingAssociation componentAssociation)
        {
            var factory = new MapperFactory(
                new TestMap(typeof(Foo), typeof(Bar), association),
                new TestMap(typeof(Baz), typeof(Qux), componentAssociation)
            );

            return (source, target) => factory.GetWriter<Bar, Foo>()(source, target, null);
        }
    }
}
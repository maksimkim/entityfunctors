namespace EntityFunctors.Tests
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Associations;
    using FluentAssertions;
    using Helpers;
    using Mappers;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ComponentCollectionAssociationTests
    {
        private static readonly MapperBuilder MapperBuilder = new MapperBuilder();

        [Test]
        public void TestMappingCreatesTargetComponent()
        {
            var sut = new ComponentCollectionAssociation<Foo, Baz, Bar, Qux>(_ => _.Bazes, _ => _.Quxes);

            var foo = new Foo
            {
                Bazes = new[] {new Baz(), new Baz()}
            };

            var bar = CreateReader(sut)(foo);
            bar.Quxes.Should().NotBeNull();
            bar.Quxes.Count().Should().Be(foo.Bazes.Count());
        }

        [Test]
        public void TestMappingAppliesMapper()
        {
            var sut = new ComponentCollectionAssociation<Foo, Baz, Bar, Qux>(_ => _.Bazes, _ => _.Quxes);
            var component = new PropertyToPropertyAssociation<Baz, Qux, int>(_ => _.Id, _ => _.Id);

            var foo = new Foo
            {
                Bazes = new[] { new Baz { Id = 1 }, new Baz { Id = 2 } }
            };

            var bar = CreateReader(sut, component)(foo);

            bar.Quxes.Should().NotBeNull();
            bar.Quxes.Should().NotBeEmpty();
            bar.Quxes.Select(_ => _.Id).Should().BeEquivalentTo(foo.Bazes.Select(_ => _.Id));
        }

        [Test]
        public void TestEmptyMapsToEmpty()
        {
            var sut = new ComponentCollectionAssociation<Foo, Baz, Bar, Qux>(_ => _.Bazes, _ => _.Quxes);

            var foo = new Foo
            {
                Bazes = Enumerable.Empty<Baz>()
            };

            var bar = CreateReader(sut)(foo);

            bar.Quxes.Should().NotBeNull();
            bar.Quxes.Should().BeEmpty();
        }

        [Test]
        public void TestMappingAssignsDefaultValue()
        {
            var sut = new ComponentCollectionAssociation<Foo, Baz, Bar, Qux>(_ => _.Bazes, _ => _.Quxes);

            var foo = new Foo();

            var bar = CreateReader(sut)(foo);

            bar.Quxes.Should().BeNull();
        }

        [Test]
        public void TestReverseMappingDoNothing()
        {
            var sut = new ComponentCollectionAssociation<Foo, Baz, Bar, Qux>(_ => _.Bazes, _ => _.Quxes);

            var foo = new Foo();

            var bar = new Bar
            {
                Quxes = new[] {new Qux(), new Qux()}
            };

            MapperBuilder.BuildMapper<Bar, Foo>(sut, MockRegistry())(bar, foo);

            foo.Bazes.Should().BeNull();
        }

        private IMappingRegistry MockRegistry(Func<ParameterExpression, ParameterExpression, Expression> mapperFactory = null)
        {
            var mock = new Mock<IMappingRegistry>();

            mapperFactory = mapperFactory ?? ((what, ever) => Expression.Empty());

            mock
                .Setup(r => r.GetMapper(It.IsAny<ParameterExpression>(), It.IsAny<ParameterExpression>(), It.IsAny<ParameterExpression>()))
                .Returns<ParameterExpression, ParameterExpression, ParameterExpression>((w, hat, ever) => mapperFactory(w, hat));

            return mock.Object;
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
    }
}

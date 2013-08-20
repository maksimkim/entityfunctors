namespace EntityFunctors.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using EntityFunctors.Associations;
    using EntityFunctors.Extensions;
    using FluentAssertions;
    using Helpers;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ComponentCollectionAssociationTests
    {
        private static readonly MapperBuilder MapperBuilder = new MapperBuilder();

        [Test]
        public void TestMappingCreatesTargetComponent()
        {
            var sut = CreateSut();

            var foo = new Foo
            {
                Bazes = new[] {new Baz(), new Baz()}
            };

            var bar = new Bar
            {
                Quxes = new[] {new Qux(), new Qux()}
            };

            var before = bar.Quxes;

            MapperBuilder.BuildMapper<Foo, Bar>(sut, MockRegistry())(foo, bar);

            bar.Quxes.Should().NotBeNull();
            bar.Quxes.Should().NotBeEquivalentTo(before);
        }

        [Test]
        public void TestMappingAppliesMapper()
        {
            var sut = CreateSut();

            var foo = new Foo
            {
                Bazes = new[] { new Baz { Id = 1 }, new Baz { Id = 2 } }
            };

            var bar = new Bar();

            MapperBuilder.BuildMapper<Foo, Bar>(
                sut,
                MockRegistry((from, to) => CreateSimpleAssignMapper<Baz, Qux, int>(from, _ => _.Id, to, _ => _.Id))
            )(foo, bar);

            bar.Quxes.Should().NotBeNull();
            bar.Quxes.Should().NotBeEmpty();
            bar.Quxes.Select(_ => _.Id).Should().BeEquivalentTo(foo.Bazes.Select(_ => _.Id));
        }

        [Test]
        public void TestEmptyMapsToEmpty()
        {
            var sut = CreateSut();

            var foo = new Foo
            {
                Bazes = Enumerable.Empty<Baz>()
            };

            var bar = new Bar
            {
                Quxes = new[] { new Qux(), new Qux() }
            };

            MapperBuilder.BuildMapper<Foo, Bar>(sut, MockRegistry())(foo, bar);

            bar.Quxes.Should().NotBeNull();
            bar.Quxes.Should().BeEmpty();
        }

        [Test]
        public void TestMappingAssignsDefaultValue()
        {
            var sut = CreateSut();

            var foo = new Foo();

            var bar = new Bar
            {
                Quxes = new[] { new Qux(), new Qux() }
            };

            MapperBuilder.BuildMapper<Foo, Bar>(sut, MockRegistry())(foo, bar);

            bar.Quxes.Should().BeNull();
        }

        [Test]
        public void TestReverseMappingDoNothing()
        {
            var sut = CreateSut();

            var foo = new Foo();

            var bar = new Bar
            {
                Quxes = new[] {new Qux(), new Qux()}
            };

            MapperBuilder.BuildMapper<Bar, Foo>(sut, MockRegistry())(bar, foo);

            foo.Bazes.Should().BeNull();
        }

        private static IMappingAssociation CreateSut()
        {
            Expression<Func<Foo, IEnumerable<Baz>>> source = _ => _.Bazes;
            Expression<Func<Bar, IEnumerable<Qux>>> target = _ => _.Quxes;
            
            return new ComponentCollectionAssociation<Foo, Bar>(
                new PropertyPart(source.GetProperty()),
                new PropertyPart(target.GetProperty())
            );
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

        private Expression CreateSimpleAssignMapper<TFrom, TTo, TProperty>(
            ParameterExpression from,
            Expression<Func<TFrom, TProperty>> fromProperty,
            ParameterExpression to,
            Expression<Func<TTo, TProperty>> toProperty
        )
        {
            return Expression.Assign(
                Expression.Property(to, toProperty.GetProperty()),
                Expression.Property(from, fromProperty.GetProperty())
            );
        }
    }
}

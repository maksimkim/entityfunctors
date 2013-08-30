namespace EntityFunctors.Tests
{
    using System;
    using System.Linq.Expressions;
    using EntityFunctors.Associations;
    using EntityFunctors.Extensions;
    using FluentAssertions;
    using Helpers;
    using Mappers;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ComponentAssociationTests
    {
        private static readonly MapperBuilder MapperBuilder = new MapperBuilder();

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

            MapperBuilder.BuildMapper<Bar, Foo>(sut, MockRegistry())(bar, foo);

            bar.Component.Should().NotBeNull();
            foo.Component.Should().BeSameAs(before);
        }

        [Test]
        public void TestInverseMappingAppliesMapper()
        {
            var foo = new Foo { Component = new Baz { Id = 15 } };
            var bar = new Bar { Component = new Qux { Id = 16 } };

            Expression<Func<Foo, Baz>> source = _ => _.Component;
            Expression<Func<Bar, Qux>> target = _ => _.Component;
            var sut = new ComponentToComponentAssociation<Foo, Baz, Bar, Qux>(source, target);

            var mapper = MapperBuilder.BuildMapper<Bar, Foo>(
                sut,
                MockRegistry((from, to) => CreateSimpleAssignMapper<Qux, Baz, int>(from, _ => _.Id, to, _ => _.Id))
            );

            mapper(bar, foo);

            foo.Component.Id.Should().Be(bar.Component.Id);
        }

        [Test]
        public void TestMappingAssignsDefaultValue()
        {
            Foo foo;
            Bar bar;

            var sut = new ComponentToComponentAssociation<Foo, Baz, Bar, Qux>(_ => _.Component, _ => _.Component);
            var component = new PropertyToPropertyAssociation<Baz, Qux, int>(_ => _.Id, _ => _.Id);

            foo = new Foo();
            bar = new Bar { Component = new Qux { Id = 16 } };
            bar = CreateReader(sut, component)(foo);
            bar.Should().NotBeNull();
            bar.Component.Should().BeNull();

            foo = new Foo {Component = new Baz()};
            bar = new Bar();
            MapperBuilder.BuildMapper<Bar, Foo>(
                sut,
                MockRegistry((from, to) => CreateSimpleAssignMapper<Qux, Baz, int>(from, _ => _.Id, to, _ => _.Id))
            )(bar, foo);
            foo.Component.Should().BeNull();
        }

        private IMappingRegistry MockRegistry(Func<ParameterExpression, ParameterExpression, Expression> mapperFactory = null)
        {
            var mock = new Mock<IMappingRegistry>();
  
            mapperFactory = mapperFactory ?? ((what, ever) => Expression.Empty());

            mock
                .Setup(r => r.GetMapper(It.IsAny<ParameterExpression>(), It.IsAny<ParameterExpression>(), It.IsAny<ParameterExpression>()))
                .Returns<ParameterExpression, ParameterExpression, ParameterExpression>((from, to, expands) => mapperFactory(from, to));

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
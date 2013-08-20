namespace EntityFunctors.Tests
{
    using System;
    using System.Linq.Expressions;
    using EntityFunctors.Associations;
    using EntityFunctors.Extensions;
    using FluentAssertions;
    using Helpers;
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
            var bar = new Bar { Component = new Qux { Id = 16 } };

            var before = bar.Component;

            var sut = CreateSut();

            MapperBuilder.BuildMapper<Foo, Bar>(sut, MockRegistry())(foo, bar);

            bar.Component.Should().NotBeNull();
            bar.Component.Should().NotBeSameAs(before);
        }

        [Test]
        public void TestDirectMappingAppliesMapper()
        {
            var foo = new Foo { Component = new Baz { Id = 15 } };
            var bar = new Bar { Component = new Qux { Id = 16 } };

            var sut = CreateSut();

            var mapper = MapperBuilder.BuildMapper<Foo, Bar>(
                sut,
                MockRegistry((from, to) => CreateSimpleAssignMapper<Baz, Qux, int>(from, _ => _.Id, to, _ => _.Id))
            );

            mapper(foo, bar);

            bar.Component.Id.Should().Be(foo.Component.Id);
        }

        [Test]
        public void TestInverseMappingLeavesExisitingTarget()
        {
            var foo = new Foo { Component = new Baz { Id = 15 } };
            var bar = new Bar { Component = new Qux { Id = 16 } };

            var before = foo.Component;

            var sut = CreateSut();

            MapperBuilder.BuildMapper<Bar, Foo>(sut, MockRegistry())(bar, foo);

            bar.Component.Should().NotBeNull();
            foo.Component.Should().BeSameAs(before);
        }

        [Test]
        public void TestInverseMappingAppliesMapper()
        {
            var foo = new Foo { Component = new Baz { Id = 15 } };
            var bar = new Bar { Component = new Qux { Id = 16 } };

            var sut = CreateSut();

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

            var sut = CreateSut();

            foo = new Foo();
            bar = new Bar { Component = new Qux { Id = 16 } };
            MapperBuilder.BuildMapper<Foo, Bar>(
                sut,
                MockRegistry((from, to) => CreateSimpleAssignMapper<Baz, Qux, int>(from, _ => _.Id, to, _ => _.Id))
            )(foo, bar);
            bar.Component.Should().BeNull();

            foo = new Foo {Component = new Baz()};
            bar = new Bar();
            MapperBuilder.BuildMapper<Bar, Foo>(
                sut,
                MockRegistry((from, to) => CreateSimpleAssignMapper<Qux, Baz, int>(from, _ => _.Id, to, _ => _.Id))
            )(bar, foo);
            foo.Component.Should().BeNull();
        }

        private IMappingAssociation CreateSut()
        {
            Expression<Func<Foo, Baz>> source = _ => _.Component;
            Expression<Func<Bar, Qux>> target = _ => _.Component;
            
            return new ComponentToComponentAssociation<Foo, Bar>(
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
    }
}
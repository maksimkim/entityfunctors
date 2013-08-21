namespace EntityFunctors.Tests
{
    using System;
    using System.Linq.Expressions;
    using EntityFunctors.Associations;
    using EntityFunctors.Extensions;
    using FluentAssertions;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class ExpressionToPropertyAssociationTests
    {
        private static readonly MapperBuilder MapperBuilder = new MapperBuilder();
        
        [Test]
        public void TestIntMapping()
        {
            var foo = new Foo { Id = 10 };

            var bar = new Bar { Id = 11 };

            Expression<Func<Foo, int>> source = _ => _.Id * _.Id;

            var sut = CreateSut<Foo, Bar, int>(source, _ => _.Id);

            MapperBuilder.BuildMapper<Foo, Bar>(sut)(foo, bar);

            var expected = source.Compile()(foo);

            bar.Id.Should().Be(expected);
        }

        [Test]
        public void TestStringMapping()
        {
            var foo = new Foo {Name = "123"};
            var bar = new Bar {Id = 555};

            Expression<Func<Foo, int>> source = _ => _.Name.Length;

            var sut = CreateSut<Foo, Bar, int>(source, _ => _.Id);

            MapperBuilder.BuildMapper<Foo, Bar>(sut)(foo, bar);

            var expected = source.Compile()(foo);

            bar.Id.Should().Be(expected);
        }

        [Test]
        public void TestInverseMappingDoesNothing()
        {
            var foo = new Foo { Id = 10 };

            var bar = new Bar { Id = 11 };

            Expression<Func<Foo, int>> source = _ => _.Id * _.Id;

            var sut = CreateSut<Foo, Bar, int>(source, _ => _.Id);

            MapperBuilder.BuildMapper<Bar, Foo>(sut)(bar, foo);

            foo.Id.Should().Be(10);
        }

        [Test]
        public void TestSettingWritablityThrowsOnInappropriateExpression()
        {
            Expression<Func<Foo, int>> source = _ => _.Id * _.Id;

            var sut = CreateSut<Foo, Bar, int>(source, _ => _.Id);

            ((Action) sut.Write).ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void TestInverseMappingAssignsOnWrite()
        {
            var foo = new Foo { Component = new Baz()};

            var bar = new Bar { Id = 11 };

            Expression<Func<Foo, int>> source = _ => _.Component.Id;

            var sut = CreateSut<Foo, Bar, int>(source, _ => _.Id);

            sut.Write();

            MapperBuilder.BuildMapper<Bar, Foo>(sut)(bar, foo);

            foo.Component.Id.Should().Be(11);
        }
        
        private ExpressionToPropertyAssociation<TSource, TTarget> CreateSut<TSource, TTarget, TProperty>(Expression<Func<TSource, TProperty>> source, Expression<Func<TTarget, TProperty>> target)
        {
            return new ExpressionToPropertyAssociation<TSource, TTarget>(
                source,
                new PropertyPart(target.GetProperty())
            );
        }
    }
}

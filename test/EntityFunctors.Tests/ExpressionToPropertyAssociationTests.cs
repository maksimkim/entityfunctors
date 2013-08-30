namespace EntityFunctors.Tests
{
    using System;
    using System.Linq.Expressions;
    using EntityFunctors.Associations;
    using FluentAssertions;
    using Helpers;
    using Mappers;
    using NUnit.Framework;

    [TestFixture]
    public class ExpressionToPropertyAssociationTests
    {
        private static readonly MapperBuilder MapperBuilder = new MapperBuilder();
        
        [Test]
        public void TestIntMapping()
        {
            var foo = new Foo { Id = 10 };

            Expression<Func<Foo, int>> source = _ => _.Id * _.Id;

            var sut = new ExpressionToPropertyAssociation<Foo, Bar, int>(source, _ => _.Id);

            var expected = source.Compile()(foo);

            var bar = CreateReader(sut)(foo);
            bar.Should().NotBeNull();
            bar.Id.Should().Be(expected);
        }

        [Test]
        public void TestStringMapping()
        {
            var foo = new Foo {Name = "123"};

            Expression<Func<Foo, int>> source = _ => _.Name.Length;

            var sut = new ExpressionToPropertyAssociation<Foo, Bar, int>(source, _ => _.Id);

            var expected = source.Compile()(foo);

            var bar = CreateReader(sut)(foo);
            bar.Should().NotBeNull();
            bar.Id.Should().Be(expected);
        }

        [Test]
        public void TestInverseMappingDoesNothing()
        {
            var foo = new Foo { Id = 10 };

            Expression<Func<Foo, int>> source = _ => _.Id * _.Id;

            var sut = new ExpressionToPropertyAssociation<Foo, Bar, int>(source, _ => _.Id);

            var bar = CreateReader(sut)(foo);
            bar.Should().NotBeNull();
            foo.Id.Should().Be(10);
        }

        [Test]
        public void TestSettingWritablityThrowsOnInappropriateExpression()
        {
            Expression<Func<Foo, int>> source = _ => _.Id * _.Id;

            var sut = new ExpressionToPropertyAssociation<Foo, Bar, int>(source, _ => _.Id);

            ((Action) sut.Write).ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void TestInverseMappingAssignsOnWrite()
        {
            var foo = new Foo { Component = new Baz()};

            var bar = new Bar { Id = 11 };

            Expression<Func<Foo, int>> source = _ => _.Component.Id;

            var sut = new ExpressionToPropertyAssociation<Foo, Bar, int>(source, _ => _.Id);

            sut.Write();

            MapperBuilder.BuildMapper<Bar, Foo>(sut)(bar, foo);

            foo.Component.Id.Should().Be(11);
        }

        private static Func<Foo, Bar> CreateReader(IMappingAssociation association)
        {
            var factory = new MapperFactory(new TestMap(typeof(Foo), typeof(Bar), association));

            return _ => factory.GetReader<Foo, Bar>()(_, null);
        }
    }
}

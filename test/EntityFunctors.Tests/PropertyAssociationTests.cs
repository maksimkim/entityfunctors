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
    public class PropertyAssociationTests
    {
        private static readonly MapperBuilder MapperBuilder = new MapperBuilder();
        
        [Test]
        public void TestIntMapping()
        {
            Foo foo;
            Bar bar;

            var sut = CreateSut<Foo, Bar, int>(_ => _.Id, _ => _.Id);

            foo = new Foo { Id = 5 };
            bar = new Bar { Id = 6 };
            MapperBuilder.BuildMapper<Foo, Bar>(sut)(foo, bar);
            bar.Id.Should().Be(foo.Id);

            foo = new Foo { Id = 5 };
            bar = new Bar { Id = 6 };
            MapperBuilder.BuildMapper<Bar, Foo>(sut)(bar, foo);
            foo.Id.Should().Be(bar.Id);
        }

        [Test]
        public void TestStringMapping()
        {
            Foo foo;
            Bar bar;

            var sut = CreateSut<Foo, Bar, string>(_ => _.Name, _ => _.Name);

            foo = new Foo { Name = "aaa" };
            bar = new Bar { Name = "bbb" };
            MapperBuilder.BuildMapper<Foo, Bar>(sut)(foo, bar);
            bar.Id.Should().Be(foo.Id);

            foo = new Foo { Name = "aaa" };
            bar = new Bar { Name = "bbb" };
            MapperBuilder.BuildMapper<Bar, Foo>(sut)(bar, foo);
            foo.Id.Should().Be(bar.Id);
        }

        [Test]
        public void TestIntToStringMapping()
        {
            Foo foo;
            Bar bar;

            var sut = CreateSut<Foo, int, Bar, string>(_ => _.Id, _ => _.ToString(), _ => _.Name, int.Parse);

            foo = new Foo { Id = 123 };
            bar = new Bar { Name = "bbb" };
            MapperBuilder.BuildMapper<Foo, Bar>(sut)(foo, bar);
            bar.Name.Should().Be(foo.Id.ToString());

            foo = new Foo { Id = 123 };
            bar = new Bar { Name = "555" };
            MapperBuilder.BuildMapper<Bar, Foo>(sut)(bar, foo);
            foo.Id.Should().Be(int.Parse(bar.Name));
        }

        [Test]
        public void TestStringToIntDefaultMapping()
        {
            Foo foo;
            Bar bar;

            var sut = CreateSut<Foo, int, Bar, string>(_ => _.Id, _ => _.ToString(), _ => _.Name, int.Parse);


            foo = new Foo { Id = 123 };
            bar = new Bar { Name = string.Empty };
            MapperBuilder.BuildMapper<Bar, Foo>(sut)(bar, foo);
            foo.Id.Should().Be(default(int));
        }

        private static PropertyToPropertyAssociation<TSource, TTarget> CreateSut<TSource, TTarget, TProperty>(
            Expression<Func<TSource, TProperty>> source,
            Expression<Func<TTarget, TProperty>> target
        )
        {
            return new PropertyToPropertyAssociation<TSource, TTarget>(
                new PropertyPart(source.GetProperty()),
                new PropertyPart(target.GetProperty())
            );
        }

        private static PropertyToPropertyAssociation<TSource, TTarget> CreateSut<TSource, TSourceProperty, TTarget, TTargetProperty>(
            Expression<Func<TSource, TSourceProperty>> source,
            Func<TSourceProperty, TTargetProperty> converter,
            Expression<Func<TTarget, TTargetProperty>> target,
            Func<TTargetProperty, TSourceProperty> inverseConverter
        )
        {
            return new PropertyToPropertyAssociation<TSource, TTarget>(
                new PropertyPart(source.GetProperty(), converter),
                new PropertyPart(target.GetProperty(), inverseConverter)
            );
        }
    }
}

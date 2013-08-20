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
    using NUnit.Framework;

    [TestFixture]
    public class CollectionAssociationTests
    {
        private static readonly MapperBuilder MapperBuilder = new MapperBuilder();
        
        [Test]
        public void TestMappingCreatesTargetComponent()
        {
            var sut = CreateSut();

            var foo = new Foo
            {
                Bazes = new[] { new Baz(), new Baz() }
            };

            var bar = new Bar
            {
                Names = new[] { "a", "b" }
            };

            var before = bar.Names;

            MapperBuilder.BuildMapper<Foo, Bar>(sut)(foo, bar);

            bar.Names.Should().NotBeNull();
            bar.Names.Should().NotBeEquivalentTo(before);
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

            MapperBuilder.BuildMapper<Foo, Bar>(sut)(foo, bar);

            bar.Names.Should().NotBeNull();
            bar.Names.Should().NotBeEmpty();
            bar.Names.Should().BeEquivalentTo(foo.Bazes.Select(_ => _.Id.ToString()));
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
                Names = new[] { "a", "b" }
            };

            MapperBuilder.BuildMapper<Foo, Bar>(sut)(foo, bar);

            bar.Names.Should().NotBeNull();
            bar.Names.Should().BeEmpty();
        }

        [Test]
        public void TestMappingAssignsDefaultValue()
        {
            var sut = CreateSut();

            var foo = new Foo();

            var bar = new Bar
            {
                Names = new[] { "a", "b" }
            };

            MapperBuilder.BuildMapper<Foo, Bar>(sut)(foo, bar);

            bar.Names.Should().BeNull();
        }

        [Test]
        public void TestReverseMappingDoNothing()
        {
            var sut = CreateSut();

            var foo = new Foo();

            var bar = new Bar
            {
                Names = new[] { "a", "b" }
            };

            MapperBuilder.BuildMapper<Bar, Foo>(sut)(bar, foo);

            foo.Bazes.Should().BeNull();
        }

        private static IMappingAssociation CreateSut()
        {
            Expression<Func<Foo, IEnumerable<Baz>>> source = _ => _.Bazes;

            Expression<Func<Bar, IEnumerable<string>>> target = _ => _.Names;

            return new CollectionAssociation<Foo, Bar>(
                new PropertyPart(source.GetProperty()),
                new PropertyPart(target.GetProperty()),
                (Expression<Func<Baz, string>>) (_ => _.Id.ToString())
            );
        }
    }
}

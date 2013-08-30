namespace EntityFunctors.Tests
{
    using System;
    using System.Linq.Expressions;
    using Associations.Impl;
    using EntityFunctors.Associations;
    using EntityFunctors.Extensions;
    using FluentAssertions;
    using Helpers;
    using Mappers;
    using NUnit.Framework;

    [TestFixture]
    public class PropertyAssociationTests
    {
        [Test]
        public void TestIntMapping()
        {
            Foo foo;
            Bar bar;

            var sut = new PropertyToPropertyAssociation<Foo, Bar, int>(_ => _.Id,_ => _.Id);

            foo = new Foo { Id = 5 };
            bar = CreateReader(sut)(foo);
            bar.Should().NotBeNull();
            bar.Id.Should().Be(foo.Id);

            foo = new Foo { Id = 5 };
            bar = new Bar { Id = 6 };
            CreateWriter(sut)(bar, foo);
            foo.Id.Should().Be(bar.Id);
        }

        [Test]
        public void TestStringMapping()
        {
            Foo foo;
            Bar bar;

            var sut = new PropertyToPropertyAssociation<Foo, Bar, string>(_ => _.Name,_ => _.Name);

            foo = new Foo { Name = "aaa" };
            bar = CreateReader(sut)(foo);
            bar.Should().NotBeNull();
            bar.Id.Should().Be(foo.Id);

            foo = new Foo { Name = "aaa" };
            bar = new Bar { Name = "bbb" };
            CreateWriter(sut)(bar, foo);
            foo.Id.Should().Be(bar.Id);
        }

        [Test]
        public void TestIntToStringMapping()
        {
            Foo foo;
            Bar bar;

            var sut = new PropertyToPropertyWithConversionAssociation<Foo, int, Bar, string>(_ => _.Id, _ => _.ToString(), _ => _.Name, _ => int.Parse(_));

            foo = new Foo { Id = 123 };
            bar = CreateReader(sut)(foo);
            bar.Should().NotBeNull();
            bar.Name.Should().Be(foo.Id.ToString());

            foo = new Foo { Id = 123 };
            bar = new Bar { Name = "555" };
            CreateWriter(sut)(bar, foo);
            foo.Id.Should().Be(int.Parse(bar.Name));
        }

        [Test]
        public void TestStringToIntDefaultMapping()
        {
            Foo foo;
            Bar bar;

            var sut = new PropertyToPropertyWithConversionAssociation<Foo, int, Bar, string>(_ => _.Id, _ => _.ToString(), _ => _.Name, _ => int.Parse(_));

            foo = new Foo { Id = 123 };
            bar = new Bar { Name = string.Empty };
            CreateWriter(sut)(bar, foo);
            foo.Id.Should().Be(default(int));
        }

        private static Func<Foo, Bar> CreateReader(IMappingAssociation association)
        {
            var factory = new MapperFactory(new TestMap(typeof(Foo), typeof(Bar), association));

            return _ => factory.GetReader<Foo, Bar>()(_, null);
        }

        private static Action<Bar, Foo> CreateWriter(IMappingAssociation association)
        {
            var factory = new MapperFactory(new TestMap(typeof(Foo), typeof(Bar), association));

            return (source, target) => factory.GetWriter<Bar, Foo>()(source, target, null);
        }
    }
}

namespace EntityFunctors.Tests
{
    using System.Linq;
    using Expressions;
    using FluentAssertions;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class ExpressionMapperTests
    {
        [Test]
        public void TestEmptyFilter()
        {
            var sut = new ExpressionMapper(new FooBarMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => true);

            exp.Should().NotBeNull();

            exp.Compile()(new Foo()).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyBinary()
        {
            var sut = new ExpressionMapper(new FooBarMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => _.Id > 3 || _.Id < -1);

            exp.Should().NotBeNull();

            exp.Compile()(new Foo()).Should().BeFalse();
        }

        [Test]
        public void TestPropertyToPropertyMethodCall()
        {
            var sut = new ExpressionMapper(new FooBarMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => _.Name.Length > 3);

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Name = "asdf"
            }).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyExtensionMethodCall()
        {
            var sut = new ExpressionMapper(new FooBarMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => _.Ids.Any(id => id > 5));

            exp.Should().NotBeNull();

            exp.Compile()(new Foo { Ids = new[] { 1, 2, 10 } }).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyWithConversionBinary()
        {
            var sut = new ExpressionMapper(new FooBarConversionMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => _.Name == "3");

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Id = 3
            }).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyWithConversionExtensionMethodCall()
        {
            var sut = new ExpressionMapper(new FooBarConversionMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => _.Names.Any());

            exp.Should().NotBeNull();

            exp.Compile()(new Foo { Ids = new[] { 0 } }).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyAccess()
        {
            var sut = new ExpressionMapper(new FooBarMap());

            var exp = sut.Map<Bar, Foo, int>(_ => _.Id);

            exp.Compile()(new Foo
            {
                Id = 10
            }).Should().Be(10);
        }

        [Test]
        public void TestPropertyToPropertyWithConversionAccess()
        {
            var sut = new ExpressionMapper(new FooBarConversionMap());

            var exp = sut.Map<Bar, string, Foo, int>(_ => _.Name);

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Id = 3
            }).Should().Be(3);
        }

        [Test]
        public void TestExpressionToProperty()
        {
            var sut = new ExpressionMapper(new FooBarExpressionMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => _.Name == "123");

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Component = new Baz
                {
                    Id = 123
                }
            }).Should().BeTrue();
        }

        [Test]
        public void TestComponentToComponentBinary()
        {
            var sut = new ExpressionMapper(new FooBarMap(), new BazQuxMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => _.Component.Id > 3);

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Component = new Baz
                {
                    Id = 4
                }
            }).Should().BeTrue();
        }

        [Test]
        public void TestComponentToComponentMethodCall()
        {
            var sut = new ExpressionMapper(new FooBarMap(), new BazQuxMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => _.Component.Id.ToString().Length > 3);

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Component = new Baz
                {
                    Id = 4000
                }
            }).Should().BeTrue();
        }

        [Test]
        public void TestComponentCollectionBinary()
        {
            var sut = new ExpressionMapper(new FooBarMap(), new BazQuxMap());

            var exp = sut.Map<Bar, Foo, bool>(_ => _.Quxes.Any(q => q.Id > 10));

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Bazes = new[]
                {
                    new Baz
                    {
                        Id = 20
                    }
                }
            }).Should().BeTrue();
        }

        private class FooBarMap : TypeMap<Foo, Bar>
        {
            public FooBarMap()
            {
                MapProperties(_ => _.Id, _ => _.Id);
                MapProperties(_ => _.Name, _ => _.Name);
                MapProperties(_ => _.Ids, _ => _.Ids);
                MapComponents(_ => _.Component, _ => _.Component);
                MapComponentCollections(_ => _.Bazes, _ => _.Quxes);
            }
        }

        private class BazQuxMap : TypeMap<Baz, Qux>
        {
            public BazQuxMap()
            {
                MapProperties(_ => _.Id, _ => _.Id);
            }
        }

        private class FooBarConversionMap : TypeMap<Foo, Bar>
        {
            public FooBarConversionMap()
            {
                MapProperties(_ => _.Id, _ => _.ToString(), _ => _.Name, int.Parse);
                MapProperties(_ => _.Ids, _ => _.Select(__ => __.ToString()), _ => _.Names, _ => _.Select(int.Parse));
            }
        }

        private class FooBarExpressionMap : TypeMap<Foo, Bar>
        {
            public FooBarExpressionMap()
            {
                MapExpressionToProperty(_ => _.Component.Id.ToString(), _ => _.Name);
            }
        }
    }
}

namespace EntityFunctors.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using EntityFunctors.Associations;
    using EntityFunctors.Expressions;
    using EntityFunctors.Extensions;
    using FluentAssertions;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class QueryTransformerTests
    {
        [Test]
        public void TestEmptyFilter()
        {
            var sut = new QueryTransformer<Bar, Foo>(Enumerable.Empty<IMappingAssociation>());
            
            var exp = sut.Transform(_ => true);

            exp.Should().NotBeNull();
            exp.Compile()(new Foo()).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyBinary()
        {
            var sut = CreateSut<Foo, Bar, int>(_ => _.Id, _ => _.Id);

            var exp = sut.Transform(_ => _.Id > 3 || _.Id < -1);

            exp.Should().NotBeNull();

            exp.Compile()(new Foo()).Should().BeFalse();
        }

        [Test]
        public void TestPropertyToPropertyMethodCall()
        {
            var sut = CreateSut<Foo, Bar, string>(_ => _.Name, _ => _.Name);

            var exp = sut.Transform(_ => _.Name.Length > 3);

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Name = "asdf"
            }).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyExtensionMethodCall()
        {
            var sut = CreateSut<Foo, Bar, IEnumerable<int>>(_ => _.Ids, _ => _.Ids);

            var exp = sut.Transform(_ => Enumerable.Any(_.Ids, id => id > 5));

            exp.Should().NotBeNull();

            exp.Compile()(new Foo { Ids = new[] {1,2,10} }).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyWithConversionBinary()
        {
            var sut = CreateSut<Foo, int, Bar, string>(_ => _.Id, _ => _.ToString(), _ => _.Name, int.Parse);

            var exp = sut.Transform(_ => _.Name == "3");

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Id = 3
            }).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyWithConversionExtensionMethodCall()
        {
            var sut = CreateSut<Foo, IEnumerable<int>, Bar, IEnumerable<string>>(
                _ => _.Ids, 
                _ => _.Select(__ => __.ToString()), 
                _ => _.Names, 
                _ => _.Select(int.Parse));

            var exp = sut.Transform(_ => Enumerable.Any(_.Names));

            exp.Should().NotBeNull();

            exp.Compile()(new Foo { Ids = new[] { 0 } }).Should().BeTrue();
        }

        [Test]
        public void TestPropertyToPropertyAccess()
        {
            var sut = CreateSut<Foo, Bar, int>(_ => _.Id, _ => _.Id);

            var exp = sut.Transform(_ => _.Id);

            exp.Compile()(new Foo
            {
                Id = 10
            }).Should().Be(10);
        }

        [Test]
        public void TestPropertyToPropertyWithConversionAccess()
        {
            var sut = CreateSut<Foo, int, Bar, string>(_ => _.Id, _ => _.ToString(), _ => _.Name, int.Parse);

            var exp = sut.Transform<string, int>(_ => _.Name);

            exp.Should().NotBeNull();

            exp.Compile()(new Foo
            {
                Id = 3
            }).Should().Be(3);
        }

        [Test]
        public void TestExpressionToProperty()
        {
            var sut = CreateExpressionToPropertySut<Foo, Bar, string>(_ => _.Component.Id.ToString(), _ => _.Name);

            var exp = sut.Transform(_ => _.Name == "123");

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
            var sut = CreateSut<Foo, Baz, Bar, Qux>(_ => _.Component, _ => _.Component);

            var exp = sut.Transform(_ => _.Component.Id > 3);

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
            var sut = CreateSut<Foo, Baz, Bar, Qux>(_ => _.Component, _ => _.Component);

            var exp = sut.Transform(_ => _.Component.Id.ToString().Length > 3);

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
            var sut = CreateSut<Foo, Baz, Bar, Qux>(_ => _.Bazes, _ => _.Quxes);

            var exp = sut.Transform(_ => Enumerable.Any(_.Quxes, q => q.Id > 10));

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


        private static IQueryTransformer<TTarget, TSource> CreateSut<TSource, TTarget, TProperty>(
            Expression<Func<TSource, TProperty>> source,
            Expression<Func<TTarget, TProperty>> target
        )
        {
            return new QueryTransformer<TTarget, TSource>(new[] 
                {
                    new PropertyToPropertyAssociation<TSource, TTarget>(
                        new PropertyPart(source.GetProperty()),
                        new PropertyPart(target.GetProperty())
                    )
                }
            );
        }

        private static IQueryTransformer<TTarget, TSource> CreateExpressionToPropertySut<TSource, TTarget, TProperty>(
            Expression<Func<TSource, TProperty>> source,
            Expression<Func<TTarget, TProperty>> target
        )
        {
            return new QueryTransformer<TTarget, TSource>(new[]
            {
                new ExpressionToPropertyAssociation<TSource, TTarget>(
                    source,
                    new PropertyPart(target.GetProperty())
                )
            });
        }

        private static IQueryTransformer<TTarget, TSource> CreateSut<TSource, TSourceProperty, TTarget, TTargetProperty>(
            Expression<Func<TSource, TSourceProperty>> source,
            Func<TSourceProperty, TTargetProperty> converter,
            Expression<Func<TTarget, TTargetProperty>> target,
            Func<TTargetProperty, TSourceProperty> inverseConverter
        )
        {
            return new QueryTransformer<TTarget, TSource>(new[] 
                {
                    new PropertyToPropertyAssociation<TSource, TTarget>(
                        new PropertyPart(source.GetProperty(), converter),
                        new PropertyPart(target.GetProperty(), inverseConverter)
                    )
                }
            );
        }

        private IQueryTransformer<TTarget, TSource> CreateSut<TSource, TSourceProperty, TTarget, TTargetProperty>(
            Expression<Func<TSource, TSourceProperty>> source,
            Expression<Func<TTarget, TTargetProperty>> target
        )
        {
            return new QueryTransformer<TTarget, TSource>(new[] 
                {
                    new ComponentToComponentAssociation<Foo, Bar>(
                        new PropertyPart(source.GetProperty()),
                        new PropertyPart(target.GetProperty())
                    )
                }
            );
        }

        private IQueryTransformer<TTarget, TSource> CreateSut<TSource, TSourceItem, TTarget, TTargetItem>(
            Expression<Func<TSource, IEnumerable<TSourceItem>>> source,
            Expression<Func<TTarget, IEnumerable<TTargetItem>>> target
        )
        {
            return new QueryTransformer<TTarget, TSource>(new[] 
                {
                    new ComponentCollectionAssociation<Foo, Bar>(
                        new PropertyPart(source.GetProperty()),
                        new PropertyPart(target.GetProperty())
                    )
                }
            );
        }
    }
}

namespace EntityFunctors.Tests
{
    using System;
    using System.Collections.Generic;
    using Associations;
    using FluentAssertions;
    using Helpers;
    using Mappers;
    using NUnit.Framework;

    //[TestFixture]
    //public class CreatorTests
    //{
    //    [Test]
    //    public void TestPropertyToPropertyInt()
    //    {
    //        var foo = new Foo { Id = 5 };
            
    //        var sut = CreateSut();
    //        var bar = sut(foo, new string[0]);

    //        bar.Should().NotBeNull();
    //        bar.Id.Should().Be(foo.Id);
    //    }

    //    [Test]
    //    public void TestPropertyToPropertyString()
    //    {
    //        var foo = new Foo { Name = "blah" };

    //        var sut = CreateSut();
    //        var bar = sut(foo, new string[0]);

    //        bar.Should().NotBeNull();
    //        bar.Name.Should().Be(foo.Name);
    //    }

    //    [Test]
    //    public void TestWriteOnlyPropertyToPropertyNotAssigned()
    //    {
    //        var foo = new Foo { Id = 5 };

    //        var sut = CreateSut(writeOnly: true);
    //        var bar = sut(foo, new string[0]);

    //        bar.Should().NotBeNull();
    //        bar.Id.Should().Be(default(int));
    //    }

    //    [Test]
    //    public void TestPropertyToPropertyWithConversion()
    //    {
    //        var foo = new Foo { OrderNo = 666 };

    //        var sut = CreateSut();
    //        var bar = sut(foo, new string[0]);

    //        bar.Should().NotBeNull();
    //        bar.OrderNo.Should().Be(foo.OrderNo.ToString());
    //    }

    //    private Func<Foo, IEnumerable<string>, Bar> CreateSut(bool writeOnly = false)
    //    {
    //        return new MapperFactory(new FooBarMap(writeOnly)).GetCreator2<Foo, Bar>();
    //    }
        
    //    private class FooBarMap : TypeMap<Foo, Bar>
    //    {
    //        public FooBarMap(bool writeOnly)
    //        {
    //            var prop = MapProperties(_ => _.Id, _ => _.Id);
    //            if (writeOnly)
    //                prop.WriteOnly();

    //            prop = MapProperties(_ => _.Name, _ => _.Name);
    //            if (writeOnly)
    //                prop.WriteOnly();

    //            prop = MapProperties(_ => _.OrderNo, _ => _.ToString(), _ => _.OrderNo, int.Parse);
    //            if (writeOnly)
    //                prop.WriteOnly();
    //        }
    //    }
    //}

    //[TestFixture]
    //public class AssignerTests
    //{
    //    [Test]
    //    public void TestPropertyToPropertyInt()
    //    {
    //        var bar = new Bar { Id = 6 };
    //        var foo = new Foo();
            
    //        var sut = CreateSut();
    //        sut(bar, foo, new string[0]);

    //        foo.Should().NotBeNull();
    //        foo.Id.Should().Be(bar.Id);
    //    }

    //    [Test]
    //    public void TestPropertyToPropertyString()
    //    {
    //        var bar = new Bar { Name = "uglY" };
    //        var foo = new Foo();

    //        var sut = CreateSut();
    //        sut(bar, foo, new string[0]);

    //        foo.Should().NotBeNull();
    //        foo.Name.Should().Be(bar.Name);
    //    }

    //    [Test]
    //    public void TestReadonlyPropertyToPropertyNotAssigned()
    //    {
    //        var bar = new Bar { Id = 6 };
    //        var foo = new Foo();

    //        var sut = CreateSut(readOnly : true);
    //        sut(bar, foo, new string[0]);

    //        foo.Should().NotBeNull();
    //        foo.Id.Should().Be(default(int));
    //    }

    //    private Action<Bar, Foo, IEnumerable<string>> CreateSut(bool readOnly = false)
    //    {
    //        return new MapperFactory(new FooBarMap(readOnly)).GetAssigner2<Bar, Foo>();
    //    }

    //    private class FooBarMap : TypeMap<Foo, Bar>
    //    {
    //        public FooBarMap(bool readOnly)
    //        {
    //            var prop = MapProperties(_ => _.Id, _ => _.Id);
    //            if (readOnly)
    //                prop.ReadOnly();
    //            prop = MapProperties(_ => _.Name, _ => _.Name);
    //            if (readOnly)
    //                prop.ReadOnly();
    //        }
    //    }
    //}

    
}
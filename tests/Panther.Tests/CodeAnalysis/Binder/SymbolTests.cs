using System.Collections.Immutable;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Shouldly;
using Xunit;
using Symbol = Panther.CodeAnalysis.Binder.Symbol;

namespace Panther.Tests.CodeAnalysis.Binder;

public class SymbolTests
{
    [Fact]
    public void LookupInRoot()
    {
        var global = Symbol.NewRoot();

        var (x, _) = global.DeclareClass("x", TextLocation.None);
        var (y, _) = global.DeclareClass("y", TextLocation.None);

        Assert.Equal(x, global.Lookup("x"));
        Assert.Equal(y, global.Lookup("y"));
    }

    [Fact]
    public void DeclareSymbolInScope()
    {
        var global = Symbol.NewRoot();
        var (x, _) = global.DeclareClass("x", TextLocation.None);

        var (y, _) = x.DeclareField("y", TextLocation.None);

        y.FullName.ShouldBe("x.y");
    }

    [Fact]
    public void LookupInParentScope()
    {
        var global = Symbol.NewRoot();
        var (x, _) = global.DeclareClass("x", TextLocation.None);
        var (y, _) = x.DeclareField("y", TextLocation.None);

        Assert.Equal(x, y.Lookup("x"));
    }

    [Fact]
    public void LookupInGrandParentScope()
    {
        var global = Symbol.NewRoot();
        var (x, _) = global.DeclareClass("x", TextLocation.None);
        var (y, _) = x.DeclareField("y", TextLocation.None);
        var (z, _) = y.DeclareField("z", TextLocation.None);

        Assert.Equal(x, z.Lookup("x"));
    }

    [Fact]
    public void LookupInSiblingScope()
    {
        var global = Symbol.NewRoot();
        var (x, _) = global.DeclareClass("x", TextLocation.None);
        var (y, _) = global.DeclareClass("y", TextLocation.None);

        Assert.Equal(y, x.Lookup("y"));
    }

    [Fact]
    public void DeclareSymbolInParentScope()
    {
        var global = Symbol.NewRoot();
        var (x, _) = global.DeclareClass("x", TextLocation.None);
        var (y, _) = x.DeclareField("y", TextLocation.None);

        var (z, _) = global.DeclareClass("z", TextLocation.None);
        var (y2, _) = z.DeclareField("y", TextLocation.None);

        y2.FullName.ShouldBe("z.y");
    }

    [Fact]
    public void DeclareSymbolInGlobalScope()
    {
        var global = Symbol.NewRoot();
        var (x, _) = global.DeclareClass("x", TextLocation.None);
        var (y, _) = x.DeclareField("y", TextLocation.None);

        var (y2, _) = global.DeclareField("y", TextLocation.None);

        y2.FullName.ShouldBe("y");
        y.FullName.ShouldBe("x.y");
    }
}

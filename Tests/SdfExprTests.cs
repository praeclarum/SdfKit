using System.Reflection;

namespace Tests;

public class SdfExprTests
{
    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public void ExprSdfsAreDynamic()
    {
        var e = SdfExprs.Sphere(1);
        var d = e.ToSdf();
        var t = d.GetType();
        var meth = d.GetMethodInfo();
        var mod = meth.Module;
        var asm = mod.Assembly;
        Assert.AreEqual(true, asm.IsDynamic);
    }
}

namespace Dodkin.Tests.Dispatch;

using Dodkin.Dispatch;

[TestClass]
public class AssemblyQualifiedTypeNameTests
{
    [TestMethod]
    public void Parse_Int_FullName()
    {
        var typeName = AssemblyQualifiedTypeName.Parse(typeof(int).AssemblyQualifiedName);
        Assert.AreEqual("System.Int32", typeName.FullName);
    }

    [TestMethod]
    public void Parse_ArrayOfInt_FullNameWithEmptyBrackets()
    {
        var typeName = AssemblyQualifiedTypeName.Parse(typeof(int[]).AssemblyQualifiedName);
        Assert.AreEqual("System.Int32[]", typeName.FullName);
    }

    [TestMethod]
    public void Parse_ComplexGenericType_AllGenericParameters()
    {
        var typeName = AssemblyQualifiedTypeName.Parse(typeof(Dictionary<string, List<Dictionary<int, bool>>>).AssemblyQualifiedName);

        Assert.AreEqual("System.Collections.Generic.Dictionary`2", typeName.FullName);
        Assert.AreEqual(2, typeName.GenericParameters.Length);
        Assert.IsNotNull(typeName.GenericParameters[0]);
        Assert.AreEqual("System.String", typeName.GenericParameters[0].FullName);
        Assert.IsNotNull(typeName.GenericParameters[1]);
        Assert.AreEqual("System.Collections.Generic.List`1", typeName.GenericParameters[1].FullName);
        Assert.AreEqual(1, typeName.GenericParameters[1].GenericParameters.Length);
        Assert.IsNotNull(typeName.GenericParameters[1].GenericParameters[0]);
        Assert.AreEqual("System.Collections.Generic.Dictionary`2", typeName.GenericParameters[1].GenericParameters[0].FullName);
        Assert.AreEqual(2, typeName.GenericParameters[1].GenericParameters[0].GenericParameters.Length);
        Assert.IsNotNull(typeName.GenericParameters[1].GenericParameters[0].GenericParameters[0]);
        Assert.AreEqual("System.Int32", typeName.GenericParameters[1].GenericParameters[0].GenericParameters[0].FullName);
        Assert.IsNotNull(typeName.GenericParameters[1].GenericParameters[0].GenericParameters[1]);
        Assert.AreEqual("System.Boolean", typeName.GenericParameters[1].GenericParameters[0].GenericParameters[1].FullName);
    }

    [TestMethod]
    public void Parse_GenericTypeEmptyParams_EmptyArray()
    {
        var typeName = AssemblyQualifiedTypeName.Parse(typeof(Dictionary<,>).AssemblyQualifiedName);

        Assert.AreEqual("System.Collections.Generic.Dictionary`2", typeName.FullName);
        Assert.IsNotNull(typeName.GenericParameters.Length);
        Assert.AreEqual(0, typeName.GenericParameters.Length);
    }
}

namespace Dodkin.Dispatch;

// Copyright Christophe Bertrand.

using System.Collections.Generic;
using System.Linq;

public class AssemblyQualifiedTypeName
{
    private readonly List<AssemblyQualifiedTypeName> genericParameters;

    public string AssemblyName { get; }
    public string FullName { get; }
    public string ShortAssemblyName { get; }
    public string Version { get; }
    public string Culture { get; }
    public string PublicKeyToken { get; }
    public IEnumerable<AssemblyQualifiedTypeName> GenericParameters => this.genericParameters.AsReadOnly();

    public AssemblyQualifiedTypeName(string typeName)
    {
        this.genericParameters = new List<AssemblyQualifiedTypeName>();

        var index = -1;
        var rootBlock = new Block();

        var bcount = 0;
        var currentBlock = rootBlock;
        for (var i = 0; i < typeName.Length; ++i)
        {
            var c = typeName[i];
            if (c == '[')
            {
                ++bcount;
                var b = new Block() { iStart = i + 1, level = bcount, parentBlock = currentBlock };
                currentBlock.innerBlocks.Add(b);
                currentBlock = b;
            }
            else if (c == ']')
            {
                currentBlock.iEnd = i - 1;
                if (typeName[currentBlock.iStart] != '[')
                {
                    currentBlock.parsedAssemblyQualifiedName = new AssemblyQualifiedTypeName(typeName.Substring(currentBlock.iStart, i - currentBlock.iStart));
                    if (bcount == 2)
                        this.genericParameters.Add(currentBlock.parsedAssemblyQualifiedName);
                }
                currentBlock = currentBlock.parentBlock;
                --bcount;
            }
            else if (bcount == 0 && c == ',')
            {
                index = i;
                break;
            }
        }

        this.FullName = typeName.Substring(0, index);
        this.AssemblyName = typeName.Substring(index + 2);


        var parts = this.AssemblyName.Split(',').Select(x => x.Trim()).ToList();
        this.Version = LookForPairThenRemove(parts, "Version");
        this.Culture = LookForPairThenRemove(parts, "Culture");
        this.PublicKeyToken = LookForPairThenRemove(parts, "PublicKeyToken");
        if (parts.Count > 0)
            this.ShortAssemblyName = parts[0];
    }

    private static string LookForPairThenRemove(List<string> strings, string Name)
    {
        for (var istr = 0; istr < strings.Count; istr++)
        {
            var s = strings[istr];
            var i = s.IndexOf(Name);
            if (i == 0)
            {
                var i2 = s.IndexOf('=');
                if (i2 > 0)
                {
                    var ret = s.Substring(i2 + 1);
                    strings.RemoveAt(istr);
                    return ret;
                }
            }
        }
        return null;
    }

    private class Block
    {
        public int iStart;
        public int iEnd;
        public int level;
        public Block parentBlock;
        public List<Block> innerBlocks = new();
        public AssemblyQualifiedTypeName parsedAssemblyQualifiedName;
    }
}

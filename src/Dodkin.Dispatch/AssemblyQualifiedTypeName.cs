namespace Dodkin.Dispatch;

using System.Diagnostics.CodeAnalysis;

sealed class AssemblyQualifiedTypeName : IParsable<AssemblyQualifiedTypeName>, ISpanParsable<AssemblyQualifiedTypeName>
{
    private AssemblyQualifiedTypeName(string fullName, string assemblyName, string version, string culture, string publicKeyToken, AssemblyQualifiedTypeName[] genericParameters)
    {
        this.FullName = fullName;
        this.AssemblyName = assemblyName;
        this.Version = version;
        this.Culture = culture;
        this.PublicKeyToken = publicKeyToken;
        this.GenericParameters = genericParameters;
    }

    public string FullName { get; }
    public string AssemblyName { get; }
    public string Version { get; }
    public string Culture { get; }
    public string PublicKeyToken { get; }
    public AssemblyQualifiedTypeName[] GenericParameters { get; }

    public static bool TryParse(ReadOnlySpan<char> typeName, [MaybeNullWhen(false)] out AssemblyQualifiedTypeName assemblyQualifiedTypeName)
    {
        var typeNameEnumerator = EnumerateTypeNames(typeName);
        if (!typeNameEnumerator.MoveNext())
        {
            assemblyQualifiedTypeName = null!;
            return false;
        }

        var fullName = string.Empty;
        var assemblyName = string.Empty;
        var version = string.Empty;
        var culture = string.Empty;
        var publicKeyToken = string.Empty;
        var genericParameters = Array.Empty<AssemblyQualifiedTypeName>();

        foreach (var typeNamePart in EnumerateParts(typeNameEnumerator.Current))
        {
            if (typeNamePart.Kind == TypeNamePartKind.AssemblyName)
            {
                assemblyName = typeNamePart.Span.ToString();
            }
            else if (typeNamePart.Kind == TypeNamePartKind.Version)
            {
                version = typeNamePart.Span.ToString();
            }
            else if (typeNamePart.Kind == TypeNamePartKind.Culture)
            {
                culture = typeNamePart.Span.ToString();
            }
            else if (typeNamePart.Kind == TypeNamePartKind.PublicKeyToken)
            {
                publicKeyToken = typeNamePart.Span.ToString();
            }
            else if (typeNamePart.Kind == TypeNamePartKind.FullName)
            {
                var fullNameEnd = typeNamePart.Span.IndexOf('[');
                var genericParamCountStart = fullNameEnd > 0 ? typeNamePart.Span[..fullNameEnd].LastIndexOf('`') : -1;
                if (genericParamCountStart > 0 &&
                    int.TryParse(typeNamePart.Span[(genericParamCountStart + 1)..fullNameEnd], out var genericParamCount))
                {
                    fullName = typeNamePart.Span[..fullNameEnd].ToString();
                    genericParameters = new AssemblyQualifiedTypeName[genericParamCount];
                    genericParamCount = 0;
                    foreach (var nestedTypeName in EnumerateTypeNames(typeNamePart.Span[fullNameEnd..]))
                    {
                        if (!TryParse(nestedTypeName, out var genericParameter))
                        {
                            assemblyQualifiedTypeName = null!;
                            return false;
                        }
                        genericParameters[genericParamCount++] = genericParameter;
                    }
                }
                else
                {
                    fullName = typeNamePart.Span.ToString();
                }
            }
        }

        assemblyQualifiedTypeName = new(fullName, assemblyName, version, culture, publicKeyToken, genericParameters);
        return true;
    }

    public static AssemblyQualifiedTypeName Parse(ReadOnlySpan<char> typeName)
    {
        if (!TryParse(typeName, out var assemblyQualifiedTypeName))
            throw new FormatException();

        return assemblyQualifiedTypeName;
    }

    private static TypeNamePartEnumerator EnumerateParts(ReadOnlySpan<char> typeName) => new(typeName);

    private static TypeNameEnumerator EnumerateTypeNames(ReadOnlySpan<char> typeNames) => new(typeNames);

    public static AssemblyQualifiedTypeName Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out AssemblyQualifiedTypeName result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static AssemblyQualifiedTypeName Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        Parse(s);

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out AssemblyQualifiedTypeName result) =>
        TryParse(s, out result);

    private enum TypeNamePartKind
    {
        Unknown,
        FullName,
        AssemblyName,
        Version,
        Culture,
        PublicKeyToken,
    }

    private readonly ref struct TypeNamePart
    {
        public TypeNamePart(ReadOnlySpan<char> span, TypeNamePartKind kind)
        {
            this.Span = span;
            this.Kind = kind;
        }

        public ReadOnlySpan<char> Span { get; }
        public TypeNamePartKind Kind { get; }
    }

    /// <summary>
    /// Enumerates the parts of an assembly quailified type name.
    /// </summary>
    private ref struct TypeNamePartEnumerator
    {
        private ReadOnlySpan<char> span;
        private TypeNamePartKind kind;

        public TypeNamePartEnumerator(ReadOnlySpan<char> span)
        {
            this.span = span;
            this.Current = default;
            this.kind = TypeNamePartKind.Unknown;
        }

        public TypeNamePart Current { get; private set; }

        public TypeNamePartEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (this.span.IsEmpty)
                return false;

            var brackets = 0;
            for (var i = 0; i != this.span.Length; ++i)
            {
                if (this.span[i] == '[')
                {
                    ++brackets;
                }
                else if (this.span[i] == ']')
                {
                    --brackets;
                }
                else if (this.span[i] == ',' && brackets == 0)
                {
                    this.Current = new(this.span[..i], ++this.kind);

                    this.span = this.span[i..];
                    var n = this.span.IndexOfAnyExcept(',', ' ');
                    this.span = n > 0 ? this.span[n..] : ReadOnlySpan<char>.Empty;

                    return true;
                }
            }

            this.Current = new(this.span, ++this.kind);
            this.span = ReadOnlySpan<char>.Empty;
            return true;
        }
    }

    /// <summary>
    /// Enumerates the type names respecting the square brackets.
    /// </summary>
    private ref struct TypeNameEnumerator
    {
        private ReadOnlySpan<char> span;

        public TypeNameEnumerator(ReadOnlySpan<char> span)
        {
            this.span =
                span.IsEmpty || span.IsWhiteSpace() ? ReadOnlySpan<char>.Empty :
                span[0] == '[' && span[^1] == ']' ? span[1..^1] :
                span;
        }

        public ReadOnlySpan<char> Current { get; private set; }

        public TypeNameEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (this.span.IsEmpty)
                return false;

            var brackets = 0;
            for (var i = 0; i != this.span.Length; ++i)
            {
                if (this.span[i] == '[')
                {
                    ++brackets;
                }
                else if (this.span[i] == ']')
                {
                    --brackets;
                }
                else if (this.span[i] == ',' && brackets == 0)
                {
                    this.Current = this.span[..i];

                    this.span = this.span[i..];
                    var n = this.span.IndexOfAnyExcept(',', ' ');
                    this.span = n > 0 ? this.span[n..] : ReadOnlySpan<char>.Empty;

                    return true;
                }
            }

            this.Current = this.span;
            this.span = ReadOnlySpan<char>.Empty;
            return true;
        }
    }
}

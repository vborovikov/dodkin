namespace Dodkin.Dispatch;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Parses and represents an assembly qualified type name.
/// </summary>
sealed class AssemblyQualifiedTypeName : IParsable<AssemblyQualifiedTypeName>, ISpanParsable<AssemblyQualifiedTypeName>
{
#pragma warning disable CS8618
    /// <summary>
    /// Creates a new instance of the <see cref="AssemblyQualifiedTypeName"/> class.
    /// </summary>
    private AssemblyQualifiedTypeName() { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new instance of the <see cref="AssemblyQualifiedTypeName"/> class with the specified values.
    /// </summary>
    /// <param name="fullName">The full name of the type.</param>
    /// <param name="assemblyName">The name of the assembly that the type is defined in.</param>
    /// <param name="version">The version of the assembly that the type is defined in.</param>
    /// <param name="culture">The culture of the assembly that the type is defined in.</param>
    /// <param name="publicKeyToken">The public key token of the assembly that the type is defined in.</param>
    /// <param name="genericParameters">The generic type parameters of the type, if it is a generic type.</param>
    private AssemblyQualifiedTypeName(string fullName, string assemblyName, string version, string culture, string publicKeyToken, AssemblyQualifiedTypeName[] genericParameters)
    {
        this.FullName = fullName;
        this.AssemblyName = assemblyName;
        this.Version = version;
        this.Culture = culture;
        this.PublicKeyToken = publicKeyToken;
        this.GenericParameters = genericParameters;
    }

    /// <summary>
    /// Gets the full name of the type.
    /// </summary>
    public string FullName { get; }
    /// <summary>
    /// Gets the name of the assembly that the type is defined in.
    /// </summary>
    public string AssemblyName { get; }
    /// <summary>
    /// Gets the version of the assembly that the type is defined in.
    /// </summary>
    public string Version { get; }
    /// <summary>
    /// Gets the culture of the assembly that the type is defined in.
    /// </summary>
    public string Culture { get; }
    /// <summary>
    /// Gets the public key token of the assembly that the type is defined in.
    /// </summary>
    public string PublicKeyToken { get; }
    /// <summary>
    /// Gets the generic type parameters of the type, if it is a generic type.
    /// </summary>
    public AssemblyQualifiedTypeName[] GenericParameters { get; }

    /// <summary>
    /// Tries to parse the specified assembly qualified type name.
    /// </summary>
    /// <param name="typeName">The assembly qualified type name to parse.</param>
    /// <param name="assemblyQualifiedTypeName">The parsed assembly qualified type name, if parsing succeeded.</param>
    /// <returns>True if the assembly qualified type name was successfully parsed, false otherwise.</returns>
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

    /// <summary>
    /// Parses the specified assembly qualified type name.
    /// </summary>
    /// <param name="typeName">The assembly qualified type name to parse.</param>
    /// <returns>The parsed assembly qualified type name.</returns>
    public static AssemblyQualifiedTypeName Parse(ReadOnlySpan<char> typeName)
    {
        if (!TryParse(typeName, out var assemblyQualifiedTypeName))
            throw new FormatException();

        return assemblyQualifiedTypeName;
    }

    private static TypeNamePartEnumerator EnumerateParts(ReadOnlySpan<char> typeName) => new(typeName);

    private static TypeNameEnumerator EnumerateTypeNames(ReadOnlySpan<char> typeNames) => new(typeNames);

    /// <inheritdoc />
    public static AssemblyQualifiedTypeName Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out AssemblyQualifiedTypeName result) =>
        TryParse(s.AsSpan(), provider, out result);

    /// <inheritdoc />
    public static AssemblyQualifiedTypeName Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        Parse(s);

    /// <inheritdoc />
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
        private PartEnumerator enumerator;
        private TypeNamePartKind kind;

        public TypeNamePartEnumerator(ReadOnlySpan<char> span)
        {
            this.enumerator = new(span);
            this.Current = default;
            this.kind = TypeNamePartKind.Unknown;
        }

        public TypeNamePart Current { get; private set; }

        public TypeNamePartEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (this.enumerator.MoveNext())
            {
                this.Current = new(this.enumerator.Current, ++this.kind);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Enumerates the type names respecting the square brackets.
    /// </summary>
    private ref struct TypeNameEnumerator
    {
        private PartEnumerator enumerator;

        public TypeNameEnumerator(ReadOnlySpan<char> span)
        {
            this.enumerator = new(
                span.IsEmpty || span.IsWhiteSpace() ? ReadOnlySpan<char>.Empty :
                span[0] == '[' && span[^1] == ']' ? span[1..^1] :
                span);
        }

        public ReadOnlySpan<char> Current => this.enumerator.Current;

        public TypeNameEnumerator GetEnumerator() => this;

        public bool MoveNext() => this.enumerator.MoveNext();
    }

    /// <summary>
    /// Enumerates comma separated parts of a type name.
    /// </summary>
    private ref struct PartEnumerator
    {
        private ReadOnlySpan<char> span;

        public PartEnumerator(ReadOnlySpan<char> span)
        {
            this.span = span;
        }

        public ReadOnlySpan<char> Current { get; private set; }

        public PartEnumerator GetEnumerator() => this;

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

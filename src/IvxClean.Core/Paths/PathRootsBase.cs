namespace IvxClean.Core.Paths;

/// <summary>
/// Shared token-expansion logic for <see cref="IPathRoots"/> implementations. Concrete
/// implementations supply the root values; this base turns tokenized templates into paths.
/// </summary>
public abstract class PathRootsBase : IPathRoots
{
    /// <inheritdoc />
    public abstract string Temp { get; }

    /// <inheritdoc />
    public abstract string LocalAppData { get; }

    /// <inheritdoc />
    public abstract string RoamingAppData { get; }

    /// <inheritdoc />
    public abstract string LocalLowAppData { get; }

    /// <inheritdoc />
    public abstract string ProgramData { get; }

    /// <inheritdoc />
    public abstract string Windows { get; }

    /// <inheritdoc />
    public abstract string SystemDrive { get; }

    /// <inheritdoc />
    public abstract string UserProfile { get; }

    /// <inheritdoc />
    public abstract string AppDataRoot { get; }

    /// <inheritdoc />
    public abstract string AppInstallDirectory { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> EachUserProfile { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> EachFixedDrive { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Expand(string tokenizedPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenizedPath);

        if (tokenizedPath[0] != '{')
        {
            // No leading token: a literal path, returned as-is.
            return [tokenizedPath];
        }

        int close = tokenizedPath.IndexOf('}', StringComparison.Ordinal);
        if (close < 0)
        {
            throw new InvalidOperationException($"Malformed path token in template: '{tokenizedPath}'.");
        }

        string token = tokenizedPath[1..close];
        string remainder = tokenizedPath[(close + 1)..].TrimStart('\\', '/');

        IReadOnlyList<string> bases = ResolveToken(token);
        if (remainder.Length == 0)
        {
            return bases;
        }

        var result = new List<string>(bases.Count);
        foreach (string b in bases)
        {
            result.Add(Path.Combine(b, remainder));
        }

        return result;
    }

    private IReadOnlyList<string> ResolveToken(string token) => token switch
    {
        "Temp" => [Temp],
        "LocalAppData" => [LocalAppData],
        "RoamingAppData" => [RoamingAppData],
        "LocalLowAppData" => [LocalLowAppData],
        "ProgramData" => [ProgramData],
        "Windows" => [Windows],
        "SystemDrive" => [SystemDrive],
        "UserProfile" => [UserProfile],
        "EachUserProfile" => EachUserProfile,
        "EachFixedDrive" => EachFixedDrive,
        _ => throw new InvalidOperationException($"Unknown path token '{{{token}}}'."),
    };
}

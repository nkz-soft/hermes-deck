namespace HermesDeck.Api.ContractTests;

/// <summary>
/// Resolves paths to contract artifacts under <c>specs/001-hermes-control-deck/contracts/</c>
/// and <c>proto/</c> robustly by walking up from the test run's base directory until the
/// repository root (identified by the presence of a <c>specs</c> directory) is found.
/// </summary>
internal static class ContractPaths
{
    private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

    public static string RepoRoot => RepoRootLazy.Value;

    public static string ContractsDirectory =>
        Path.Combine(RepoRoot, "specs", "001-hermes-control-deck", "contracts");

    public static string GetContractFile(string fileName) =>
        Path.Combine(ContractsDirectory, fileName);

    public static string ProtoFile => Path.Combine(RepoRoot, "proto", "agent-service.proto");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "specs", "001-hermes-control-deck", "contracts");
            if (Directory.Exists(candidate))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate repository root (a 'specs/001-hermes-control-deck/contracts' directory) " +
            $"by walking up from '{AppContext.BaseDirectory}'.");
    }
}

using System.Reflection;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Versioning;

public sealed class AssemblyVersionProvider : IVersionProvider
{
    public string DisplayVersion { get; }
    public int BuildNumber { get; }
    public string FullVersion { get; }

    public AssemblyVersionProvider()
    {
        var asm = Assembly.GetExecutingAssembly();

        // InformationalVersion is set to "v.0001" style in .csproj
        var infoVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        DisplayVersion = infoVersion ?? "v.0001";

        // Build number from AssemblyVersion (1.0.0.N — last component is our build counter)
        var assemblyVersion = asm.GetName().Version;
        BuildNumber = assemblyVersion?.Revision ?? 1;

        // Full version for diagnostics
        FullVersion = assemblyVersion?.ToString() ?? "1.0.0.1";
    }
}

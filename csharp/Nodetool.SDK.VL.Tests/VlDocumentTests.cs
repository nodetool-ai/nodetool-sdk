using NUnit.Framework;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;
using VL.TestFramework;

namespace Nodetool.SDK.VL.Tests;

[TestFixture]
[NonParallelizable]
public sealed class VlDocumentTests
{
    private const string SupportedVvvvVersionPrefix = "vvvv_gamma_7.1-";
    private TestEnvironment? _environment;
    private string _repoRoot = null!;
    private string _vvvvDirectory = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _repoRoot = FindRepoRoot(TestContext.CurrentContext.TestDirectory);
        var vvvvExe = FindVvvvExecutable();
        _vvvvDirectory = Path.GetDirectoryName(vvvvExe)!;
        var packageDirectory = Path.Combine(_repoRoot, "vvvv");

        AssemblyLoadContext.Default.Resolving += ResolveFromVvvv;
        _environment = TestEnvironmentLoader.Load(
            vvvvExe,
            new[] { packageDirectory },
            preCompilePackages: false);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _environment?.Dispose();
        _environment = null;
        AssemblyLoadContext.Default.Resolving -= ResolveFromVvvv;
    }

    [Test]
    public Task PrimaryDocumentCompilesWithoutErrors()
        => CompileDocumentAsync("vvvv/VL.Nodetool.vl");

    [Test]
    public Task HelpDocumentCompilesWithoutErrors()
        => CompileDocumentAsync("vvvv/help/Nodetool_Help.vl");

    [Test]
    public async Task PackagedDocumentCompilesFromIsolatedRepository()
    {
        var package = Directory
            .EnumerateFiles(
                Path.Combine(_repoRoot, "vvvv", "deployment", "out"),
                "VL.Nodetool.*.nupkg")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (package is null)
        {
            Assert.Ignore(
                "No packed VL.Nodetool package is available. Run " +
                "vvvv/deployment/pack-and-verify.ps1 first.");
            return;
        }

        var isolatedRoot = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            ".vl-nodetool-package");
        if (Directory.Exists(isolatedRoot))
            Directory.Delete(isolatedRoot, recursive: true);
        Directory.CreateDirectory(isolatedRoot);
        ZipFile.ExtractToDirectory(package, isolatedRoot);
        var documentPath = Path.Combine(
            isolatedRoot,
            "VL.Nodetool.vl");
        Assert.That(
            File.Exists(documentPath),
            Is.True,
            $"Package document is missing: {documentPath}");
        var document = XDocument.Load(documentPath);
        var factoryLocations = document
            .Descendants()
            .Where(element =>
                element.Name.LocalName == "NodeFactoryDependency")
            .Select(element => (string?)element.Attribute("Location"))
            .Where(location => !string.IsNullOrWhiteSpace(location))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.That(
            factoryLocations,
            Is.EqualTo(new[]
            {
                "Nodetool",
                "Nodetool.Nodes",
                "Nodetool.Workflows"
            }));

        using var environment = TestEnvironmentLoader.Load(
            Path.Combine(_vvvvDirectory, "vvvv.exe"),
            new[] { isolatedRoot },
            preCompilePackages: false);
        await environment.LoadAndTestAsync(
            documentPath,
            runEntryPoint: false);

        // Managed assemblies loaded by vvvv remain locked on Windows until
        // this test process exits. The next run removes this fixed directory
        // before extracting the new package, so artifacts do not accumulate.
    }

    private async Task CompileDocumentAsync(string relativePath)
    {
        var documentPath = Path.Combine(
            _repoRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.That(File.Exists(documentPath), Is.True, $"Missing VL document: {documentPath}");
        await _environment!.LoadAndTestAsync(documentPath, runEntryPoint: false);
    }

    private static string FindVvvvExecutable()
    {
        var explicitExe = Environment.GetEnvironmentVariable("VVVV_EXE");
        if (IsVvvvExecutable(explicitExe))
            return Path.GetFullPath(explicitExe!);

        var explicitHome = Environment.GetEnvironmentVariable("VVVV_HOME");
        if (!string.IsNullOrWhiteSpace(explicitHome))
        {
            var candidate = Path.Combine(explicitHome, "vvvv.exe");
            if (IsVvvvExecutable(candidate))
                return Path.GetFullPath(candidate);
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var installRoot = Path.Combine(programFiles, "vvvv");
        if (Directory.Exists(installRoot))
        {
            var candidate = Directory
                .EnumerateDirectories(installRoot, $"{SupportedVvvvVersionPrefix}*")
                .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => Path.Combine(path, "vvvv.exe"))
                .FirstOrDefault(IsVvvvExecutable);

            if (candidate is not null)
                return candidate;
        }

        throw new FileNotFoundException(
            "vvvv gamma 7.1 was not found. Set VVVV_EXE to the full vvvv.exe path " +
            "or VVVV_HOME to its installation directory.");
    }

    private static bool IsVvvvExecutable(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        File.Exists(path) &&
        string.Equals(Path.GetFileName(path), "vvvv.exe", StringComparison.OrdinalIgnoreCase);

    private Assembly? ResolveFromVvvv(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        var candidate = Path.Combine(_vvvvDirectory, $"{assemblyName.Name}.dll");
        return File.Exists(candidate)
            ? context.LoadFromAssemblyPath(candidate)
            : null;
    }

    private static string FindRepoRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            var gitMarker = Path.Combine(directory.FullName, ".git");
            if ((Directory.Exists(gitMarker) || File.Exists(gitMarker)) &&
                Directory.Exists(Path.Combine(directory.FullName, "vvvv")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find the nodetool-sdk repository above {startDirectory}.");
    }
}

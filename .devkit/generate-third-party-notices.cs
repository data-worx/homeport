using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

// Regenerates THIRD-PARTY-NOTICES.md from what one project actually publishes.
// Project-owned: devkit-sync never overwrites this file. Which project that is,
// is NOTICES_PROJECT in .devkit/gates.sh; an empty value switches the notices
// gate off, for a repository that ships no distributable build.
//
// A PackageReference scan cannot tell shipped packages from build-time-only
// ones, and neither can a plain `dotnet build`'s own deps.json: a design-time
// package such as Microsoft.EntityFrameworkCore.Design drags in Roslyn and
// MSBuild packages that `build` keeps around but a publish trims. Only
// `dotnet publish`'s deps.json is the ground truth for what a build actually
// ships, so this always publishes to a scratch directory and reads that.
// `nuget-license`, run pinned through `dotnet tool execute`, supplies the
// licence and copyright metadata for the packages deps.json names.
//
// The shape of the shipped build lives in the project file. The only input a
// publish takes on the command line is `-r` with one of the runtime
// identifiers listed there, so this publishes once for each of them - the
// self-contained platform builds - and once without one - the portable build.
// It passes nothing else that could change the resolved package graph, and it
// fails when two of those publishes ship different packages: one notices file
// is only true if it is true for every build.
//
// A self-contained publish also carries the .NET runtime, which deps.json
// names as runtime packs, one per shared framework and runtime identifier.
// Each pack ships its own MIT licence and a THIRD-PARTY-NOTICES file for the
// code inside it; both are read from the pack in the NuGet cache. A pack is
// listed by its major and minor version, without the runtime identifier: the
// patch is whichever the SDK on the build machine knows, so the file would
// otherwise change with every SDK update while saying nothing new, and the
// texts are compared across runtime identifiers so a difference still fails.
// ReadShippedPackages rejects any other deps.json library type it was not
// written for, so a new publish shape fails here - loudly - instead of
// silently leaving a licence out of a file that claims to be complete.
//
// Not everything shipped is a package. Fonts, icons and other plain files no
// deps.json names are listed by hand in the 'assets' section of the overrides
// file and rendered in a table of their own. Each entry names the files it
// covers, and a listed file that does not exist fails the run: a notice for
// something no longer shipped is as wrong as a missing one.
//
// The licence texts themselves are committed under .devkit/licenses/, one file
// per SPDX identifier, named exactly as nuget-license reports it. They are the
// canonical texts and are not edited, with one exception: MIT.txt carries a
// placeholder where the canonical template puts the copyright line, because
// one text covers several entries and each entry's own notice sits in the
// table above it. A shipped package or asset under a licence with no file
// there fails rather than producing a notices file that claims to reproduce a
// licence it does not carry.
//
// Usage: dotnet run --file .devkit/generate-third-party-notices.cs -- --project <csproj> [--output <path>]
//
// --output defaults to THIRD-PARTY-NOTICES.md at the repository root, i.e. this
// overwrites the real, committed file - the maintenance command to run by hand
// after a package change. `gates.sh` passes a temporary path instead, to check
// the committed file is still current without touching it.

internal static class Program
{
    private const string NoticesFileName = "THIRD-PARTY-NOTICES.md";
    private const string OverridesFileName = ".devkit/third-party-notices.overrides.json";
    private const string LicenseTextDirectory = ".devkit/licenses";
    private const string NotDeclared = "— not declared by the package";

    // Pinned, so the metadata a gate compares against does not change with a
    // tool release nobody chose.
    private const string NugetLicense = "nuget-license@4.0.18";

    // What deps.json puts in front of a runtime pack's package id.
    private const string RuntimePackPrefix = "runtimepack.";

    private static async Task<int> Main(string[] args)
    {
        // A maintenance command that fails says why in one line. A stack trace
        // through an async pipeline buries the sentence that names the problem.
        try
        {
            return await GenerateAsync(args);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task<int> GenerateAsync(string[] args)
    {
        if (!TryParseArgs(args, out var projectArg, out var outputArg, out var argumentError))
        {
            Console.Error.WriteLine(argumentError);
            return 1;
        }

        var repoRoot = (await RunAsync("git", "rev-parse", "--show-toplevel")).Trim();
        var outputPath = outputArg is null ? Path.Combine(repoRoot, NoticesFileName) : Path.GetFullPath(outputArg);
        Directory.SetCurrentDirectory(repoRoot);

        var project = new Project(projectArg!);
        if (!File.Exists(project.Path))
        {
            Console.Error.WriteLine($"--project names {project.Path}, which does not exist.");
            return 1;
        }

        var runtimeIdentifiers = (await RunAsync("dotnet", "msbuild", project.Path, "-getProperty:RuntimeIdentifiers"))
            .Trim()
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var targetFramework = (await RunAsync("dotnet", "msbuild", project.Path, "-getProperty:TargetFramework")).Trim();
        var assemblyName = (await RunAsync("dotnet", "msbuild", project.Path, "-getProperty:AssemblyName")).Trim();
        var packageRoot = ParseGlobalPackages(await RunAsync("dotnet", "nuget", "locals", "global-packages", "--list"));

        var publishDir = Directory.CreateTempSubdirectory("third-party-notices-");
        try
        {
            var problems = new List<string>();

            var portable = await PublishAsync(project, assemblyName, publishDir.FullName, targetFramework, runtimeIdentifier: null);
            var platforms = new List<(string RuntimeIdentifier, PublishContents Contents)>();
            foreach (var runtimeIdentifier in runtimeIdentifiers)
            {
                platforms.Add((runtimeIdentifier, await PublishAsync(project, assemblyName, publishDir.FullName, targetFramework, runtimeIdentifier)));
            }

            if (portable.RuntimePacks.Count > 0)
            {
                problems.Add("The publish without a runtime identifier carries the .NET runtime, although it is "
                    + "meant to be the portable, framework-dependent build; establish why before shipping it.");
            }

            foreach (var (runtimeIdentifier, contents) in platforms)
            {
                problems.AddRange(Differences("the portable build", portable.Packages, runtimeIdentifier, contents.Packages));
            }

            var runtimeNotices = ReadRuntimeNotices(packageRoot, platforms, problems);

            var shipped = portable.Packages;
            var metadata = await ReadLicenseMetadataAsync(project);
            var overrides = ReadCopyrightOverrides();
            var assets = ReadAssets();

            foreach (var file in assets.SelectMany(asset => asset.Files).Where(file => !File.Exists(file)))
            {
                problems.Add($"{OverridesFileName} lists the asset file {file}, which does not exist.");
            }

            foreach (var package in shipped.Where(package => !metadata.ContainsKey(package)))
            {
                problems.Add($"No licence metadata for {package.Id} {package.Version}.");
            }

            foreach (var package in shipped.Where(package => metadata.TryGetValue(package, out var notice) && notice.License.Length == 0))
            {
                problems.Add(
                    $"{package.Id} {package.Version} declares no licence. A package whose licence is unknown "
                    + "cannot be shipped under it: establish the licence and feed it in with nuget-license's "
                    + "-override option, or drop the package.");
            }

            foreach (var id in overrides.Keys.Where(id => shipped.All(package => package.Id != id)))
            {
                problems.Add($"{OverridesFileName} overrides the copyright of {id}, which {project.Name} does not ship.");
            }

            var rows = shipped
                .Where(metadata.ContainsKey)
                .Select(package => metadata[package])
                .Select(notice => notice with
                {
                    Copyright = overrides.TryGetValue(notice.Id, out var recovered) ? recovered : notice.Copyright,
                })
                .Concat(runtimeNotices.Select(runtime => new PackageNotice(
                    runtime.Id, runtime.Version, runtime.License, runtime.Copyright)))
                .OrderBy(notice => notice.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var licenseTexts = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var licenses = rows.Select(row => row.License).Concat(assets.Select(asset => asset.License));
            foreach (var license in licenses.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var path = LicenseTextPath(license);
                if (path is null || !File.Exists(path))
                {
                    problems.Add(
                        $"No licence text for '{license}'. Add it as {LicenseTextDirectory}/{license}.txt: the "
                        + "notices file states that it reproduces every licence it names, and MIT and Apache-2.0 "
                        + "both require the text to travel with the software.");
                    continue;
                }

                licenseTexts[license] = File.ReadAllText(path).ReplaceLineEndings("\n").TrimEnd('\n');
            }

            if (problems.Count > 0)
            {
                problems.ForEach(Console.Error.WriteLine);
                return 1;
            }

            // Written through a StreamWriter with an explicit NewLine, because the
            // default is Environment.NewLine and that is CRLF on Windows, while
            // .gitattributes normalises every text file to LF. The committed file
            // would then differ from the generated one on a fresh clone, and the
            // notices gate would be red with a whole-file diff and no hint why.
            using (var writer = new StreamWriter(outputPath) { NewLine = "\n" })
            {
                Render(writer, project, rows, assets, runtimeNotices, licenseTexts);
            }

            Console.WriteLine(
                $"Wrote {rows.Count} packages, {assets.Count} assets and {licenseTexts.Count} licence texts, "
                + $"checked against {runtimeIdentifiers.Length + 1} publishes, to {outputPath}.");
            return 0;
        }
        finally
        {
            publishDir.Delete(recursive: true);
        }
    }

    private static bool TryParseArgs(string[] args, out string? project, out string? output, out string? error)
    {
        project = null;
        output = null;
        error = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--project" or "--output" when i + 1 == args.Length:
                    error = $"{args[i]} needs a path.";
                    return false;
                case "--project":
                    project = args[++i];
                    break;
                case "--output":
                    output = args[++i];
                    break;
                default:
                    error = $"Unknown argument: {args[i]}";
                    return false;
            }
        }

        if (project is null)
        {
            error = "--project is required: the project file whose publish is shipped, as NOTICES_PROJECT in "
                + ".devkit/gates.sh names it.";
            return false;
        }

        return true;
    }

    // A publish writes its deps.json beside the output - except a single-file
    // publish, which produces one only in the intermediate output. Switching the
    // single file off is not an option - it would be a shape passed on the
    // command line - so in that case the deps.json is read from obj/, where the
    // publish that just ran left it.
    private static async Task<PublishContents> PublishAsync(
        Project project, string assemblyName, string root, string targetFramework, string? runtimeIdentifier)
    {
        var output = Path.Combine(root, runtimeIdentifier ?? "portable");
        var arguments = new List<string> { "publish", project.Path, "-c", "Release", "-o", output, "--nologo" };
        if (runtimeIdentifier is not null)
        {
            arguments.AddRange(["-r", runtimeIdentifier]);
        }

        await RunAsync("dotnet", [.. arguments]);

        var depsJsonName = assemblyName + ".deps.json";
        var depsJson = Path.Combine(output, depsJsonName);
        if (!File.Exists(depsJson) && runtimeIdentifier is not null)
        {
            depsJson = Path.Combine(project.Directory, "obj", "Release", targetFramework, runtimeIdentifier, depsJsonName);
        }

        return ReadShippedPackages(depsJson);
    }

    // Accepts only the library types a publish produces - packages, and the
    // runtime packs a self-contained publish adds - and rejects every other one
    // rather than skipping what it does not know: its licence would otherwise go
    // unnamed with nothing saying so.
    private static PublishContents ReadShippedPackages(string depsJsonPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(depsJsonPath));

        var packages = new List<PackageIdentity>();
        var runtimePacks = new List<PackageIdentity>();
        foreach (var library in document.RootElement.GetProperty("libraries").EnumerateObject())
        {
            var type = library.Value.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;
            if (type == "project")
            {
                continue;
            }

            if (type is not ("package" or "runtimepack"))
            {
                throw new InvalidOperationException(
                    $"deps.json library '{library.Name}' has the unhandled type '{type ?? "(none)"}'. The publish "
                    + "shape changed; establish what that type ships, and whose licence covers it, before "
                    + "extending this generator.");
            }

            var separator = library.Name.LastIndexOf('/');
            if (separator < 0)
            {
                throw new InvalidOperationException(
                    $"deps.json library '{library.Name}' is not in the expected '<id>/<version>' form.");
            }

            var identity = new PackageIdentity(library.Name[..separator], library.Name[(separator + 1)..]);
            if (type == "package")
            {
                packages.Add(identity);
            }
            else
            {
                runtimePacks.Add(identity with { Id = identity.Id[RuntimePackPrefix.Length..] });
            }
        }

        return new PublishContents(packages, runtimePacks);
    }

    private static IEnumerable<string> Differences(
        string expectedName,
        IReadOnlyList<PackageIdentity> expected,
        string actualName,
        IReadOnlyList<PackageIdentity> actual)
    {
        foreach (var package in actual.Except(expected))
        {
            yield return $"The {actualName} build ships {package.Id} {package.Version}, {expectedName} does not. "
                + "One notices file covers every build, so every build has to ship the same packages.";
        }

        foreach (var package in expected.Except(actual))
        {
            yield return $"{expectedName} ships {package.Id} {package.Version}, the {actualName} build does not. "
                + "One notices file covers every build, so every build has to ship the same packages.";
        }
    }

    // One notice per shared framework, read from the runtime pack of every
    // runtime identifier and required to be the same in all of them. The licence
    // must be MIT and its copyright line is taken from the pack's own licence
    // file, so nothing about the runtime is written down by hand.
    private static IReadOnlyList<RuntimeNotice> ReadRuntimeNotices(
        string packageRoot,
        IReadOnlyList<(string RuntimeIdentifier, PublishContents Contents)> platforms,
        List<string> problems)
    {
        var notices = new Dictionary<string, (string RuntimeIdentifier, RuntimeNotice Notice)>();
        foreach (var (runtimeIdentifier, contents) in platforms)
        {
            if (contents.RuntimePacks.Count == 0)
            {
                problems.Add($"The {runtimeIdentifier} build carries no .NET runtime, although it is meant to be self-contained.");
            }

            foreach (var pack in contents.RuntimePacks)
            {
                var suffix = "." + runtimeIdentifier;
                if (!pack.Id.EndsWith(suffix, StringComparison.Ordinal))
                {
                    problems.Add($"The runtime pack {pack.Id} of the {runtimeIdentifier} build does not end in {suffix}.");
                    continue;
                }

                var id = pack.Id[..^suffix.Length];
                var directory = Path.Combine(packageRoot, pack.Id.ToLowerInvariant(), pack.Version);
                var license = ReadPackFile(directory, "LICENSE.TXT").Split('\n');
                var copyright = license.FirstOrDefault(line => line.StartsWith("Copyright", StringComparison.Ordinal));
                if (license[0].Trim() != "The MIT License (MIT)" || copyright is null)
                {
                    problems.Add($"The licence of {pack.Id} {pack.Version} is not the MIT licence with a copyright "
                        + "line this generator expects; establish what it is before shipping it.");
                    continue;
                }

                var version = string.Join('.', pack.Version.Split('.').Take(2));
                var notice = new RuntimeNotice(id, version, "MIT", copyright.Trim(), ReadPackFile(directory, "THIRD-PARTY-NOTICES.TXT"));

                if (!notices.TryGetValue(id, out var first))
                {
                    notices[id] = (runtimeIdentifier, notice);
                }
                else if (first.Notice != notice)
                {
                    problems.Add($"The {id} notices of the {runtimeIdentifier} build differ from those of the "
                        + $"{first.RuntimeIdentifier} build. One notices file covers every build.");
                }
            }
        }

        return [.. notices.Values.Select(entry => entry.Notice).OrderBy(notice => notice.Id, StringComparer.Ordinal)];
    }

    // The file names differ in case between packs (LICENSE.TXT, LICENSE.txt),
    // and the NuGet cache sits on a case-sensitive file system on Linux.
    private static string ReadPackFile(string directory, string name)
    {
        var path = Directory.EnumerateFiles(directory)
            .FirstOrDefault(file => string.Equals(Path.GetFileName(file), name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"{directory} has no {name}.");

        return File.ReadAllText(path).ReplaceLineEndings("\n").TrimEnd('\n');
    }

    private static string ParseGlobalPackages(string output)
    {
        const string Prefix = "global-packages:";
        var line = output.Split('\n').Select(entry => entry.Trim()).FirstOrDefault(entry => entry.StartsWith(Prefix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"'dotnet nuget locals' named no global packages folder:{Environment.NewLine}{output}");

        return line[Prefix.Length..].Trim();
    }

    private static async Task<Dictionary<PackageIdentity, PackageNotice>> ReadLicenseMetadataAsync(Project project)
    {
        var json = await RunAsync(
            "dotnet", "tool", "execute", NugetLicense, "--yes", "--", "-i", project.Path, "-t", "-o", "Json");

        using var document = JsonDocument.Parse(json);

        var metadata = new Dictionary<PackageIdentity, PackageNotice>();
        foreach (var entry in document.RootElement.EnumerateArray())
        {
            var identity = new PackageIdentity(
                entry.GetProperty("PackageId").GetString()!,
                entry.GetProperty("PackageVersion").GetString()!);

            metadata[identity] = new PackageNotice(
                identity.Id,
                identity.Version,
                ReadString(entry, "License") ?? "",
                ReadString(entry, "Copyright") ?? NotDeclared);
        }

        return metadata;
    }

    // Copyright notices for packages that declare none. Deliberately not derived
    // from <authors>: a notice assembled out of a field that is not one reads
    // exactly like a real one, and this file exists to reproduce the real one.
    private static Dictionary<string, string> ReadCopyrightOverrides()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(OverridesFileName));

        return document.RootElement.GetProperty("copyright").EnumerateObject()
            .ToDictionary(entry => entry.Name, entry => entry.Value.GetProperty("notice").GetString()!);
    }

    private static IReadOnlyList<AssetNotice> ReadAssets()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(OverridesFileName));

        return document.RootElement.GetProperty("assets").EnumerateArray()
            .Select(entry => new AssetNotice(
                entry.GetProperty("name").GetString()!,
                entry.GetProperty("version").GetString()!,
                entry.GetProperty("license").GetString()!,
                entry.GetProperty("copyright").GetString()!,
                entry.GetProperty("files").EnumerateArray().Select(file => file.GetString()!).ToList()))
            .OrderBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? LicenseTextPath(string license) =>
        license.Length == 0 || license.AsSpan().IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            ? null
            : Path.Combine(LicenseTextDirectory, license + ".txt");

    private static void Render(
        TextWriter writer,
        Project project,
        IReadOnlyList<PackageNotice> rows,
        IReadOnlyList<AssetNotice> assets,
        IReadOnlyList<RuntimeNotice> runtimeNotices,
        IReadOnlyDictionary<string, string> licenseTexts)
    {
        writer.WriteLine("# Third-party notices");
        writer.WriteLine();
        writer.WriteLine($"{project.Name} ships the third-party packages{(assets.Count > 0 ? " and assets" : "")} listed below. The");
        writer.WriteLine("tables name the licence each one is used under and the copyright notice it");
        writer.WriteLine("carries; the full text of every licence named is reproduced further down, as");
        writer.WriteLine("those licences require.");
        writer.WriteLine();
        writer.WriteLine("This file is generated — see `.devkit/generate-third-party-notices.cs` — and");
        writer.WriteLine("ships next to what `dotnet publish` produces. Regenerate it after a package or");
        writer.WriteLine("asset change; the `notices` gate fails while it is stale.");
        writer.WriteLine();
        writer.WriteLine("## Packages");
        writer.WriteLine();
        writer.WriteLine("| Package | Version | Licence | Copyright |");
        writer.WriteLine("|---|---|---|---|");
        foreach (var row in rows)
        {
            writer.WriteLine($"| {row.Id} | {row.Version} | {row.License} | {Cell(row.Copyright)} |");
        }

        if (rows.Any(row => row.Copyright == NotDeclared))
        {
            writer.WriteLine();
            writer.WriteLine($"A copyright reading \"{NotDeclared}\" is declared neither in that package's own");
            writer.WriteLine("metadata nor in a licence file inside it, and no notice for it has been found");
            writer.WriteLine("elsewhere yet. The gap is deliberate: an approximation a reader cannot tell");
            writer.WriteLine("apart from a real notice is worse than a missing one. Notices recovered from a");
            writer.WriteLine($"project's own repository are recorded in `{OverridesFileName}`, each");
            writer.WriteLine("with the source it was copied from.");
        }

        if (runtimeNotices.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("The `.App.Runtime` rows are the .NET runtime, which the builds for a platform");
            writer.WriteLine("carry with them. They are listed by major and minor version: a build carries");
            writer.WriteLine("the latest patch of that runtime the SDK it was built with knows.");
        }

        if (assets.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("## Assets");
            writer.WriteLine();
            writer.WriteLine("Files shipped as they are rather than as packages. Each one's own licence file");
            writer.WriteLine("ships beside it as well.");
            writer.WriteLine();
            writer.WriteLine("| Asset | Version | Licence | Copyright |");
            writer.WriteLine("|---|---|---|---|");
            foreach (var asset in assets)
            {
                writer.WriteLine($"| {asset.Name} | {asset.Version} | {asset.License} | {Cell(asset.Copyright)} |");
            }
        }

        if (runtimeNotices.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("## .NET runtime notices");
            writer.WriteLine();
            writer.WriteLine("The .NET runtime contains code from third parties of its own. Each runtime pack");
            writer.WriteLine("ships the notices for that code, reproduced here as they come.");

            foreach (var runtime in runtimeNotices)
            {
                writer.WriteLine();
                writer.WriteLine($"### {runtime.Id}");
                writer.WriteLine();
                writer.WriteLine("```text");
                foreach (var line in runtime.ThirdPartyNotices.Split('\n'))
                {
                    writer.WriteLine(line);
                }

                writer.WriteLine("```");
            }
        }

        writer.WriteLine();
        writer.WriteLine("## Licence texts");

        foreach (var (license, text) in licenseTexts)
        {
            writer.WriteLine();
            writer.WriteLine($"### {license}");
            writer.WriteLine();
            writer.WriteLine($"Applies to every entry marked `{license}` in the tables above. Each of them");
            writer.WriteLine("keeps its own copyright notice, as given in those tables.");
            writer.WriteLine();
            writer.WriteLine("```text");
            foreach (var line in text.Split('\n'))
            {
                writer.WriteLine(line);
            }

            writer.WriteLine("```");
        }
    }

    // A pipe inside a value ends the table cell, so one copyright line carrying
    // one would silently break the row it sits in.
    private static string Cell(string value) => value.Replace("|", "\\|");

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;

    // Both streams are read concurrently: reading stdout to the end first
    // deadlocks as soon as the child fills the stderr pipe while this process is
    // still blocked on stdout.
    private static async Task<string> RunAsync(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdout, stderr);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{fileName} {string.Join(' ', arguments)}' exited with {process.ExitCode}."
                + $"{Environment.NewLine}{stdout.Result}{Environment.NewLine}{stderr.Result}");
        }

        Console.Error.Write(stderr.Result);
        return stdout.Result;
    }

    // The project whose publish is shipped, as the repository root sees it.
    private sealed record Project(string Path)
    {
        public string Directory => System.IO.Path.GetDirectoryName(Path)!;

        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);
    }

    private sealed record PackageIdentity(string Id, string Version);

    private sealed record PublishContents(IReadOnlyList<PackageIdentity> Packages, IReadOnlyList<PackageIdentity> RuntimePacks);

    private sealed record RuntimeNotice(string Id, string Version, string License, string Copyright, string ThirdPartyNotices);

    private sealed record PackageNotice(string Id, string Version, string License, string Copyright);

    private sealed record AssetNotice(
        string Name,
        string Version,
        string License,
        string Copyright,
        IReadOnlyList<string> Files);
}

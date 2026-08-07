using System.Xml.Linq;

namespace EbookReader.Architecture.Tests;

public sealed class ArchitectureContractTests
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["EbookReader.Domain"] = [],
            ["EbookReader.Epub"] = ["EbookReader.Domain"],
            ["EbookReader.Application"] = ["EbookReader.Domain"],
            ["EbookReader.Layout"] = ["EbookReader.Domain"],
            ["EbookReader.Cli"] =
            [
                "EbookReader.Application",
                "EbookReader.Domain",
                "EbookReader.Epub",
                "EbookReader.Layout",
            ],
        };

    [Fact]
    public void ProductionProjectsFollowTheDependencyAllowlist()
    {
        string root = RepositoryRoot.Find();

        foreach ((string projectName, string[] expectedReferences) in AllowedProjectReferences)
        {
            string projectFile = Path.Combine(root, "src", projectName, $"{projectName}.csproj");
            XDocument document = XDocument.Load(projectFile);

            string[] actualReferences = document
                .Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFileNameWithoutExtension(value!)!)
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expectedReferences.Order(StringComparer.Ordinal), actualReferences);
        }
    }

    [Fact]
    public void OnlyCliReferencesTerminalGui()
    {
        string root = RepositoryRoot.Find();
        string[] owners = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(ProjectReferencesTerminalGui)
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["EbookReader.Cli"], owners);
    }

    [Fact]
    public void ProductionProjectsDoNotPinPackageVersionsLocally()
    {
        string root = RepositoryRoot.Find();

        foreach (string projectFile in Directory.EnumerateFiles(
                     Path.Combine(root, "src"),
                     "*.csproj",
                     SearchOption.AllDirectories))
        {
            XDocument document = XDocument.Load(projectFile);
            XElement[] locallyVersionedPackages = document
                .Descendants("PackageReference")
                .Where(element => element.Attribute("Version") is not null)
                .ToArray();

            Assert.Empty(locallyVersionedPackages);
        }
    }

    [Fact]
    public void CentralPackagesPinTheFoundationVersions()
    {
        string root = RepositoryRoot.Find();
        XDocument document = XDocument.Load(Path.Combine(root, "Directory.Packages.props"));

        Dictionary<string, string> versions = document
            .Descendants("PackageVersion")
            .ToDictionary(
                element => element.Attribute("Include")!.Value,
                element => element.Attribute("Version")!.Value,
                StringComparer.OrdinalIgnoreCase);

        Assert.Equal("2.4.17", versions["Terminal.Gui"]);
        Assert.Equal("1.7.1", versions["AngleSharp"]);
        Assert.Equal("3.2.2", versions["xunit.v3.mtp-v2"]);
    }

    [Fact]
    public void ValidationScriptsUseMtpSolutionSelector()
    {
        string root = RepositoryRoot.Find();
        string windowsScript = File.ReadAllText(Path.Combine(root, "validate.cmd"));
        string posixScript = File.ReadAllText(Path.Combine(root, "validate.sh"));
        const string expectedCommand = "dotnet test --solution EbookReader.sln -c Release --no-build";

        Assert.Contains(expectedCommand, windowsScript);
        Assert.Contains(expectedCommand, posixScript);
        Assert.DoesNotContain("dotnet test EbookReader.sln", windowsScript);
        Assert.DoesNotContain("dotnet test EbookReader.sln", posixScript);
    }

    [Fact]
    public void SharedBuildContractTargetsNet10AndCSharp14()
    {
        string root = RepositoryRoot.Find();
        XDocument document = XDocument.Load(Path.Combine(root, "Directory.Build.props"));

        Assert.Equal("net10.0", document.Descendants("TargetFramework").Single().Value);
        Assert.Equal("14.0", document.Descendants("LangVersion").Single().Value);
        Assert.Equal("enable", document.Descendants("Nullable").Single().Value);
        Assert.Equal("true", document.Descendants("TreatWarningsAsErrors").Single().Value);
    }

    [Fact]
    public void DomainProjectHasNoPackageReferences()
    {
        string root = RepositoryRoot.Find();
        string projectFile = Path.Combine(root, "src", "EbookReader.Domain", "EbookReader.Domain.csproj");
        XDocument document = XDocument.Load(projectFile);

        Assert.Empty(document.Descendants("PackageReference"));
    }

    [Fact]
    public void DomainSourceDoesNotMentionFormatOrUiTechnologies()
    {
        string root = RepositoryRoot.Find();
        string domainDirectory = Path.Combine(root, "src", "EbookReader.Domain");
        string[] forbiddenTerms = ["Epub", "AngleSharp", "Terminal.Gui", "System.Xml", "System.IO.Compression"];

        foreach (string sourceFile in Directory.EnumerateFiles(domainDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, source, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void OnlyEpubAdapterReferencesAngleSharp()
    {
        string root = RepositoryRoot.Find();
        string[] owners = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(ProjectReferencesAngleSharp)
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["EbookReader.Epub"], owners);
    }

    [Fact]
    public void AngleSharpTypesStayInsideEpubContentBoundary()
    {
        string root = RepositoryRoot.Find();
        string epubDirectory = Path.Combine(root, "src", "EbookReader.Epub");

        foreach (string sourceFile in Directory.EnumerateFiles(epubDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            if (!source.Contains("AngleSharp", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.Contains(
                Path.Combine("EbookReader.Epub", "Content"),
                sourceFile,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EpubNavigationIntermediateModelDoesNotReferenceDomainTypes()
    {
        string root = RepositoryRoot.Find();
        string navigationDirectory = Path.Combine(root, "src", "EbookReader.Epub", "Navigation");

        foreach (string sourceFile in Directory.EnumerateFiles(navigationDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("EbookReader.Domain", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EpubAdapterDoesNotExtractArchivesToFilesystem()
    {
        string root = RepositoryRoot.Find();
        string epubDirectory = Path.Combine(root, "src", "EbookReader.Epub");

        foreach (string sourceFile in Directory.EnumerateFiles(epubDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("ExtractToDirectory", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ExtractToFile", source, StringComparison.Ordinal);
        }
    }


    [Fact]
    public void CliProjectProducesEreaderAssembly()
    {
        string root = RepositoryRoot.Find();
        XDocument document = XDocument.Load(Path.Combine(root, "src", "EbookReader.Cli", "EbookReader.Cli.csproj"));

        Assert.Equal("ereader", document.Descendants("AssemblyName").Single().Value);
    }

    [Fact]
    public void M10ConsoleProjectionDoesNotImplementViewportLayout()
    {
        string root = RepositoryRoot.Find();
        string readingDirectory = Path.Combine(root, "src", "EbookReader.Cli", "Reading");
        string[] forbiddenTerms = ["Console.WindowWidth", "Console.WindowHeight", "Terminal.Gui", "EbookReader.Epub"];

        foreach (string sourceFile in Directory.EnumerateFiles(readingDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void M11LayoutEngineIsTerminalAndFormatIndependent()
    {
        string root = RepositoryRoot.Find();
        string layoutDirectory = Path.Combine(root, "src", "EbookReader.Layout");
        string[] forbiddenTerms =
        [
            "Terminal.Gui",
            "Console.WindowWidth",
            "Console.WindowHeight",
            "EbookReader.Epub",
            "AngleSharp",
        ];

        foreach (string sourceFile in Directory.EnumerateFiles(layoutDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void M12LogicalReadingStateDoesNotDependOnEphemeralLayoutCoordinates()
    {
        string root = RepositoryRoot.Find();
        string[] directories =
        [
            Path.Combine(root, "src", "EbookReader.Domain"),
            Path.Combine(root, "src", "EbookReader.Application"),
        ];
        string[] forbiddenTerms = ["LayoutPosition", "LayoutPage", "PageNumber"];

        foreach (string directory in directories)
        {
            foreach (string sourceFile in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(sourceFile);
                foreach (string forbiddenTerm in forbiddenTerms)
                {
                    Assert.DoesNotContain(forbiddenTerm, source, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void GraphifyGeneratedOutputIsExcludedFromSourceBaseline()
    {
        string root = RepositoryRoot.Find();
        string gitIgnore = File.ReadAllText(Path.Combine(root, ".gitignore"));

        Assert.Contains("/graphify-out/", gitIgnore, StringComparison.Ordinal);
    }

    [Fact]
    public void ProtectionInspectionContainsNoCryptographicImplementation()
    {
        string root = RepositoryRoot.Find();
        string validationDirectory = Path.Combine(root, "src", "EbookReader.Epub", "Validation");

        foreach (string sourceFile in Directory.EnumerateFiles(validationDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("System.Security.Cryptography", source, StringComparison.Ordinal);
            Assert.DoesNotContain("CryptoStream", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void M12FlowWordArrayUsesLengthInsteadOfCountMethodGroup()
    {
        string root = RepositoryRoot.Find();
        string layoutEngine = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Layout",
            "DeterministicLayoutEngine.cs"));

        Assert.DoesNotContain("word.Elements.Count", layoutEngine, StringComparison.Ordinal);
        Assert.Contains("word.Elements.Length", layoutEngine, StringComparison.Ordinal);
    }

    [Fact]
    public void M13ReaderSessionDoesNotDependOnTerminalGuiOrEpub()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderSession.cs"));

        Assert.DoesNotContain("Terminal.Gui", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EbookReader.Epub", source, StringComparison.Ordinal);
    }

    [Fact]
    public void M13ReaderWindowRemainsAThinTerminalGuiAdapter()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));
        string[] forbiddenTerms =
        [
            "DeterministicLayoutEngine",
            "LayoutNavigator",
            "LogicalReadingNavigator",
            "EbookReader.Epub",
        ];

        foreach (string forbiddenTerm in forbiddenTerms)
        {
            Assert.DoesNotContain(forbiddenTerm, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void M13ValidationSmokeUsesExplicitPlainMode()
    {
        string root = RepositoryRoot.Find();
        string windowsScript = File.ReadAllText(Path.Combine(root, "validate.cmd"));
        string posixScript = File.ReadAllText(Path.Combine(root, "validate.sh"));

        Assert.Contains("--plain test-books\\m1.0-smoke.epub", windowsScript, StringComparison.Ordinal);
        Assert.Contains("--plain test-books/m1.0-smoke.epub", posixScript, StringComparison.Ordinal);
    }

    [Fact]
    public void M13TerminalGuiApplicationFactoryIsGloballyQualified()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "TerminalGuiReaderHost.cs"));

        Assert.Contains(
            "global::Terminal.Gui.App.Application.Create()",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "= Application.Create()",
            source,
            StringComparison.Ordinal);
    }


    [Fact]
    public void M14ReaderWindowReflowsFromTerminalGuiBodyViewport()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));

        Assert.Contains("_body.ViewportChanged += (_, _) => SynchronizeViewport();", source, StringComparison.Ordinal);
        Assert.Contains("SynchronizeViewport();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LayoutComplete +=", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OnDrawingContent", source, StringComparison.Ordinal);
        Assert.Contains("_body.Viewport.Width", source, StringComparison.Ordinal);
        Assert.Contains("_body.Viewport.Height", source, StringComparison.Ordinal);
        Assert.Contains("_session.Reflow(viewport)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.WindowWidth", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.WindowHeight", source, StringComparison.Ordinal);
    }

    [Fact]
    public void M14ReaderSessionReflowDoesNotReplaceLogicalLocationFromLayoutCoordinates()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderSession.cs"));

        Assert.Contains("ReadingLocation logicalLocation = Location;", source, StringComparison.Ordinal);
        Assert.Contains("Layout = DeterministicLayoutEngine.Layout(_book, viewport);", source, StringComparison.Ordinal);
        Assert.Contains("Location = logicalLocation;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Location = Position", source, StringComparison.Ordinal);
    }



    [Fact]
    public void M20Hotfix1ReaderWindowUsesSlidingViewportForLineNavigation()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));

        Assert.Contains("_body.ShowReaderLines(_session.GetCurrentViewportLines());", source, StringComparison.Ordinal);
        Assert.Contains("key == Key.CursorDown || IsCharacter(key, 'j')", source, StringComparison.Ordinal);
        Assert.Contains("key == Key.CursorUp || IsCharacter(key, 'k')", source, StringComparison.Ordinal);
    }

    [Fact]
    public void M20Hotfix2ReaderWindowSeparatesHeaderBodyAndFooter()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));

        Assert.Contains("private readonly Label _headerSeparator;", source, StringComparison.Ordinal);
        Assert.Contains("private readonly Label _footerSeparator;", source, StringComparison.Ordinal);
        Assert.Contains("Y = 2,", source, StringComparison.Ordinal);
        Assert.Contains("Height = Dim.Fill(2)", source, StringComparison.Ordinal);
        Assert.Contains("Y = Pos.AnchorEnd(2)", source, StringComparison.Ordinal);
        Assert.Contains("Text = HorizontalRule", source, StringComparison.Ordinal);
        Assert.Contains("Add(_header, _headerSeparator, _body, _footerSeparator, _footer);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void M20PersistedStateContainsOnlyLogicalCoordinates()
    {
        string root = RepositoryRoot.Find();
        string stateDirectory = Path.Combine(root, "src", "EbookReader.Application", "State");
        string[] forbiddenTerms = ["PageNumber", "LineIndex", "LayoutPosition", "LayoutPage", "BookLayout"];

        foreach (string sourceFile in Directory.EnumerateFiles(stateDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void M20JsonStoreUsesSameDirectoryTemporaryFileAndAtomicMove()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Application",
            "State",
            "JsonReadingStateStore.cs"));

        Assert.Contains("Guid.NewGuid():N}.tmp", source, StringComparison.Ordinal);
        Assert.Contains("stream.Flush(flushToDisk: true);", source, StringComparison.Ordinal);
        Assert.Contains("File.Move(temporaryPath, FilePath, overwrite: true);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("File.Copy", source, StringComparison.Ordinal);
    }

    [Fact]
    public void M20ReaderWindowDoesNotOwnPersistence()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));

        Assert.DoesNotContain("JsonReadingStateStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("state.json", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Text.Json", source, StringComparison.Ordinal);
    }

    [Fact]
    public void M20PlainModeRemainsStateless()
    {
        string root = RepositoryRoot.Find();
        string source = File.ReadAllText(Path.Combine(root, "src", "EbookReader.Cli", "CliEntryPoint.cs"));
        int methodStart = source.IndexOf("private static int ReadValidBook", StringComparison.Ordinal);
        int methodEnd = source.IndexOf("private static JsonReadingStateStore? TryCreateStateStore", StringComparison.Ordinal);
        string method = source[methodStart..methodEnd];
        int plainBranch = method.IndexOf("if (!interactive)", StringComparison.Ordinal);
        int stateStoreUse = method.IndexOf("JsonReadingStateStore? store", StringComparison.Ordinal);

        Assert.True(methodStart >= 0);
        Assert.True(methodEnd > methodStart);
        Assert.True(plainBranch >= 0);
        Assert.True(stateStoreUse > plainBranch);
        Assert.Contains("BookConsoleRenderer.Write(book, output);", method, StringComparison.Ordinal);
    }


    [Fact]
    public void M21InteractiveTocUsesDomainLocationsWithoutTerminalGuiCollectionWidgets()
    {
        string root = RepositoryRoot.Find();
        string session = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderSession.cs"));
        string window = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));

        Assert.Contains("FlattenTableOfContents", session, StringComparison.Ordinal);
        Assert.Contains("ReadingLocation? Target", File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderTocEntry.cs")), StringComparison.Ordinal);
        Assert.Contains("key == Key.Tab || IsCharacter(key, 't')", window, StringComparison.Ordinal);
        Assert.Contains("_session.NavigateToTocEntry(_tocSelectedIndex)", window, StringComparison.Ordinal);
        Assert.Contains("BuildToc()", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ListView", window, StringComparison.Ordinal);
        Assert.DoesNotContain("TreeView", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Dialog", window, StringComparison.Ordinal);
        Assert.DoesNotContain("EbookReader.Epub", window, StringComparison.Ordinal);
    }

    [Fact]
    public void M22MetadataViewUsesFormatNeutralProjectionWithoutEpubTypes()
    {
        string root = RepositoryRoot.Find();
        string session = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderSession.cs"));
        string window = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));
        string formatter = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderMetadataFormatter.cs"));

        Assert.Contains("BuildMetadataEntries(book)", session, StringComparison.Ordinal);
        Assert.Contains("IsCharacter(key, 'm')", window, StringComparison.Ordinal);
        Assert.Contains("BuildMetadata()", window, StringComparison.Ordinal);
        Assert.Contains("ReaderMetadataFormatter.Format", window, StringComparison.Ordinal);
        Assert.Contains("TerminalCellWidth.Measure", formatter, StringComparison.Ordinal);
        Assert.DoesNotContain("EbookReader.Epub", session, StringComparison.Ordinal);
        Assert.DoesNotContain("EbookReader.Epub", window, StringComparison.Ordinal);
        Assert.DoesNotContain("EbookReader.Epub", formatter, StringComparison.Ordinal);
        Assert.DoesNotContain("EpubPackage", session, StringComparison.Ordinal);
        Assert.DoesNotContain("EpubPackage", window, StringComparison.Ordinal);
    }

    [Fact]
    public void M23SearchLivesInApplicationAndDoesNotDependOnLayoutTerminalGuiOrEpub()
    {
        string root = RepositoryRoot.Find();
        string searchDirectory = Path.Combine(root, "src", "EbookReader.Application", "Search");

        Assert.True(Directory.Exists(searchDirectory));
        foreach (string sourceFile in Directory.EnumerateFiles(searchDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("EbookReader.Layout", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Terminal.Gui", source, StringComparison.Ordinal);
            Assert.DoesNotContain("EbookReader.Epub", source, StringComparison.Ordinal);
            Assert.DoesNotContain("VisualLine", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BookLayout", source, StringComparison.Ordinal);
        }

        string engine = File.ReadAllText(Path.Combine(searchDirectory, "BookTextSearch.cs"));
        Assert.Contains("ContentText.GetPlainText(block)", engine, StringComparison.Ordinal);
        Assert.Contains("ReadingLocation(section.Id, block.Id, matchOffset)", engine, StringComparison.Ordinal);
    }

    [Fact]
    public void M23ReaderWindowUsesInlineSearchPromptAndLogicalResultNavigation()
    {
        string root = RepositoryRoot.Find();
        string window = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));

        Assert.Contains("IsCharacter(key, '/')", window, StringComparison.Ordinal);
        Assert.Contains("_session.Search(query)", window, StringComparison.Ordinal);
        Assert.Contains("_session.NextSearchResult", window, StringComparison.Ordinal);
        Assert.Contains("_session.PreviousSearchResult", window, StringComparison.Ordinal);
        Assert.Contains("key == Key.Backspace", window, StringComparison.Ordinal);
        Assert.DoesNotContain("TextField", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Dialog", window, StringComparison.Ordinal);
        Assert.DoesNotContain("EbookReader.Epub", window, StringComparison.Ordinal);
    }

    [Fact]
    public void M23SearchStateIsNotPersistedInReadingStateJson()
    {
        string root = RepositoryRoot.Find();
        string stateDirectory = Path.Combine(root, "src", "EbookReader.Application", "State");

        foreach (string sourceFile in Directory.EnumerateFiles(stateDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("SearchQuery", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BookSearchMatch", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void M24BookmarksPersistOnlyLogicalLocationsAndStayOutsideDomain()
    {
        string root = RepositoryRoot.Find();
        string bookmarkState = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Application",
            "State",
            "ReadingBookmarkSnapshot.cs"));
        string domainDirectory = Path.Combine(root, "src", "EbookReader.Domain");

        Assert.Contains("ReadingLocation Location", bookmarkState, StringComparison.Ordinal);
        Assert.DoesNotContain("PageNumber", bookmarkState, StringComparison.Ordinal);
        Assert.DoesNotContain("LineIndex", bookmarkState, StringComparison.Ordinal);
        Assert.DoesNotContain("LayoutPosition", bookmarkState, StringComparison.Ordinal);

        foreach (string sourceFile in Directory.EnumerateFiles(domainDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("ReadingBookmarkSnapshot", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void M24ReaderWindowUsesLogicalBookmarkSessionWithoutOwningJsonPersistence()
    {
        string root = RepositoryRoot.Find();
        string window = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));

        Assert.Contains("IsCharacter(key, 'b')", window, StringComparison.Ordinal);
        Assert.Contains("IsCharacter(key, 'B')", window, StringComparison.Ordinal);
        Assert.Contains("_session.ToggleBookmark()", window, StringComparison.Ordinal);
        Assert.Contains("_session.NavigateToBookmark", window, StringComparison.Ordinal);
        Assert.Contains("_session.RemoveBookmark", window, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonReadingStateStore", window, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Text.Json", window, StringComparison.Ordinal);
        Assert.DoesNotContain("EbookReader.Epub", window, StringComparison.Ordinal);
    }

    [Fact]
    public void M30StateSchemaSupportsLegacyV1V2AndWritesV3History()
    {
        string root = RepositoryRoot.Find();
        string store = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Application",
            "State",
            "JsonReadingStateStore.cs"));

        Assert.Contains("CurrentSchemaVersion = 3", store, StringComparison.Ordinal);
        Assert.Contains("document.SchemaVersion is not (1 or 2 or CurrentSchemaVersion)", store, StringComparison.Ordinal);
        Assert.Contains("Bookmarks = state.Bookmarks", store, StringComparison.Ordinal);
        Assert.Contains("History = state.History", store, StringComparison.Ordinal);
    }


    [Fact]
    public void M24Hotfix1BookmarkLimitAvoidsGlobalAliasInsideInterpolation()
    {
        string root = RepositoryRoot.Find();
        string session = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderSession.cs"));

        Assert.Contains("ReadingBookmarkState.MaximumBookmarksPerBook", session, StringComparison.Ordinal);
        Assert.DoesNotContain("{global::EbookReader.Application.State.ReadingBookmarkState", session, StringComparison.Ordinal);
    }

    [Fact]
    public void M24Hotfix1ReaderPaletteUsesRequestedSemanticColors()
    {
        string root = RepositoryRoot.Find();
        string palette = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderColorPalette.cs"));

        Assert.Contains("new(\"White\", \"Black\")", palette, StringComparison.Ordinal);
        Assert.Contains("new(\"Cyan\", \"Black\")", palette, StringComparison.Ordinal);
        Assert.Contains("new(\"Green\", \"Black\", TextStyle.Bold)", palette, StringComparison.Ordinal);
        Assert.Contains("new(\"Yellow\", \"Black\", TextStyle.Italic)", palette, StringComparison.Ordinal);
        Assert.Contains("new(\"Gray\", \"Black\")", palette, StringComparison.Ordinal);
    }

    [Fact]
    public void M24Hotfix1TerminalColorsStayOutsideLayoutProject()
    {
        string root = RepositoryRoot.Find();
        string layoutRoot = Path.Combine(root, "src", "EbookReader.Layout");
        string combined = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(layoutRoot, "*.cs", SearchOption.AllDirectories).Select(path => File.ReadAllText(path)));

        Assert.DoesNotContain("Terminal.Gui", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Cyan", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Green", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Yellow", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void M24Hotfix2WindowUsesSetSchemeForGrayChromeAndWhiteContent()
    {
        string root = RepositoryRoot.Find();
        string window = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));

        Assert.Contains("SetScheme(ReaderColorPalette.ChromeScheme);", window, StringComparison.Ordinal);
        Assert.Contains("_header.SetScheme(ReaderColorPalette.PlainScheme);", window, StringComparison.Ordinal);
        Assert.Contains("_headerSeparator.SetScheme(ReaderColorPalette.ChromeScheme);", window, StringComparison.Ordinal);
        Assert.Contains("_footerSeparator.SetScheme(ReaderColorPalette.ChromeScheme);", window, StringComparison.Ordinal);
        Assert.Contains("_footer.SetScheme(ReaderColorPalette.PlainScheme);", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Scheme = ReaderColorPalette", window, StringComparison.Ordinal);
        Assert.Contains("_headerSeparator", window, StringComparison.Ordinal);
        Assert.Contains("_footerSeparator", window, StringComparison.Ordinal);
    }

    [Fact]
    public void M24Hotfix1ReaderBodyUsesSemanticStyleRuns()
    {
        string root = RepositoryRoot.Find();
        string body = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderBodyView.cs"));

        Assert.Contains("VisualLineKind.Heading", body, StringComparison.Ordinal);
        Assert.Contains("line.StyleSpans", body, StringComparison.Ordinal);
        Assert.Contains("ReaderColorPalette.ForStyle", body, StringComparison.Ordinal);
        Assert.Contains("SetScheme(ReaderColorPalette.PlainScheme);", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Scheme = ReaderColorPalette", body, StringComparison.Ordinal);
        Assert.Contains("OnDrawingContent(DrawContext? context)", body, StringComparison.Ordinal);
    }

    [Fact]
    public void M25StableProgressUsesLogicalDomainTextAndNeverPersistsLayoutCoordinates()
    {
        string root = RepositoryRoot.Find();
        string progress = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Application",
            "Progress",
            "BookProgressIndex.cs"));
        string window = File.ReadAllText(Path.Combine(
            root,
            "src",
            "EbookReader.Cli",
            "Tui",
            "ReaderWindow.cs"));
        string stateDirectory = Path.Combine(root, "src", "EbookReader.Application", "State");
        string state = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(stateDirectory, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.Contains("ContentText.GetPlainText(block).Length", progress, StringComparison.Ordinal);
        Assert.Contains("location.CharacterOffset", progress, StringComparison.Ordinal);
        Assert.DoesNotContain("BookLayout", progress, StringComparison.Ordinal);
        Assert.DoesNotContain("PageNumber", progress, StringComparison.Ordinal);
        Assert.DoesNotContain("Viewport", progress, StringComparison.Ordinal);
        Assert.Contains("_session.Progress.Percentage", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Progress", state, StringComparison.Ordinal);
        Assert.DoesNotContain("Percentage", state, StringComparison.Ordinal);
    }

    [Fact]
    public void M30LibraryHistoryStaysLogicalAndTerminalGuiRemainsInCli()
    {
        string root = RepositoryRoot.Find();
        string libraryDirectory = Path.Combine(root, "src", "EbookReader.Application", "Library");
        string library = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(libraryDirectory, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        string window = File.ReadAllText(Path.Combine(root, "src", "EbookReader.Cli", "Tui", "LibraryWindow.cs"));
        string state = File.ReadAllText(Path.Combine(
            root, "src", "EbookReader.Application", "State", "JsonReadingStateStore.cs"));

        Assert.Contains("ReadingLocation", library, StringComparison.Ordinal);
        Assert.DoesNotContain("BookLayout", library, StringComparison.Ordinal);
        Assert.DoesNotContain("PageNumber", library, StringComparison.Ordinal);
        Assert.DoesNotContain("Viewport", library, StringComparison.Ordinal);
        Assert.DoesNotContain("Terminal.Gui", library, StringComparison.Ordinal);
        Assert.Contains("ViewportChanged", window, StringComparison.Ordinal);
        Assert.Contains("CurrentSchemaVersion = 3", state, StringComparison.Ordinal);
        Assert.DoesNotContain("progress", state, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ProjectReferencesAngleSharp(string projectFile)
    {
        XDocument document = XDocument.Load(projectFile);
        return document
            .Descendants("PackageReference")
            .Any(element => string.Equals(
                element.Attribute("Include")?.Value,
                "AngleSharp",
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool ProjectReferencesTerminalGui(string projectFile)
    {
        XDocument document = XDocument.Load(projectFile);
        return document
            .Descendants("PackageReference")
            .Any(element => string.Equals(
                element.Attribute("Include")?.Value,
                "Terminal.Gui",
                StringComparison.OrdinalIgnoreCase));
    }
}

internal static class RepositoryRoot
{
    public static string Find()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "EbookReader.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Impossibile trovare la root della repository partendo da '{AppContext.BaseDirectory}'.");
    }
}

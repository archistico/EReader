using EbookReader.Epub.Navigation;

namespace EbookReader.Epub.Tests.Navigation;

public sealed class EpubNavigationReaderTests
{
    [Fact]
    public void ReadsNestedEpub3TableOfContents()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation(
            "<li><a href=\"Text/ch1.xhtml#s1\"> Chapter <em>One</em> </a><ol><li><a href=\"Text/ch2.xhtml#s2\">Section Two</a></li></ol></li>");

        EpubNavigationDocument document = NavigationFixtureFactory.ReadEpub3(nav);

        Assert.Equal(EpubNavigationSourceKind.Epub3NavigationDocument, document.SourceKind);
        Assert.Equal("EPUB/nav.xhtml", document.SourcePath.Value);
        Assert.Equal("Contents", document.TableOfContents.Label);
        EpubNavigationNode first = Assert.Single(document.TableOfContents.Items);
        Assert.Equal("Chapter One", first.Label);
        Assert.Equal("EPUB/Text/ch1.xhtml", first.Target!.LocalPath!.Value);
        Assert.Equal("s1", first.Target.Fragment);
        EpubNavigationNode child = Assert.Single(first.Children);
        Assert.Equal("Section Two", child.Label);
        Assert.Equal("EPUB/Text/ch2.xhtml", child.Target!.LocalPath!.Value);
    }

    [Fact]
    public void ReadsEpub3GroupingSpanWithChildren()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation(
            "<li><span>Part One</span><ol><li><a href=\"Text/ch1.xhtml\">Chapter One</a></li></ol></li>");

        EpubNavigationNode group = Assert.Single(NavigationFixtureFactory.ReadEpub3(nav).TableOfContents.Items);

        Assert.Null(group.Target);
        Assert.Equal("Part One", group.Label);
        Assert.Single(group.Children);
    }

    [Fact]
    public void UsesImageAlternativeTextInEpub3Label()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation(
            "<li><a href=\"Text/ch1.xhtml\">Chapter <img src=\"cover.png\" alt=\"One\" /></a></li>");

        EpubNavigationNode node = Assert.Single(NavigationFixtureFactory.ReadEpub3(nav).TableOfContents.Items);

        Assert.Equal("Chapter One", node.Label);
    }

    [Fact]
    public void DecodesEpub3FragmentWithoutChangingResourcePath()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation(
            "<li><a href=\"Text/ch1.xhtml#section%201\">One</a></li>");

        EpubNavigationTarget target = Assert.Single(NavigationFixtureFactory.ReadEpub3(nav).TableOfContents.Items).Target!;

        Assert.Equal("EPUB/Text/ch1.xhtml", target.LocalPath!.Value);
        Assert.Equal("section 1", target.Fragment);
    }

    [Fact]
    public void SupportsSameDocumentFragmentInEpub3Navigation()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation(
            "<li><a href=\"#local-anchor\">Local</a></li>");

        EpubNavigationDocument document = NavigationFixtureFactory.ReadEpub3(
            nav,
            spine: "<itemref idref=\"nav\" linear=\"no\" /><itemref idref=\"c1\" /><itemref idref=\"c2\" />");
        EpubNavigationTarget target = Assert.Single(document.TableOfContents.Items).Target!;

        Assert.Equal("EPUB/nav.xhtml", target.LocalPath!.Value);
        Assert.Equal("local-anchor", target.Fragment);
    }

    [Fact]
    public void ReadsOptionalPageListAndLandmarks()
    {
        string nav = """
        <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
          <body>
            <nav epub:type="toc"><ol><li><a href="Text/ch1.xhtml">One</a></li></ol></nav>
            <nav epub:type="page-list"><h2>Pages</h2><ol><li><a href="Text/ch1.xhtml#p1">1</a></li></ol></nav>
            <nav epub:type="landmarks"><h2>Landmarks</h2><ol><li><a epub:type="bodymatter" href="Text/ch1.xhtml#body">Start</a></li></ol></nav>
          </body>
        </html>
        """;

        EpubNavigationDocument document = NavigationFixtureFactory.ReadEpub3(nav);

        Assert.Equal("Pages", document.PageList!.Label);
        Assert.Equal("1", Assert.Single(document.PageList.Items).Label);
        Assert.Equal("Landmarks", document.Landmarks!.Label);
        EpubNavigationNode landmark = Assert.Single(document.Landmarks.Items);
        Assert.Equal("Start", landmark.Label);
        Assert.Contains("bodymatter", landmark.Types);
    }

    [Fact]
    public void RejectsMissingEpub3NavigationManifestItem()
    {
        string manifest = "<item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" />";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(
            NavigationFixtureFactory.ValidEpub3Navigation("<li><a href=\"Text/ch1.xhtml\">One</a></li>"),
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />");

        Assert.Equal(EpubNavigationErrorCode.NavigationDocumentNotFound, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMultipleEpub3NavigationManifestItems()
    {
        string manifest = "<item id=\"nav1\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\" /><item id=\"nav2\" href=\"nav2.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" />";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(
            NavigationFixtureFactory.ValidEpub3Navigation("<li><a href=\"Text/ch1.xhtml\">One</a></li>"),
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />",
            additionalEntries: [("EPUB/nav2.xhtml", NavigationFixtureFactory.ValidEpub3Navigation("<li><a href=\"Text/ch1.xhtml\">One</a></li>"))]);

        Assert.Equal(EpubNavigationErrorCode.MultipleNavigationDocuments, exception.ErrorCode);
    }

    [Fact]
    public void RejectsWrongEpub3NavigationMediaType()
    {
        string manifest = "<item id=\"nav\" href=\"nav.xhtml\" media-type=\"text/html\" properties=\"nav\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" />";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(
            NavigationFixtureFactory.ValidEpub3Navigation("<li><a href=\"Text/ch1.xhtml\">One</a></li>"),
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />");

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationMediaType, exception.ErrorCode);
    }

    [Fact]
    public void RejectsRemoteEpub3NavigationSource()
    {
        string manifest = "<item id=\"nav\" href=\"https://example.test/nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" />";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(
            NavigationFixtureFactory.ValidEpub3Navigation("<li><a href=\"Text/ch1.xhtml\">One</a></li>"),
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />");

        Assert.Equal(EpubNavigationErrorCode.NavigationSourceMustBeLocal, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMalformedEpub3NavigationXml()
    {
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure("<html>");

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationXhtml, exception.ErrorCode);
    }

    [Fact]
    public void RejectsDoctypeInEpub3Navigation()
    {
        string nav = "<!DOCTYPE html><html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\"><body><nav epub:type=\"toc\"><ol><li><a href=\"Text/ch1.xhtml\">One</a></li></ol></nav></body></html>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationXhtml, exception.ErrorCode);
    }

    [Fact]
    public void RejectsWrongEpub3XhtmlNamespace()
    {
        string nav = "<html xmlns:epub=\"http://www.idpf.org/2007/ops\"><body><nav epub:type=\"toc\"><ol><li><a href=\"Text/ch1.xhtml\">One</a></li></ol></nav></body></html>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationXhtmlNamespace, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingEpub3TocNav()
    {
        string nav = "<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\"><body><nav epub:type=\"page-list\"><ol><li><a href=\"Text/ch1.xhtml\">1</a></li></ol></nav></body></html>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.MissingTableOfContents, exception.ErrorCode);
    }

    [Fact]
    public void RejectsDuplicateEpub3TocNav()
    {
        string nav = "<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\"><body><nav epub:type=\"toc\"><ol><li><a href=\"Text/ch1.xhtml\">One</a></li></ol></nav><nav epub:type=\"toc\"><ol><li><a href=\"Text/ch2.xhtml\">Two</a></li></ol></nav></body></html>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.DuplicateNavigationAid, exception.ErrorCode);
    }

    [Fact]
    public void RejectsTocWithoutOrderedList()
    {
        string nav = "<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\"><body><nav epub:type=\"toc\"><h1>Contents</h1></nav></body></html>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationStructure, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEmptyEpub3OrderedList()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation(string.Empty);
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationStructure, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub3ListItemWithMultipleLabels()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation(
            "<li><a href=\"Text/ch1.xhtml\">One</a><span>Duplicate</span></li>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationStructure, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub3GroupingSpanWithoutChildren()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation("<li><span>Part One</span></li>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationStructure, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub3AnchorWithoutHref()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation("<li><a>One</a></li>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationHref, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEmptyEpub3Label()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation("<li><a href=\"Text/ch1.xhtml\">   </a></li>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.EmptyNavigationLabel, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub3MissingTargetResource()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation("<li><a href=\"Text/missing.xhtml\">Missing</a></li>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.NavigationTargetNotFound, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub3UndeclaredTargetResource()
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation("<li><a href=\"Text/extra.xhtml\">Extra</a></li>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(
            nav,
            additionalEntries: [("EPUB/Text/extra.xhtml", "<html />")]);

        Assert.Equal(EpubNavigationErrorCode.NavigationTargetNotFound, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub3TargetThatIsDeclaredButNotTopLevelContent()
    {
        string manifest = "<item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" /><item id=\"extra\" href=\"Text/extra.xhtml\" media-type=\"application/xhtml+xml\" />";
        string nav = NavigationFixtureFactory.ValidEpub3Navigation("<li><a href=\"Text/extra.xhtml\">Extra</a></li>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(
            nav,
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />",
            additionalEntries: [("EPUB/Text/extra.xhtml", "<html />")]);

        Assert.Equal(EpubNavigationErrorCode.NavigationTargetNotFound, exception.ErrorCode);
    }

    [Theory]
    [InlineData("../../outside.xhtml")]
    [InlineData("Text%2Fch1.xhtml")]
    [InlineData("https://example.test/ch1.xhtml")]
    [InlineData("Text/ch1.xhtml?mode=1")]
    [InlineData("Text/ch1.xhtml#bad%ZZ")]
    public void RejectsInvalidEpub3NavigationHref(string href)
    {
        string nav = NavigationFixtureFactory.ValidEpub3Navigation($"<li><a href=\"{href}\">One</a></li>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationHref, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub3NavigationDeeperThanLimit()
    {
        string nested = "<li><a href=\"Text/ch1.xhtml\">Leaf</a></li>";
        for (int index = 0; index < 65; index++)
        {
            nested = $"<li><span>Level {index}</span><ol>{nested}</ol></li>";
        }

        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(
            NavigationFixtureFactory.ValidEpub3Navigation(nested));

        Assert.Equal(EpubNavigationErrorCode.NavigationDepthExceeded, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub3NavigationWithTooManyNodes()
    {
        System.Text.StringBuilder items = new();
        for (int index = 0; index <= 20_000; index++)
        {
            items.Append("<li><a href=\"Text/ch1.xhtml\">N</a></li>");
        }

        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(
            NavigationFixtureFactory.ValidEpub3Navigation(items.ToString()));

        Assert.Equal(EpubNavigationErrorCode.TooManyNavigationNodes, exception.ErrorCode);
    }

    [Fact]
    public void RejectsOversizedEpub3NavigationDocument()
    {
        string padding = new('x', (4 * 1024 * 1024) + 1);
        string nav = NavigationFixtureFactory.ValidEpub3Navigation(
            $"<li><a href=\"Text/ch1.xhtml\">{padding}</a></li>");

        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub3Failure(nav);

        Assert.Equal(EpubNavigationErrorCode.NavigationDocumentTooLarge, exception.ErrorCode);
    }

    [Fact]
    public void ReadsNestedEpub2NcxIncludingCanonicalDoctype()
    {
        string points = "<navPoint id=\"n1\" playOrder=\"1\"><navLabel><text>Chapter One</text></navLabel><content src=\"Text/ch1.xhtml#s1\"/><navPoint id=\"n2\" playOrder=\"2\"><navLabel><text>Section Two</text></navLabel><content src=\"Text/ch2.xhtml#s2\"/></navPoint></navPoint>";
        EpubNavigationDocument document = NavigationFixtureFactory.ReadEpub2(
            NavigationFixtureFactory.ValidNcx(points, includeDoctype: true));

        Assert.Equal(EpubNavigationSourceKind.Epub2Ncx, document.SourceKind);
        Assert.Equal("EPUB/toc.ncx", document.SourcePath.Value);
        EpubNavigationNode first = Assert.Single(document.TableOfContents.Items);
        Assert.Equal("Chapter One", first.Label);
        Assert.Equal("s1", first.Target!.Fragment);
        Assert.Equal("Section Two", Assert.Single(first.Children).Label);
    }

    [Fact]
    public void RejectsEpub2WithoutSpineTocReference()
    {
        string ncx = NavigationFixtureFactory.ValidNcx("<navPoint id=\"n1\"><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(ncx, spineAttributes: string.Empty);

        Assert.Equal(EpubNavigationErrorCode.MissingNcxReference, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub2SpineTocReferenceToMissingManifestId()
    {
        string ncx = NavigationFixtureFactory.ValidNcx("<navPoint id=\"n1\"><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(ncx, spineAttributes: "toc=\"missing\"");

        Assert.Equal(EpubNavigationErrorCode.NcxManifestItemNotFound, exception.ErrorCode);
    }

    [Fact]
    public void RejectsWrongEpub2NcxMediaType()
    {
        string manifest = "<item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/xml\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" />";
        string ncx = NavigationFixtureFactory.ValidNcx("<navPoint id=\"n1\"><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(
            ncx,
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />");

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxMediaType, exception.ErrorCode);
    }

    [Fact]
    public void RejectsRemoteEpub2NcxSource()
    {
        string manifest = "<item id=\"ncx\" href=\"https://example.test/toc.ncx\" media-type=\"application/x-dtbncx+xml\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" />";
        string ncx = NavigationFixtureFactory.ValidNcx("<navPoint id=\"n1\"><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint>");
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(
            ncx,
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />");

        Assert.Equal(EpubNavigationErrorCode.NavigationSourceMustBeLocal, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMalformedNcxXml()
    {
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure("<ncx>");

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxXml, exception.ErrorCode);
    }

    [Fact]
    public void RejectsWrongNcxNamespace()
    {
        string ncx = "<ncx version=\"2005-1\"><navMap /></ncx>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(ncx);

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxNamespace, exception.ErrorCode);
    }

    [Fact]
    public void RejectsWrongNcxVersion()
    {
        string ncx = "<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"1\"><navMap><navPoint id=\"n\"><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint></navMap></ncx>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(ncx);

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxXml, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingNcxNavMap()
    {
        string ncx = "<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\"></ncx>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(ncx);

        Assert.Equal(EpubNavigationErrorCode.MissingNcxNavMap, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEmptyNcxNavMap()
    {
        string ncx = "<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\"><navMap /></ncx>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(ncx);

        Assert.Equal(EpubNavigationErrorCode.MissingNcxNavMap, exception.ErrorCode);
    }

    [Fact]
    public void RejectsDuplicateNcxNavPointId()
    {
        string points = "<navPoint id=\"dup\"><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint><navPoint id=\"dup\"><navLabel><text>Two</text></navLabel><content src=\"Text/ch2.xhtml\"/></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(NavigationFixtureFactory.ValidNcx(points));

        Assert.Equal(EpubNavigationErrorCode.DuplicateNcxId, exception.ErrorCode);
    }

    [Fact]
    public void RejectsNcxNavPointWithoutId()
    {
        string points = "<navPoint><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(NavigationFixtureFactory.ValidNcx(points));

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxNavPoint, exception.ErrorCode);
    }

    [Fact]
    public void RejectsNcxNavPointWithoutLabel()
    {
        string points = "<navPoint id=\"n1\"><content src=\"Text/ch1.xhtml\"/></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(NavigationFixtureFactory.ValidNcx(points));

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxNavPoint, exception.ErrorCode);
    }

    [Fact]
    public void RejectsNcxNavPointWithoutText()
    {
        string points = "<navPoint id=\"n1\"><navLabel /><content src=\"Text/ch1.xhtml\"/></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(NavigationFixtureFactory.ValidNcx(points));

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxNavPoint, exception.ErrorCode);
    }

    [Fact]
    public void RejectsNcxNavPointWithoutContent()
    {
        string points = "<navPoint id=\"n1\"><navLabel><text>One</text></navLabel></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(NavigationFixtureFactory.ValidNcx(points));

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxNavPoint, exception.ErrorCode);
    }

    [Fact]
    public void RejectsNcxContentWithoutSource()
    {
        string points = "<navPoint id=\"n1\"><navLabel><text>One</text></navLabel><content /></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(NavigationFixtureFactory.ValidNcx(points));

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxNavPoint, exception.ErrorCode);
    }

    [Fact]
    public void RejectsNcxTargetOutsideManifest()
    {
        string points = "<navPoint id=\"n1\"><navLabel><text>Missing</text></navLabel><content src=\"Text/missing.xhtml\"/></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(NavigationFixtureFactory.ValidNcx(points));

        Assert.Equal(EpubNavigationErrorCode.NavigationTargetNotFound, exception.ErrorCode);
    }

    [Fact]
    public void RejectsCanonicalNcxDoctypeWhenNavPointOmitsPlayOrder()
    {
        string points = "<navPoint id=\"n1\"><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(
            NavigationFixtureFactory.ValidNcx(points, includeDoctype: true));

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxNavPoint, exception.ErrorCode);
    }

    [Fact]
    public void RejectsNonCanonicalNcxDoctype()
    {
        string ncx = "<!DOCTYPE ncx SYSTEM \"https://example.test/not-ncx.dtd\"><ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\"><head><meta name=\"dtb:uid\" content=\"book-id\" /></head><docTitle><text>Test</text></docTitle><navMap><navPoint id=\"n1\" playOrder=\"1\"><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint></navMap></ncx>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(ncx);

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxXml, exception.ErrorCode);
    }

    [Fact]
    public void RejectsInternalSubsetInCanonicalNcxDoctype()
    {
        string ncx = "<!DOCTYPE ncx PUBLIC \"-//NISO//DTD ncx 2005-1//EN\" \"http://www.daisy.org/z3986/2005/ncx-2005-1.dtd\" [<!ENTITY x \"unsafe\">]><ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\"><head><meta name=\"dtb:uid\" content=\"book-id\" /></head><docTitle><text>Test</text></docTitle><navMap><navPoint id=\"n1\" playOrder=\"1\"><navLabel><text>One</text></navLabel><content src=\"Text/ch1.xhtml\"/></navPoint></navMap></ncx>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(ncx);

        Assert.Equal(EpubNavigationErrorCode.InvalidNcxXml, exception.ErrorCode);
    }

    [Fact]
    public void RejectsNcxTargetThatIsDeclaredButNotTopLevelContent()
    {
        string manifest = "<item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" /><item id=\"extra\" href=\"Text/extra.xhtml\" media-type=\"application/xhtml+xml\" />";
        string points = "<navPoint id=\"n1\"><navLabel><text>Extra</text></navLabel><content src=\"Text/extra.xhtml\"/></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(
            NavigationFixtureFactory.ValidNcx(points),
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />",
            additionalEntries: [("EPUB/Text/extra.xhtml", "<html />")]);

        Assert.Equal(EpubNavigationErrorCode.NavigationTargetNotFound, exception.ErrorCode);
    }

    [Theory]
    [InlineData("https://example.test/ch1.xhtml")]
    [InlineData("Text/ch1.xhtml?mode=1")]
    [InlineData("../../outside.xhtml")]
    public void RejectsInvalidNcxTarget(string href)
    {
        string points = $"<navPoint id=\"n1\"><navLabel><text>One</text></navLabel><content src=\"{href}\"/></navPoint>";
        EpubNavigationException exception = NavigationFixtureFactory.ReadEpub2Failure(
            NavigationFixtureFactory.ValidNcx(points));

        Assert.Equal(EpubNavigationErrorCode.InvalidNavigationHref, exception.ErrorCode);
    }
}

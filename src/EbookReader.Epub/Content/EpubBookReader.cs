using System.Globalization;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Navigation;
using EbookReader.Domain.Reading;
using EbookReader.Domain.Resources;
using EbookReader.Epub.Container;
using EbookReader.Epub.Navigation;
using EbookReader.Epub.Package;

namespace EbookReader.Epub.Content;

/// <summary>
/// Orchestrates the EPUB adapter and projects EPUB 2/3 reflowable XHTML content into the
/// format-neutral Domain model. AngleSharp and all source-format details stop at this boundary.
/// </summary>
public static class EpubBookReader
{
    private const string XhtmlMediaType = "application/xhtml+xml";

    private static readonly char[] SemanticTypeSeparators = [' ', '\t', '\r', '\n', '\f'];

    private static readonly HashSet<string> BlockElementNames = new(StringComparer.Ordinal)
    {
        "address", "article", "aside", "blockquote", "div", "dl", "figure", "footer", "form",
        "h1", "h2", "h3", "h4", "h5", "h6", "header", "hr", "img", "main", "nav", "ol", "p", "pre",
        "section", "table", "ul",
    };

    public static Book Read(EpubContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        EpubPackageDocument package = EpubPackageReader.Read(container);
        EpubNavigationDocument navigation = EpubNavigationReader.Read(container, package);
        return Read(container, package, navigation);
    }

    public static Book Read(
        EpubContainer container,
        EpubPackageDocument package,
        EpubNavigationDocument navigation)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        return ReadCore(container, package, navigation, recoverSupplementaryContent: false).Book;
    }

    internal static EpubBookRecoveryResult ReadRecovering(
        EpubContainer container,
        EpubPackageDocument package,
        EpubNavigationDocument? navigation) =>
        ReadCore(container, package, navigation, recoverSupplementaryContent: true);

    private static EpubBookRecoveryResult ReadCore(
        EpubContainer container,
        EpubPackageDocument package,
        EpubNavigationDocument? navigation,
        bool recoverSupplementaryContent)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(package);

        Dictionary<string, EpubManifestItem> manifestById = package.Manifest.ToDictionary(item => item.Id, StringComparer.Ordinal);
        Dictionary<string, EpubManifestItem> manifestByPath = package.Manifest
            .Where(item => item.LocalPath is not null)
            .ToDictionary(item => item.LocalPath!.Value, StringComparer.Ordinal);

        Dictionary<string, ReadingLocation> anchors = new(StringComparer.Ordinal);
        Dictionary<string, SectionId> firstSectionByPath = new(StringComparer.Ordinal);
        List<SectionDraft> sectionDrafts = new(package.Spine.Count);
        List<EpubContentRecoveryIssue> recoveryIssues = [];

        for (int index = 0; index < package.Spine.Count; index++)
        {
            EpubSpineItem spineItem = package.Spine[index];
            Dictionary<string, ReadingLocation> sectionAnchors = new(StringComparer.Ordinal);

            try
            {
                EpubManifestItem contentItem = ResolveXhtmlSpineItem(spineItem, manifestById);
                OcfPath sourcePath = contentItem.LocalPath
                    ?? throw Error(
                        EpubContentErrorCode.MissingSpineContent,
                        $"La risorsa spine '{contentItem.Id}' deve essere locale al contenitore EPUB.");

                if (!container.Contains(sourcePath))
                {
                    throw Error(
                        EpubContentErrorCode.MissingSpineContent,
                        $"Il Content Document '{sourcePath.Value}' non esiste nel contenitore EPUB.");
                }

                SectionId sectionId = new($"spine-{index + 1:D6}-{spineItem.IdRef}");
                SectionDraft section = ParseSection(
                    container,
                    sourcePath,
                    sectionId,
                    spineItem.IsLinear,
                    manifestByPath,
                    sectionAnchors);

                if (sectionAnchors.Keys.Any(anchors.ContainsKey))
                {
                    throw Error(
                        EpubContentErrorCode.DuplicateAnchor,
                        $"Anchor duplicato rilevato durante la composizione del Content Document '{sourcePath.Value}'.");
                }

                foreach ((string key, ReadingLocation value) in sectionAnchors)
                {
                    anchors.Add(key, value);
                }

                sectionDrafts.Add(section);
                firstSectionByPath.TryAdd(sourcePath.Value, sectionId);
            }
            catch (EpubContentException exception) when (recoverSupplementaryContent && !spineItem.IsLinear)
            {
                recoveryIssues.Add(new EpubContentRecoveryIssue(
                    EpubContentRecoveryKind.SupplementarySpineItemSkipped,
                    $"Spine supplementare '{spineItem.IdRef}' ignorato: {exception.Message}"));
            }
        }

        List<ReadingSection> readingOrder = ResolveSections(
            sectionDrafts,
            firstSectionByPath,
            anchors,
            recoverSupplementaryContent,
            recoveryIssues);
        TableOfContents tableOfContents = TableOfContents.Empty;
        if (navigation is not null)
        {
            try
            {
                tableOfContents = recoverSupplementaryContent
                    ? BuildTableOfContentsRecovering(navigation.TableOfContents.Items, firstSectionByPath, anchors, recoveryIssues)
                    : BuildTableOfContents(navigation.TableOfContents.Items, firstSectionByPath, anchors);
            }
            catch (EpubContentException exception) when (recoverSupplementaryContent)
            {
                recoveryIssues.Add(new EpubContentRecoveryIssue(
                    EpubContentRecoveryKind.TableOfContentsDropped,
                    $"Indice di navigazione ignorato perché non risolvibile sul contenuto leggibile: {exception.Message}"));
            }
        }

        List<BookResource> resources = BuildResources(package.Manifest);
        BookMetadata metadata = BuildMetadata(package.Metadata);

        Book book = new(
            new BookId(package.Metadata.UniqueIdentifier),
            metadata,
            readingOrder,
            tableOfContents,
            resources);

        return new EpubBookRecoveryResult(book, recoveryIssues.ToArray());
    }

    private static SectionDraft ParseSection(
        EpubContainer container,
        OcfPath sourcePath,
        SectionId sectionId,
        bool isLinear,
        Dictionary<string, EpubManifestItem> manifestByPath,
        Dictionary<string, ReadingLocation> anchors)
    {
        string source;
        using (Stream stream = container.OpenEntry(sourcePath))
        {
            source = ReadBoundedText(stream, sourcePath);
        }

        HtmlParser parser = new();
        IDocument document = parser.ParseDocument(source);

        IElement? body = document.QuerySelector("body");
        if (body is null)
        {
            throw Error(EpubContentErrorCode.MissingBody, $"Il Content Document '{sourcePath.Value}' non contiene body.");
        }

        ValidateNodeBudget(body, sourcePath);

        SectionDraft section = new(sectionId, sourcePath, isLinear);
        RegisterElementAnchor(body, sectionId, null, 0, sourcePath, anchors);
        ParseFlowNodes(body.ChildNodes, section, manifestByPath, anchors, depth: 1, listDepth: 0);

        string? documentTitle = NormalizeOptionalText(document.QuerySelector("title")?.TextContent);
        section.Title ??= documentTitle;
        return section;
    }

    private static void ParseFlowNodes(
        INodeList nodes,
        SectionDraft section,
        Dictionary<string, EpubManifestItem> manifestByPath,
        Dictionary<string, ReadingLocation> anchors,
        int depth,
        int listDepth)
    {
        EnsureDepth(depth);
        List<INode> inlineBatch = [];

        foreach (INode node in nodes)
        {
            if (node is IElement element && IsBlockElement(element))
            {
                FlushInlineBatch(inlineBatch, section, anchors);
                ParseBlockElement(element, section, manifestByPath, anchors, depth, listDepth);
            }
            else
            {
                inlineBatch.Add(node);
            }
        }

        FlushInlineBatch(inlineBatch, section, anchors);
    }

    private static void FlushInlineBatch(
        List<INode> inlineBatch,
        SectionDraft section,
        Dictionary<string, ReadingLocation> anchors)
    {
        if (inlineBatch.Count == 0)
        {
            return;
        }

        BlockId blockId = NextBlockId(section);
        InlineParseState state = new(section.Id, blockId, section.SourcePath, anchors);
        List<InlineDraft> inlines = ParseInlineNodes(inlineBatch, state, depth: 1);
        if (state.Length > 0)
        {
            BlockDraft block = new(blockId, BlockDraftKind.Paragraph);
            block.Inlines.AddRange(inlines);
            AddBlock(section, block);
        }

        inlineBatch.Clear();
    }

    private static void ParseBlockElement(
        IElement element,
        SectionDraft section,
        Dictionary<string, EpubManifestItem> manifestByPath,
        Dictionary<string, ReadingLocation> anchors,
        int depth,
        int listDepth)
    {
        EnsureDepth(depth);
        string name = element.LocalName;

        if (name is "script" or "style" or "template" or "nav" or "form")
        {
            return;
        }

        if (name.Length == 2 && name[0] == 'h' && name[1] is >= '1' and <= '6')
        {
            BlockId id = NextBlockId(section);
            InlineParseState state = new(section.Id, id, section.SourcePath, anchors);
            RegisterElementAnchor(element, section.Id, id, 0, section.SourcePath, anchors);
            List<InlineDraft> inlines = ParseInlineNodes(element.ChildNodes, state, depth + 1);

            BlockDraft heading = new(id, BlockDraftKind.Heading)
            {
                LevelOrDepth = name[1] - '0',
            };
            heading.Inlines.AddRange(inlines);
            AddBlock(section, heading);
            if (state.Length > 0)
            {
                section.Title ??= PlainText(inlines);
            }
            return;
        }

        switch (name)
        {
            case "p":
                AddInlineBlock(element, section, anchors, BlockDraftKind.Paragraph, depth);
                return;
            case "blockquote":
                AddInlineBlock(element, section, anchors, BlockDraftKind.Quote, depth);
                return;
            case "pre":
                AddPreformattedBlock(element, section, anchors);
                return;
            case "hr":
                AddThematicBreak(element, section, anchors);
                return;
            case "img":
                AddImageBlock(element, section, manifestByPath, anchors, caption: null);
                return;
            case "figure":
                AddFigure(element, section, manifestByPath, anchors);
                return;
            case "ul":
                AddList(element, section, manifestByPath, anchors, depth, listDepth + 1, ListKind.Unordered);
                return;
            case "ol":
                AddList(element, section, manifestByPath, anchors, depth, listDepth + 1, ListKind.Ordered);
                return;
            case "table":
                AddInlineBlock(element, section, anchors, BlockDraftKind.Paragraph, depth);
                return;
            default:
                ParseContainerElement(element, section, manifestByPath, anchors, depth, listDepth);
                return;
        }
    }

    private static void ParseContainerElement(
        IElement element,
        SectionDraft section,
        Dictionary<string, EpubManifestItem> manifestByPath,
        Dictionary<string, ReadingLocation> anchors,
        int depth,
        int listDepth)
    {
        int firstNewBlock = section.Blocks.Count;
        ParseFlowNodes(element.ChildNodes, section, manifestByPath, anchors, depth + 1, listDepth);

        string? anchor = OptionalId(element.Id);
        if (anchor is null)
        {
            return;
        }

        ReadingLocation location = firstNewBlock < section.Blocks.Count
            ? ReadingLocation.AtBlockStart(section.Id, section.Blocks[firstNewBlock].Id)
            : ReadingLocation.AtSectionStart(section.Id);
        RegisterAnchor(section.SourcePath, anchor, location, anchors);
    }

    private static void AddInlineBlock(
        IElement element,
        SectionDraft section,
        Dictionary<string, ReadingLocation> anchors,
        BlockDraftKind kind,
        int depth)
    {
        BlockId id = NextBlockId(section);
        InlineParseState state = new(section.Id, id, section.SourcePath, anchors);
        RegisterElementAnchor(element, section.Id, id, 0, section.SourcePath, anchors);
        List<InlineDraft> inlines = ParseInlineNodes(element.ChildNodes, state, depth + 1);

        BlockDraft block = new(id, kind)
        {
            LevelOrDepth = kind == BlockDraftKind.Quote ? Math.Max(1, CountAncestorQuotes(element)) : 1,
        };
        block.Inlines.AddRange(inlines);
        AddBlock(section, block);
    }

    private static void AddPreformattedBlock(
        IElement element,
        SectionDraft section,
        Dictionary<string, ReadingLocation> anchors)
    {
        string text = element.TextContent;
        BlockId id = NextBlockId(section);
        RegisterElementAnchor(element, section.Id, id, 0, section.SourcePath, anchors);
        AddBlock(section, new BlockDraft(id, BlockDraftKind.Preformatted) { Text = text });
    }

    private static void AddThematicBreak(
        IElement element,
        SectionDraft section,
        Dictionary<string, ReadingLocation> anchors)
    {
        BlockId id = NextBlockId(section);
        RegisterElementAnchor(element, section.Id, id, 0, section.SourcePath, anchors);
        AddBlock(section, new BlockDraft(id, BlockDraftKind.ThematicBreak));
    }

    private static void AddFigure(
        IElement figure,
        SectionDraft section,
        Dictionary<string, EpubManifestItem> manifestByPath,
        Dictionary<string, ReadingLocation> anchors)
    {
        IElement? image = figure.QuerySelector("img");
        if (image is null)
        {
            ParseContainerElement(figure, section, manifestByPath, anchors, depth: 1, listDepth: 0);
            return;
        }

        string? caption = NormalizeOptionalText(figure.QuerySelector("figcaption")?.TextContent);
        int before = section.Blocks.Count;
        AddImageBlock(image, section, manifestByPath, anchors, caption);
        string? figureAnchor = OptionalId(figure.Id);
        if (figureAnchor is not null)
        {
            ReadingLocation location = before < section.Blocks.Count
                ? ReadingLocation.AtBlockStart(section.Id, section.Blocks[before].Id)
                : ReadingLocation.AtSectionStart(section.Id);
            RegisterAnchor(section.SourcePath, figureAnchor, location, anchors);
        }
    }

    private static void AddImageBlock(
        IElement image,
        SectionDraft section,
        Dictionary<string, EpubManifestItem> manifestByPath,
        Dictionary<string, ReadingLocation> anchors,
        string? caption)
    {
        string? src = OptionalAttributeValue(image.GetAttribute("src"));
        if (src is null)
        {
            return;
        }

        OcfPath path = ResolveLocalPath(section.SourcePath, src, allowFragment: false);
        if (!manifestByPath.TryGetValue(path.Value, out EpubManifestItem? manifestItem))
        {
            throw Error(
                EpubContentErrorCode.ImageResourceNotFound,
                $"L'immagine '{src}' non corrisponde a una risorsa locale del manifest.");
        }

        if (!manifestItem.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw Error(
                EpubContentErrorCode.ImageResourceNotImage,
                $"La risorsa '{manifestItem.Id}' usata da img non ha un media type image/*.");
        }

        BlockId id = NextBlockId(section);
        RegisterElementAnchor(image, section.Id, id, 0, section.SourcePath, anchors);
        AddBlock(
            section,
            new BlockDraft(id, BlockDraftKind.Image)
            {
                ImageManifestId = manifestItem.Id,
                AlternativeText = NormalizeOptionalText(image.GetAttribute("alt")),
                Caption = caption,
            });
    }

    private static void AddList(
        IElement list,
        SectionDraft section,
        Dictionary<string, EpubManifestItem> manifestByPath,
        Dictionary<string, ReadingLocation> anchors,
        int depth,
        int listDepth,
        ListKind listKind)
    {
        int ordinal = ParsePositiveInteger(list.GetAttribute("start")) ?? 1;
        int firstNewBlock = section.Blocks.Count;

        foreach (IElement child in list.Children)
        {
            if (!string.Equals(child.LocalName, "li", StringComparison.Ordinal))
            {
                continue;
            }

            int? explicitValue = ParsePositiveInteger(child.GetAttribute("value"));
            int itemOrdinal = explicitValue ?? ordinal;
            BlockId id = NextBlockId(section);
            InlineParseState state = new(section.Id, id, section.SourcePath, anchors);
            RegisterElementAnchor(child, section.Id, id, 0, section.SourcePath, anchors);

            List<INode> inlineNodes = child.ChildNodes
                .Where(node => node is not IElement element || element.LocalName is not ("ul" or "ol"))
                .ToList();
            List<InlineDraft> inlines = ParseInlineNodes(inlineNodes, state, depth + 1);
            BlockDraft item = new(id, BlockDraftKind.ListItem)
            {
                ListKind = listKind,
                LevelOrDepth = listDepth,
                Ordinal = listKind == ListKind.Ordered ? itemOrdinal : null,
            };
            item.Inlines.AddRange(inlines);
            AddBlock(section, item);

            foreach (IElement nested in child.Children.Where(element => element.LocalName is "ul" or "ol"))
            {
                AddList(
                    nested,
                    section,
                    manifestByPath,
                    anchors,
                    depth + 1,
                    listDepth + 1,
                    nested.LocalName == "ol" ? ListKind.Ordered : ListKind.Unordered);
            }

            ordinal = itemOrdinal + 1;
        }

        string? listAnchor = OptionalId(list.Id);
        if (listAnchor is not null)
        {
            ReadingLocation location = firstNewBlock < section.Blocks.Count
                ? ReadingLocation.AtBlockStart(section.Id, section.Blocks[firstNewBlock].Id)
                : ReadingLocation.AtSectionStart(section.Id);
            RegisterAnchor(section.SourcePath, listAnchor, location, anchors);
        }
    }

    private static List<InlineDraft> ParseInlineNodes(IEnumerable<INode> nodes, InlineParseState state, int depth)
    {
        EnsureDepth(depth);
        List<InlineDraft> result = [];

        foreach (INode node in nodes)
        {
            switch (node)
            {
                case IText text:
                    AppendNormalizedText(result, text.Data, state);
                    break;
                case IElement element:
                    ParseInlineElement(element, result, state, depth + 1);
                    break;
            }
        }

        return result;
    }

    private static void ParseInlineElement(
        IElement element,
        List<InlineDraft> target,
        InlineParseState state,
        int depth)
    {
        EnsureDepth(depth);
        if (element.LocalName is "script" or "style" or "template")
        {
            return;
        }

        if (element.LocalName == "br")
        {
            state.PendingSpace = false;
            RegisterElementAnchor(element, state.SectionId, state.BlockId, state.Length, state.SourcePath, state.Anchors);
            target.Add(new LineBreakDraft());
            state.Length++;
            state.HasText = true;
            return;
        }

        FlushPendingSpace(target, state);
        RegisterElementAnchor(element, state.SectionId, state.BlockId, state.Length, state.SourcePath, state.Anchors);

        if (element.LocalName == "img")
        {
            string? alt = NormalizeOptionalText(element.GetAttribute("alt"));
            if (alt is not null)
            {
                AppendNormalizedText(target, alt, state);
            }

            return;
        }

        if (element.LocalName == "a")
        {
            string? href = element.GetAttribute("href")?.Trim();
            List<InlineDraft> content = ParseInlineNodes(element.ChildNodes, state, depth + 1);
            if (content.Count > 0)
            {
                if (href is null)
                {
                    target.AddRange(content);
                }
                else
                {
                    target.Add(new LinkDraft(
                        href,
                        state.SourcePath,
                        GetHyperlinkRole(element),
                        content));
                }
            }

            return;
        }

        if (element.LocalName is "strong" or "b")
        {
            List<InlineDraft> content = ParseInlineNodes(element.ChildNodes, state, depth + 1);
            if (content.Count > 0)
            {
                target.Add(new StrongDraft(content));
            }

            return;
        }

        if (element.LocalName is "em" or "i")
        {
            List<InlineDraft> content = ParseInlineNodes(element.ChildNodes, state, depth + 1);
            if (content.Count > 0)
            {
                target.Add(new EmphasisDraft(content));
            }

            return;
        }

        bool separatesFlow = BlockElementNames.Contains(element.LocalName);
        if (separatesFlow && state.HasText && target.Count > 0)
        {
            state.PendingSpace = true;
        }

        target.AddRange(ParseInlineNodes(element.ChildNodes, state, depth + 1));

        if (separatesFlow && state.HasText)
        {
            state.PendingSpace = true;
        }
    }

    private static void FlushPendingSpace(List<InlineDraft> target, InlineParseState state)
    {
        if (!state.PendingSpace || !state.HasText)
        {
            return;
        }

        if (target.Count > 0 && target[^1] is TextDraft previous)
        {
            target[^1] = new TextDraft(previous.Text + " ");
        }
        else
        {
            target.Add(new TextDraft(" "));
        }

        state.PendingSpace = false;
        state.LastCharacterWasSpace = true;
        state.Length++;
    }

    private static void AppendNormalizedText(List<InlineDraft> target, string value, InlineParseState state)
    {
        StringBuilder buffer = new();
        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                state.PendingSpace = (state.HasText || buffer.Length > 0) && !state.LastCharacterWasSpace;
                continue;
            }

            if (state.PendingSpace && (state.HasText || buffer.Length > 0))
            {
                buffer.Append(' ');
                state.Length++;
                state.PendingSpace = false;
            }

            buffer.Append(character);
            state.Length++;
            state.HasText = true;
            state.LastCharacterWasSpace = false;
        }

        if (buffer.Length == 0)
        {
            return;
        }

        if (target.Count > 0 && target[^1] is TextDraft previous)
        {
            target[^1] = new TextDraft(previous.Text + buffer.ToString());
        }
        else
        {
            target.Add(new TextDraft(buffer.ToString()));
        }
    }

    private static List<ReadingSection> ResolveSections(
        List<SectionDraft> drafts,
        Dictionary<string, SectionId> firstSectionByPath,
        Dictionary<string, ReadingLocation> anchors,
        bool recoverLinkIntegrity,
        List<EpubContentRecoveryIssue> recoveryIssues)
    {
        List<ReadingSection> sections = new(drafts.Count);
        foreach (SectionDraft draft in drafts)
        {
            List<ContentBlock> blocks = new(draft.Blocks.Count);
            foreach (BlockDraft block in draft.Blocks)
            {
                blocks.Add(ResolveBlock(block, firstSectionByPath, anchors, recoverLinkIntegrity, recoveryIssues));
            }

            sections.Add(
                new ReadingSection(
                    draft.Id,
                    blocks,
                    draft.Title,
                    draft.IsLinear ? ReadingSectionRole.Primary : ReadingSectionRole.Supplementary));
        }

        return sections;
    }

    private static ContentBlock ResolveBlock(
        BlockDraft draft,
        Dictionary<string, SectionId> firstSectionByPath,
        Dictionary<string, ReadingLocation> anchors,
        bool recoverLinkIntegrity,
        List<EpubContentRecoveryIssue> recoveryIssues) =>
        draft.Kind switch
        {
            BlockDraftKind.Paragraph => new ParagraphBlock(draft.Id, ResolveInlines(draft.Inlines, firstSectionByPath, anchors, recoverLinkIntegrity, recoveryIssues)),
            BlockDraftKind.Heading => new HeadingBlock(draft.Id, draft.LevelOrDepth, ResolveInlines(draft.Inlines, firstSectionByPath, anchors, recoverLinkIntegrity, recoveryIssues)),
            BlockDraftKind.Quote => new QuoteBlock(draft.Id, ResolveInlines(draft.Inlines, firstSectionByPath, anchors, recoverLinkIntegrity, recoveryIssues), draft.LevelOrDepth),
            BlockDraftKind.ListItem => new ListItemBlock(
                draft.Id,
                draft.ListKind,
                ResolveInlines(draft.Inlines, firstSectionByPath, anchors, recoverLinkIntegrity, recoveryIssues),
                draft.LevelOrDepth,
                draft.Ordinal),
            BlockDraftKind.Preformatted => new PreformattedBlock(draft.Id, draft.Text ?? string.Empty),
            BlockDraftKind.Image => new ImageBlock(
                draft.Id,
                new ResourceId(draft.ImageManifestId ?? throw Error(EpubContentErrorCode.InvalidContent, "Image draft senza resource id.")),
                draft.AlternativeText,
                draft.Caption),
            BlockDraftKind.ThematicBreak => new ThematicBreakBlock(draft.Id),
            _ => throw Error(EpubContentErrorCode.InvalidContent, $"Tipo block draft non supportato: {draft.Kind}."),
        };

    private static List<InlineContent> ResolveInlines(
        List<InlineDraft> drafts,
        Dictionary<string, SectionId> firstSectionByPath,
        Dictionary<string, ReadingLocation> anchors,
        bool recoverLinkIntegrity,
        List<EpubContentRecoveryIssue> recoveryIssues)
    {
        List<InlineContent> result = new(drafts.Count);
        foreach (InlineDraft draft in drafts)
        {
            switch (draft)
            {
                case TextDraft text:
                    result.Add(new TextRun(text.Text));
                    break;
                case LineBreakDraft:
                    result.Add(LineBreakInline.Instance);
                    break;
                case EmphasisDraft emphasis:
                    result.Add(new EmphasisSpan(ResolveInlines(emphasis.Content, firstSectionByPath, anchors, recoverLinkIntegrity, recoveryIssues)));
                    break;
                case StrongDraft strong:
                    result.Add(new StrongSpan(ResolveInlines(strong.Content, firstSectionByPath, anchors, recoverLinkIntegrity, recoveryIssues)));
                    break;
                case LinkDraft link:
                    ResolveLink(result, link, firstSectionByPath, anchors, recoverLinkIntegrity, recoveryIssues);
                    break;
            }
        }

        return result;
    }

    private static HyperlinkRole GetHyperlinkRole(IElement element)
    {
        string? semanticType = element.GetAttribute("epub:type");
        if (string.IsNullOrWhiteSpace(semanticType))
        {
            return HyperlinkRole.Generic;
        }

        foreach (string token in semanticType.Split(SemanticTypeSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(token, "noteref", StringComparison.OrdinalIgnoreCase))
            {
                return HyperlinkRole.NoteReference;
            }
        }

        return HyperlinkRole.Generic;
    }

    private static void ResolveLink(
        List<InlineContent> target,
        LinkDraft link,
        Dictionary<string, SectionId> firstSectionByPath,
        Dictionary<string, ReadingLocation> anchors,
        bool recoverLinkIntegrity,
        List<EpubContentRecoveryIssue> recoveryIssues)
    {
        List<InlineContent> content = ResolveInlines(
            link.Content,
            firstSectionByPath,
            anchors,
            recoverLinkIntegrity,
            recoveryIssues);

        if (Uri.TryCreate(link.Href, UriKind.Absolute, out Uri? absoluteUri))
        {
            if (ExternalLinkPolicy.IsAllowed(absoluteUri))
            {
                target.Add(new HyperlinkSpan(new ExternalLinkTarget(absoluteUri), content));
                return;
            }

            target.AddRange(content);
            if (recoverLinkIntegrity)
            {
                recoveryIssues.Add(new EpubContentRecoveryIssue(
                    EpubContentRecoveryKind.UnsafeExternalHyperlinkSuppressed,
                    $"Link esterno '{link.Href}' reso non azionabile: lo schema URI non appartiene alla allow-list http/https/mailto e non viene delegato al sistema operativo."));
            }

            return;
        }

        OcfPath path;
        string? fragment;
        try
        {
            (path, fragment) = ResolveLocalReference(link.SourcePath, link.Href);
        }
        catch (EpubContentException exception) when (recoverLinkIntegrity && exception.ErrorCode == EpubContentErrorCode.InvalidLocalReference)
        {
            PreserveBrokenInternalLink(target, content, link, recoveryIssues, exception.Message);
            return;
        }

        if (!firstSectionByPath.TryGetValue(path.Value, out SectionId? sectionId))
        {
            target.AddRange(content);
            if (recoverLinkIntegrity)
            {
                recoveryIssues.Add(new EpubContentRecoveryIssue(
                    EpubContentRecoveryKind.InternalHyperlinkDropped,
                    BrokenInternalLinkMessage(
                        link,
                        $"la risorsa target '{path.Value}' non appartiene al reading order navigabile")));
            }

            return;
        }

        ReadingLocation location;
        try
        {
            location = fragment is null
                ? ReadingLocation.AtSectionStart(sectionId)
                : FindAnchor(path, fragment, anchors);
        }
        catch (EpubContentException exception) when (recoverLinkIntegrity && exception.ErrorCode == EpubContentErrorCode.InternalTargetNotFound)
        {
            PreserveBrokenInternalLink(target, content, link, recoveryIssues, exception.Message);
            return;
        }

        target.Add(new HyperlinkSpan(new InternalLinkTarget(location), content, link.Role));
    }

    private static void PreserveBrokenInternalLink(
        List<InlineContent> target,
        List<InlineContent> content,
        LinkDraft link,
        List<EpubContentRecoveryIssue> recoveryIssues,
        string reason)
    {
        target.AddRange(content);
        recoveryIssues.Add(new EpubContentRecoveryIssue(
            EpubContentRecoveryKind.InternalHyperlinkDropped,
            BrokenInternalLinkMessage(link, reason)));
    }

    private static string BrokenInternalLinkMessage(LinkDraft link, string reason)
    {
        string kind = link.Role == HyperlinkRole.NoteReference ? "Rimando nota" : "Link interno";
        return $"{kind} '{link.Href}' reso non azionabile: {reason}. Il testo resta leggibile e nessun target esterno al package viene cercato.";
    }

    private static TableOfContents BuildTableOfContents(
        IReadOnlyList<EpubNavigationNode> source,
        Dictionary<string, SectionId> firstSectionByPath,
        Dictionary<string, ReadingLocation> anchors)
    {
        List<NavigationItem> items = new(source.Count);
        foreach (EpubNavigationNode node in source)
        {
            List<NavigationItem> children = BuildNavigationItems(node.Children, firstSectionByPath, anchors);
            ReadingLocation? target = node.Target is null
                ? null
                : ResolveNavigationLocation(node.Target, firstSectionByPath, anchors);
            items.Add(new NavigationItem(node.Label, target, children));
        }

        return new TableOfContents(items);
    }

    private static List<NavigationItem> BuildNavigationItems(
        IReadOnlyList<EpubNavigationNode> source,
        Dictionary<string, SectionId> firstSectionByPath,
        Dictionary<string, ReadingLocation> anchors)
    {
        List<NavigationItem> items = new(source.Count);
        foreach (EpubNavigationNode node in source)
        {
            List<NavigationItem> children = BuildNavigationItems(node.Children, firstSectionByPath, anchors);
            ReadingLocation? target = node.Target is null
                ? null
                : ResolveNavigationLocation(node.Target, firstSectionByPath, anchors);
            items.Add(new NavigationItem(node.Label, target, children));
        }

        return items;
    }

    private static TableOfContents BuildTableOfContentsRecovering(
        IReadOnlyList<EpubNavigationNode> source,
        Dictionary<string, SectionId> firstSectionByPath,
        Dictionary<string, ReadingLocation> anchors,
        List<EpubContentRecoveryIssue> recoveryIssues) =>
        new(BuildNavigationItemsRecovering(source, firstSectionByPath, anchors, recoveryIssues));

    private static List<NavigationItem> BuildNavigationItemsRecovering(
        IReadOnlyList<EpubNavigationNode> source,
        Dictionary<string, SectionId> firstSectionByPath,
        Dictionary<string, ReadingLocation> anchors,
        List<EpubContentRecoveryIssue> recoveryIssues)
    {
        List<NavigationItem> items = new(source.Count);
        foreach (EpubNavigationNode node in source)
        {
            List<NavigationItem> children = BuildNavigationItemsRecovering(
                node.Children,
                firstSectionByPath,
                anchors,
                recoveryIssues);
            ReadingLocation? target = null;
            if (node.Target is not null)
            {
                try
                {
                    target = ResolveNavigationLocation(node.Target, firstSectionByPath, anchors);
                }
                catch (EpubContentException exception) when (
                    exception.ErrorCode == EpubContentErrorCode.InternalTargetNotFound ||
                    exception.ErrorCode == EpubContentErrorCode.InvalidLocalReference)
                {
                    recoveryIssues.Add(new EpubContentRecoveryIssue(
                        EpubContentRecoveryKind.NavigationTargetDropped,
                        $"Voce TOC '{node.Label}' con target non risolvibile: {exception.Message} " +
                        (children.Count == 0
                            ? "La voce foglia viene omessa; gli altri target validi restano disponibili."
                            : "La voce resta come gruppo non navigabile perché contiene figli validi.")));
                }
            }

            if (target is null && children.Count == 0)
            {
                continue;
            }

            items.Add(new NavigationItem(node.Label, target, children));
        }

        return items;
    }

    private static ReadingLocation ResolveNavigationLocation(
        EpubNavigationTarget target,
        Dictionary<string, SectionId> firstSectionByPath,
        Dictionary<string, ReadingLocation> anchors)
    {
        if (!firstSectionByPath.TryGetValue(target.LocalPath.Value, out SectionId? sectionId))
        {
            throw Error(
                EpubContentErrorCode.InternalTargetNotFound,
                $"Il target TOC '{target.LocalPath.Value}' non corrisponde a una sezione del reading order.");
        }

        return target.Fragment is null
            ? ReadingLocation.AtSectionStart(sectionId)
            : FindAnchor(target.LocalPath, target.Fragment, anchors);
    }

    private static ReadingLocation FindAnchor(
        OcfPath path,
        string fragment,
        Dictionary<string, ReadingLocation> anchors)
    {
        string key = AnchorKey(path, fragment);
        if (!anchors.TryGetValue(key, out ReadingLocation? location))
        {
            throw Error(
                EpubContentErrorCode.InternalTargetNotFound,
                $"Anchor XHTML non trovato: '{path.Value}#{fragment}'.");
        }

        return location;
    }

    private static BookMetadata BuildMetadata(EpubPackageMetadata source)
    {
        string title = source.Titles[0];
        List<string> languages = source.Languages.ToList();
        List<BookContributor> contributors = [];
        List<BookIdentifier> identifiers = [];
        List<string> subjects = [];
        string? description = null;
        string? publisher = null;
        string? rights = null;

        foreach (EpubDublinCoreMetadata item in source.DublinCore)
        {
            switch (item.Name)
            {
                case "creator":
                    contributors.Add(new BookContributor(item.Value, MapContributorRole(item.Role, ContributorRole.Author), item.FileAs));
                    break;
                case "contributor":
                    contributors.Add(new BookContributor(item.Value, MapContributorRole(item.Role, ContributorRole.Other), item.FileAs));
                    break;
                case "identifier":
                    identifiers.Add(new BookIdentifier(item.Value, item.Scheme));
                    break;
                case "subject":
                    subjects.Add(item.Value);
                    break;
                case "description" when description is null:
                    description = item.Value;
                    break;
                case "publisher" when publisher is null:
                    publisher = item.Value;
                    break;
                case "rights" when rights is null:
                    rights = item.Value;
                    break;
            }
        }

        return new BookMetadata(
            title,
            languages: languages,
            contributors: contributors,
            identifiers: identifiers,
            description: description,
            publisher: publisher,
            subjects: subjects,
            rights: rights);
    }

    private static ContributorRole MapContributorRole(string? role, ContributorRole fallback)
    {
        string? value = role?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return fallback;
        }

        if (string.Equals(value, "aut", StringComparison.OrdinalIgnoreCase))
        {
            return ContributorRole.Author;
        }

        if (string.Equals(value, "edt", StringComparison.OrdinalIgnoreCase))
        {
            return ContributorRole.Editor;
        }

        if (string.Equals(value, "trl", StringComparison.OrdinalIgnoreCase))
        {
            return ContributorRole.Translator;
        }

        if (string.Equals(value, "ill", StringComparison.OrdinalIgnoreCase))
        {
            return ContributorRole.Illustrator;
        }

        if (string.Equals(value, "nrt", StringComparison.OrdinalIgnoreCase))
        {
            return ContributorRole.Narrator;
        }

        return ContributorRole.Other;
    }

    private static List<BookResource> BuildResources(IReadOnlyList<EpubManifestItem> manifest)
    {
        List<BookResource> resources = [];
        foreach (EpubManifestItem item in manifest)
        {
            if (item.LocalPath is null)
            {
                continue;
            }

            resources.Add(
                new BookResource(
                    new ResourceId(item.Id),
                    MapResourceKind(item.MediaType),
                    item.MediaType,
                    item.LocalPath.Value));
        }

        return resources;
    }

    private static ResourceKind MapResourceKind(string mediaType)
    {
        if (mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceKind.Image;
        }

        if (string.Equals(mediaType, "text/css", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceKind.Stylesheet;
        }

        if (mediaType.StartsWith("font/", StringComparison.OrdinalIgnoreCase) ||
            mediaType.Contains("font", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mediaType, "application/vnd.ms-opentype", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceKind.Font;
        }

        if (mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceKind.Audio;
        }

        if (mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceKind.Video;
        }

        return ResourceKind.Other;
    }

    private static EpubManifestItem ResolveXhtmlSpineItem(
        EpubSpineItem spineItem,
        Dictionary<string, EpubManifestItem> manifestById)
    {
        EpubManifestItem current = manifestById[spineItem.IdRef];
        HashSet<string> visited = new(StringComparer.Ordinal);
        while (visited.Add(current.Id))
        {
            if (string.Equals(current.MediaType, XhtmlMediaType, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            if (current.FallbackId is null)
            {
                break;
            }

            current = manifestById[current.FallbackId];
        }

        throw Error(
            EpubContentErrorCode.UnsupportedSpineContent,
            $"Lo spine item '{spineItem.IdRef}' non risolve a un Content Document application/xhtml+xml.");
    }

    private static string ReadBoundedText(Stream stream, OcfPath sourcePath)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[16 * 1024];
        int total = 0;
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            total += read;
            if (total > EpubContentLimits.MaxContentDocumentBytes)
            {
                throw Error(
                    EpubContentErrorCode.ContentDocumentTooLarge,
                    $"Il Content Document '{sourcePath.Value}' supera {EpubContentLimits.MaxContentDocumentBytes} byte.");
            }

            buffer.Write(chunk, 0, read);
        }

        byte[] bytes = buffer.ToArray();
        try
        {
            string text;
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                text = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true)
                    .GetString(bytes, 2, bytes.Length - 2);
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                text = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true)
                    .GetString(bytes, 2, bytes.Length - 2);
            }
            else
            {
                int offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
                text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                    .GetString(bytes, offset, bytes.Length - offset);
            }

            ValidateXmlCharacterRange(text, sourcePath);
            return text;
        }
        catch (DecoderFallbackException exception)
        {
            throw new EpubContentException(
                EpubContentErrorCode.InvalidXhtml,
                $"Il Content Document '{sourcePath.Value}' contiene una sequenza di byte non valida per UTF-8/UTF-16.",
                exception);
        }
    }

    private static void ValidateXmlCharacterRange(string text, OcfPath sourcePath)
    {
        foreach (char value in text)
        {
            if (value < 0x20 && value != '\t' && value != '\n' && value != '\r')
            {
                throw Error(
                    EpubContentErrorCode.InvalidXhtml,
                    $"Il Content Document '{sourcePath.Value}' contiene caratteri di controllo non ammessi in XHTML/XML.");
            }
        }
    }

    private static void ValidateNodeBudget(IElement body, OcfPath sourcePath)
    {
        int count = 0;
        Stack<(INode Node, int Depth)> stack = new();
        stack.Push((body, 1));
        while (stack.Count > 0)
        {
            (INode node, int depth) = stack.Pop();
            if (depth > EpubContentLimits.MaxTreeDepth)
            {
                throw Error(
                    EpubContentErrorCode.ContentDepthExceeded,
                    $"Il Content Document '{sourcePath.Value}' supera la profondità massima {EpubContentLimits.MaxTreeDepth}.");
            }

            count++;
            if (count > EpubContentLimits.MaxNodesPerDocument)
            {
                throw Error(
                    EpubContentErrorCode.TooManyContentNodes,
                    $"Il Content Document '{sourcePath.Value}' supera {EpubContentLimits.MaxNodesPerDocument} nodi.");
            }

            foreach (INode child in node.ChildNodes)
            {
                stack.Push((child, depth + 1));
            }
        }
    }

    private static BlockId NextBlockId(SectionDraft section) =>
        new($"b{section.Blocks.Count + 1:D6}");

    private static void AddBlock(SectionDraft section, BlockDraft block)
    {
        if (section.Blocks.Count >= EpubContentLimits.MaxBlocksPerDocument)
        {
            throw Error(
                EpubContentErrorCode.TooManyBlocks,
                $"Il Content Document '{section.SourcePath.Value}' supera {EpubContentLimits.MaxBlocksPerDocument} blocchi.");
        }

        section.Blocks.Add(block);
    }

    private static bool IsBlockElement(IElement element) => BlockElementNames.Contains(element.LocalName);

    private static void EnsureDepth(int depth)
    {
        if (depth > EpubContentLimits.MaxTreeDepth)
        {
            throw Error(
                EpubContentErrorCode.ContentDepthExceeded,
                $"La conversione XHTML supera la profondità massima {EpubContentLimits.MaxTreeDepth}.");
        }
    }

    private static int CountAncestorQuotes(IElement element)
    {
        int depth = 1;
        IElement? current = element.ParentElement;
        while (current is not null)
        {
            if (current.LocalName == "blockquote")
            {
                depth++;
            }

            current = current.ParentElement;
        }

        return depth;
    }

    private static int? ParsePositiveInteger(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) && result > 0)
        {
            return result;
        }

        return null;
    }

    private static void RegisterElementAnchor(
        IElement element,
        SectionId sectionId,
        BlockId? blockId,
        int characterOffset,
        OcfPath sourcePath,
        Dictionary<string, ReadingLocation> anchors)
    {
        string? id = OptionalId(element.Id);
        if (id is null)
        {
            return;
        }

        ReadingLocation location = blockId is null
            ? ReadingLocation.AtSectionStart(sectionId)
            : new ReadingLocation(sectionId, blockId, characterOffset);
        RegisterAnchor(sourcePath, id, location, anchors);
    }

    private static void RegisterAnchor(
        OcfPath sourcePath,
        string id,
        ReadingLocation location,
        Dictionary<string, ReadingLocation> anchors)
    {
        string key = AnchorKey(sourcePath, id);
        if (!anchors.TryAdd(key, location))
        {
            throw Error(
                EpubContentErrorCode.DuplicateAnchor,
                $"Anchor XHTML duplicato: '{sourcePath.Value}#{id}'.");
        }
    }

    private static string AnchorKey(OcfPath path, string fragment) => $"{path.Value}#{fragment}";

    private static (OcfPath Path, string? Fragment) ResolveLocalReference(OcfPath sourcePath, string href)
    {
        int hashIndex = href.IndexOf('#');
        string pathPart = hashIndex < 0 ? href : href[..hashIndex];
        string? fragment = hashIndex < 0 ? null : DecodeFragment(href[(hashIndex + 1)..]);
        if (pathPart.Contains('?'))
        {
            throw Error(EpubContentErrorCode.InvalidLocalReference, $"Query non supportata nel link locale '{href}'.");
        }

        OcfPath path = pathPart.Length == 0 ? sourcePath : ResolveLocalPath(sourcePath, pathPart, allowFragment: false);
        return (path, fragment);
    }

    private static OcfPath ResolveLocalPath(OcfPath sourcePath, string reference, bool allowFragment)
    {
        string pathPart = reference;
        if (!allowFragment && (reference.Contains('#') || reference.Contains('?')))
        {
            throw Error(EpubContentErrorCode.InvalidLocalReference, $"Riferimento locale non valido: '{reference}'.");
        }

        int slash = sourcePath.Value.LastIndexOf('/');
        string baseDirectory = slash < 0 ? string.Empty : sourcePath.Value[..slash];
        string combined = baseDirectory.Length == 0 ? pathPart : $"{baseDirectory}/{pathPart}";
        try
        {
            return OcfPath.FromContainerReference(combined);
        }
        catch (EpubContainerException exception)
        {
            throw new EpubContentException(
                EpubContentErrorCode.InvalidLocalReference,
                $"Riferimento locale non valido da '{sourcePath.Value}': '{reference}'.",
                exception);
        }
    }

    private static string DecodeFragment(string raw)
    {
        ValidatePercentEscapes(raw);
        try
        {
            return Uri.UnescapeDataString(raw);
        }
        catch (UriFormatException exception)
        {
            throw new EpubContentException(
                EpubContentErrorCode.InvalidLocalReference,
                $"Fragment percent-encoded non valido: '{raw}'.",
                exception);
        }
    }

    private static void ValidatePercentEscapes(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != '%')
            {
                continue;
            }

            if (index + 2 >= value.Length || !IsHex(value[index + 1]) || !IsHex(value[index + 2]))
            {
                throw Error(EpubContentErrorCode.InvalidLocalReference, $"Escape percentuale non valido: '{value}'.");
            }

            index += 2;
        }
    }

    private static bool IsHex(char value) =>
        value is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';

    private static string? OptionalId(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;

    private static string? OptionalAttributeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string PlainText(List<InlineDraft> content)
    {
        StringBuilder builder = new();
        AppendPlainText(builder, content);
        return builder.ToString();
    }

    private static void AppendPlainText(StringBuilder builder, List<InlineDraft> content)
    {
        foreach (InlineDraft inline in content)
        {
            switch (inline)
            {
                case TextDraft text:
                    builder.Append(text.Text);
                    break;
                case LineBreakDraft:
                    builder.Append('\n');
                    break;
                case EmphasisDraft emphasis:
                    AppendPlainText(builder, emphasis.Content);
                    break;
                case StrongDraft strong:
                    AppendPlainText(builder, strong.Content);
                    break;
                case LinkDraft link:
                    AppendPlainText(builder, link.Content);
                    break;
            }
        }
    }

    private static EpubContentException Error(EpubContentErrorCode code, string message) => new(code, message);

    private sealed class InlineParseState
    {
        public InlineParseState(
            SectionId sectionId,
            BlockId blockId,
            OcfPath sourcePath,
            Dictionary<string, ReadingLocation> anchors)
        {
            SectionId = sectionId;
            BlockId = blockId;
            SourcePath = sourcePath;
            Anchors = anchors;
        }

        public SectionId SectionId { get; }

        public BlockId BlockId { get; }

        public OcfPath SourcePath { get; }

        public Dictionary<string, ReadingLocation> Anchors { get; }

        public int Length { get; set; }

        public bool HasText { get; set; }

        public bool LastCharacterWasSpace { get; set; }

        public bool PendingSpace { get; set; }
    }
}

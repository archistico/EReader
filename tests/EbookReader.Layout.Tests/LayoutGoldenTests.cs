using System.Text;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Resources;

namespace EbookReader.Layout.Tests;

public sealed class LayoutGoldenTests
{
    [Fact]
    public void Golden40x10()
    {
        Assert.Equal(Expected40x10, Render(DeterministicLayoutEngine.Layout(CreateSection(), new LayoutViewport(40, 10))));
    }

    [Fact]
    public void Golden80x24()
    {
        Assert.Equal(Expected80x24, Render(DeterministicLayoutEngine.Layout(CreateSection(), new LayoutViewport(80, 24))));
    }

    [Fact]
    public void Golden120x40()
    {
        Assert.Equal(Expected120x40, Render(DeterministicLayoutEngine.Layout(CreateSection(), new LayoutViewport(120, 40))));
    }

    private static ReadingSection CreateSection() =>
        new(
            new SectionId("chapter"),
            [
                new HeadingBlock(new BlockId("heading"), 1, [new TextRun("Layout deterministico")]),
                new ParagraphBlock(
                    new BlockId("paragraph"),
                    [new TextRun("Il motore dispone il testo Unicode 😀 senza separare i grafemi e mantiene stabile la spaziatura tra i blocchi.")]),
                new QuoteBlock(
                    new BlockId("quote"),
                    [new TextRun("Una citazione annidata conserva il proprio prefisso durante il wrapping.")],
                    depth: 2),
                new ListItemBlock(
                    new BlockId("list-one"),
                    ListKind.Unordered,
                    [new TextRun("Primo elemento con contenuto abbastanza lungo da andare a capo.")]),
                new ListItemBlock(
                    new BlockId("list-two"),
                    ListKind.Ordered,
                    [new TextRun("Secondo elemento.")],
                    depth: 2,
                    ordinal: 7),
                new PreformattedBlock(new BlockId("pre"), "alpha\tbeta\n  gamma"),
                new ImageBlock(new BlockId("image"), new ResourceId("cover"), "Copertina", "Esempio"),
                new ThematicBreakBlock(new BlockId("break")),
            ]);

    private static string Render(BookLayout layout)
    {
        StringBuilder result = new();
        foreach (LayoutPage page in layout.Pages)
        {
            result.Append("--- pagina ");
            result.Append(page.Number);
            result.AppendLine(" ---");
            foreach (VisualLine line in page.Lines)
            {
                result.AppendLine(line.Text);
            }
        }

        return result.ToString().ReplaceLineEndings("\n").TrimEnd('\n');
    }

    private const string Expected40x10 = """
--- pagina 1 ---
Layout deterministico

Il motore dispone il testo Unicode 😀
senza separare i grafemi e mantiene
stabile la spaziatura tra i blocchi.

> > Una citazione annidata conserva il
> > proprio prefisso durante il
> > wrapping.

--- pagina 2 ---
- Primo elemento con contenuto
  abbastanza lungo da andare a capo.
  7. Secondo elemento.

alpha   beta
  gamma

[Immagine: Copertina — Esempio]

---
""";

    private const string Expected80x24 = """
--- pagina 1 ---
Layout deterministico

Il motore dispone il testo Unicode 😀 senza separare i grafemi e mantiene
stabile la spaziatura tra i blocchi.

> > Una citazione annidata conserva il proprio prefisso durante il wrapping.

- Primo elemento con contenuto abbastanza lungo da andare a capo.
  7. Secondo elemento.

alpha   beta
  gamma

[Immagine: Copertina — Esempio]

---
""";

    private const string Expected120x40 = """
--- pagina 1 ---
Layout deterministico

Il motore dispone il testo Unicode 😀 senza separare i grafemi e mantiene stabile la spaziatura tra i blocchi.

> > Una citazione annidata conserva il proprio prefisso durante il wrapping.

- Primo elemento con contenuto abbastanza lungo da andare a capo.
  7. Secondo elemento.

alpha   beta
  gamma

[Immagine: Copertina — Esempio]

---
""";
}

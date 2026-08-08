# M3.4 — Images

M3.4 keeps EReader a terminal-first EPUB reader while making illustrations inspectable on demand.

## Reading model

An EPUB image remains represented in the format-neutral Domain as:

```text
ImageBlock
  ResourceId
  AlternativeText?
  Caption?

BookResource
  ResourceId
  ResourceKind.Image
  MediaType
  Name?
```

`BookResource` still has no byte payload. The layout therefore remains deterministic and independent from the source format or filesystem.

The body continues to display the established fallback:

```text
[Immagine]
[Immagine: testo alternativo]
[Immagine: testo alternativo — didascalia]
```

When the current `ReadingLocation` belongs to an `ImageBlock`, the header also exposes its media type and the normal footer adds:

```text
Enter immagine
```

## Opening an image

Press `Enter` while positioned on the image block.

Pipeline:

```text
ReadingLocation
  ↓
ReaderSession.CurrentImage
  ↓ ResourceId
EpubImageResourceReader
  ↓ bounded byte payload
ExternalImagePreviewService
  ↓ private temporary file
OS-associated viewer
```

No image is loaded simply because it is visible on screen. Preview only occurs after the explicit key press.

## Supported preview media types

```text
image/jpeg   → .jpg
image/png    → .png
image/gif    → .gif
image/webp   → .webp
```

The extension is derived from the manifest `media-type`, not from the EPUB path/name.

M3.4 deliberately does **not** launch:

- `image/svg+xml`;
- remote image URLs;
- non-image manifest resources;
- resources larger than 16 MiB.

SVG is excluded because an OS may route it to a browser and SVG can contain active/external constructs. It still remains visible through its text placeholder/alternative text.

## Temporary files

The CLI creates a per-reader private directory below the operating-system temporary directory only after the preview command. Files are numbered and use the safe extension selected from the media type.

When the TUI closes EReader attempts to remove the temporary directory recursively. Cleanup is best-effort: on systems where the external viewer still locks the file, the failure is ignored rather than turning a successful reading session into an error.

This is not EPUB archive extraction: the EPUB adapter still never calls `ExtractToDirectory`/`ExtractToFile`; one explicitly requested, bounded resource is copied by the outer CLI adapter for interoperability with an external application.

## Persistence

M3.4 non modificava gli schemi allora correnti (`state.json` schema 3 e `config.json` schema 1). Da M3.7 lo stato di lettura è evoluto a schema 4 per highlight/note, mentre il contratto immagini resta invariato e `config.json` rimane schema 1.

No image bytes, temporary path, media viewer state or image layout coordinate is persisted.

## Interaction with existing features

- Search remains pre-layout text search. Alternative text/caption participate exactly as before through `ContentText`.
- Stable progress continues to use logical UTF-16 text; image preview does not change its weight.
- Bookmarking an image persists only its `ReadingLocation`.
- Resize/reflow preserves the same location and does not reopen the image.
- Themes affect only the textual placeholder/chrome, not the external viewer.
- `--plain` remains text-only and never starts another process.

See ADR-0048.

## M3.10 — Immagini mancanti e recovery

M3.10 distingue la presenza logica nel manifest dalla disponibilità fisica nel contenitore. Se un'immagine referenziata dal testo è dichiarata nel manifest ma manca dal file EPUB, il libro resta leggibile: il Domain conserva l'`ImageBlock` e il renderer può mostrare placeholder/alt text, mentre la validazione emette `ER-EPUB-RECOVERY-RESOURCE-001`. La preview esplicita della risorsa restituisce invece `ResourceNotFound`. Una risorsa opzionale diversa e mancante non deve impedire l'apertura di un'immagine presente. Non viene effettuato alcun download o lookup esterno.

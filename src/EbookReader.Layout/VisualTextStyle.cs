namespace EbookReader.Layout;

/// <summary>
/// Format-neutral inline emphasis hints preserved by the layout engine.
/// Terminal colors remain the responsibility of the outer UI adapter.
/// </summary>
[Flags]
public enum VisualTextStyle : byte
{
    None = 0,
    Strong = 1,
    Emphasis = 2,
    StrongEmphasis = Strong | Emphasis,
}

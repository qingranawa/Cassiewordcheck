namespace CassieWordCheck.Models;

/// <summary>
/// WinUI 结果面板使用的无 UI 结果片段喵
/// </summary>
public sealed record ResultSegment(
    string Text,
    CheckStatus Status,
    bool IsInteractive,
    string? Word);

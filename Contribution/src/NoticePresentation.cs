namespace Restitutor.Contribution;

internal sealed record NoticeRow(string City, string Line, int Kind);
internal static class NoticePresentation
{
    // Stable partition across every pending notice; saved notices are never reordered.
    internal static NoticeRow[] Rows(IEnumerable<Notice> notices) => notices
        .SelectMany(n => n.Lines.Select(line => new NoticeRow(n.City, line,
            line.StartsWith("교역품 확인 · ", StringComparison.Ordinal) ? 0 :
            line.StartsWith("해금 · ", StringComparison.Ordinal) ? 1 : 2)))
        .OrderBy(row => row.Kind).ToArray();
}

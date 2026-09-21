namespace Restitutor.TabCharacters;

internal static class BookRules
{
    internal static bool Matches(int bookType, int bookSkill, int selectedSkill) =>
        selectedSkill < 0 ? bookType != 1 : bookType == 1 && bookSkill == selectedSkill;
    internal static bool CanCommit(long expectedRole, long currentRole, long guid, int count, int status, bool busy) =>
        expectedRole == currentRole && guid != 0 && count > 0 && status == 0 && !busy;
}

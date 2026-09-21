// Pagination regression suite isolates the popup adapter. The adapter itself runs in PopupTests.csproj.
using Il2CppClient.UILogic.UICharacter;
namespace Restitutor.TabCharacters;
public sealed partial class EntryPoint
{
    private static bool BooksOpen => false;
    private void InstallBooks() { }
    private static void CloseBooks() { }
    private static void ClearBookBindings() { }
    private static void RefreshBookBindings(UICharacterView view) { }
    private static void TickBooks() { }
}

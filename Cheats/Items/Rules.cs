namespace Restitutor.Cheats.Items;

internal enum Tab { Equipment, Book, DevilFruit }
internal sealed record Entry(int Id,string Name,string TypeName,int Type);

// Pure rules (no game types) so items-tests runs without the game.
internal static class Rules {
    // ItemType table (r11 chunk 85): 31 무기, 32 방어구, 33 의상, 34 도구 (parent 3 = 장비).
    // 35/36 (항해·탐험 도구) are consumables (parent 1000) and are not character equipment.
    internal static readonly int[] EquipmentTypes={31,32,33,34};
    // 71 서적(읽을거리), 72 스킬북, 73 언어 서적 (parent 2000).
    internal static readonly int[] BookTypes={72,73,71};
    internal static Tab? TabOf(int type,int id) {
        if(Array.IndexOf(DevilFruits.Ids,id)>=0) return Tab.DevilFruit;
        if(Array.IndexOf(EquipmentTypes,type)>=0) return Tab.Equipment;
        if(Array.IndexOf(BookTypes,type)>=0) return Tab.Book;
        return null;
    }
    internal static int TypeOrder(Tab tab,int type) {
        var order=tab==Tab.Equipment?EquipmentTypes:tab==Tab.Book?BookTypes:Array.Empty<int>();
        int i=Array.IndexOf(order,type);return i<0?99:i;
    }
    internal static List<Entry> Sort(Tab tab,IEnumerable<Entry> items)=>
        items.OrderBy(e=>TypeOrder(tab,e.Type)).ThenBy(e=>e.Id).ToList();
    // Case-insensitive name match; a digits-only query also matches the item id.
    internal static List<Entry> Filter(IReadOnlyList<Entry> items,string? query) {
        string q=(query??"").Trim();
        if(q.Length==0) return items.ToList();
        bool digits=q.All(char.IsDigit);
        return items.Where(e=>e.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||(digits&&e.Id.ToString().Contains(q))).ToList();
    }
    internal const int PerColumn=8,Columns=2,PageSize=PerColumn*Columns;
    internal static int Pages(int count)=>Math.Max(1,(count+PageSize-1)/PageSize);
    internal static int ClampPage(int page,int count)=>Math.Clamp(page,0,Pages(count)-1);
    internal static IEnumerable<T> PageOf<T>(IReadOnlyList<T> items,int page)=>items.Skip(ClampPage(page,items.Count)*PageSize).Take(PageSize);
    // Layout (panel coordinates, 640 wide, two columns of 310 with a 20 gap).
    internal const float PanelWidth=640,ColumnWidth=310,ColumnGap=20,TabsTop=42,SearchTop=84,ListTop=126,RowHeight=34,PagerHeight=40,Footer=34;
    internal static float Height=>ListTop+PerColumn*RowHeight+PagerHeight+Footer;
    internal static (float x,float y) Slot(int index)=>(index<PerColumn?0:ColumnWidth+ColumnGap,(index%PerColumn)*RowHeight);
}

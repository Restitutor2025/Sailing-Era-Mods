using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.PlayerStore.Flatbuffers;
using Il2CppFlatBuffers;
using System.Security.Cryptography;
using System.Text.Json;
namespace Restitutor.Contribution;

// Sidecars are matched to the entire serialized game payload, never merely a save slot.
internal static class SaveState
{
    internal sealed class Snapshot
    {
        public int Version { get; set; } = 3;
        public List<int> ReachedHundred { get; set; } = new();
        public List<PendingPort> Pending { get; set; } = new();
        public List<Notice> Notices { get; set; } = new();
    }
    private static string Key(PlayerDataSerializer serializer)
    {
        var builder = new FlatBufferBuilder(1024);
        var offset = FBPlayerData.Pack(builder, serializer.PlayerDataT);
        builder.Finish(offset.Value);
        byte[] bytes = builder.SizedByteArray().ToArray();
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
    private static string DirectoryPath => Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "Restitutor", "Contribution");
    internal static void Save(PlayerDataSerializer serializer)
    {
        try
        {
            var snapshot = new Snapshot {
                Pending = EntryPoint.Journal.Ports.Values.ToList(), Notices = EntryPoint.Journal.Notices.ToList(),
                ReachedHundred = EntryPoint.Journal.ReachedHundred.ToList()
            };
            string json = JsonSerializer.Serialize(snapshot);
            string path = Path.Combine(DirectoryPath, Key(serializer) + ".json");
            Directory.CreateDirectory(DirectoryPath);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, json); File.Move(temporary, path, true);
        }
        catch (Exception ex) { EntryPoint.Error("Contribution sidecar save failed", ex); }
    }
    internal static void Load(PlayerDataSerializer serializer)
    {
        try
        {
            string path = Path.Combine(DirectoryPath, Key(serializer) + ".json");
            if (!File.Exists(path)) return;
            var state = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path));
            if (state == null || state.Version is not (1 or 2 or 3)) throw new InvalidDataException("Unknown sidecar version");
            if(state.Version==3)EntryPoint.Journal.ReachedHundred.UnionWith(state.ReachedHundred);
            foreach (var old in state.Pending) {
                if(state.Version<3){old.RevealGoods=false;old.Lines.Clear();old.UnknownGoods.Clear();}
                var p=Rules.GrantsOnly(old);
                if(p==null)continue;
                if(EntryPoint.Player!.WorldPort.GetPortData(p.Port)==null)throw new InvalidDataException("Missing pending port");
                if(p.Benefits.Keys.Any(id=>Il2CppGyyx.Template.TemplateManager.GetGovHouseLicence(id)==null))throw new InvalidDataException("Missing grant template");
                EntryPoint.Journal.Ports[p.Port]=p;
            }
            // v1 mixed notices and decline-only state are deliberately not restored.
            if(state.Version>=2)foreach(var n in state.Notices)EntryPoint.Journal.AddNotice(n);
        }
        catch (Exception ex) { EntryPoint.Error("Contribution sidecar load failed", ex); }
    }
}







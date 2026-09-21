using System.Text.Json;
namespace Restitutor.Cheats.Skill;

// Persisted choice: UserData/Restitutor/Cheats/skill.v1.json  {"interval":15}
internal static class Settings {
    private sealed class Data { public int interval { get; set; }=Rules.Default; }
    internal static int Interval { get; private set; }=Rules.Default;
    private static string Dir=>Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory,"Restitutor","Cheats");
    private static string FilePath=>Path.Combine(Dir,"skill.v1.json");
    internal static string Load() {
        try {
            if(!File.Exists(FilePath)) { Interval=Rules.Default; return "no file; default "+Rules.Default; }
            var data=JsonSerializer.Deserialize<Data>(File.ReadAllText(FilePath));
            int value=data?.interval??Rules.Default;
            Interval=Rules.Normalize(value);
            return value==Interval ? "loaded "+Interval : $"invalid {value}; default {Interval}";
        } catch(Exception ex) { Interval=Rules.Default; return "read failed; default "+Rules.Default+" ("+ex.Message+")"; }
    }
    internal static bool Save(int value) {
        if(!Rules.Valid(value)) return false;
        Directory.CreateDirectory(Dir);
        string temp=FilePath+".tmp";
        File.WriteAllText(temp,JsonSerializer.Serialize(new Data{interval=value}));
        File.Move(temp,FilePath,true);
        Interval=value;
        return true;
    }
}

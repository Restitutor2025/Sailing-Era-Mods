namespace Il2CppClient.Manager {
 public class PlayerDataSerializer {public byte[] PlayerDataT={1,2,3};}
}
namespace Il2CppFlatBuffers {
 public class FlatBufferBuilder {
  public byte[] Bytes=Array.Empty<byte>(); public FlatBufferBuilder(int size){}
  public void Finish(int offset){} public byte[] SizedByteArray()=>Bytes;
 }
}
namespace Il2CppClient.PlayerStore.Flatbuffers {
 public struct Offset {public int Value;}
 public static class FBPlayerData {
  public static Offset Pack(Il2CppFlatBuffers.FlatBufferBuilder b,byte[] data){b.Bytes=data;return new();}
 }
}
namespace MelonLoader.Utils {
 public static class MelonEnvironment {
  public static string UserDataDirectory=Path.Combine(AppContext.BaseDirectory,"save-checks",Guid.NewGuid().ToString("N"));
 }
}

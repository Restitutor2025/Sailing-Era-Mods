using System.Reflection;
using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.Contribution;

internal static class UiAssets
{
    private static readonly Dictionary<string, NTexture> textures = new();
    private static readonly Dictionary<int, NTexture> icons = new();
    private static readonly List<(GImage Image, Material Material)> materials = new();
    internal static void Bind(GImage image, NTexture texture, bool clipped = false)
    {
        Material? material = null;
        try
        {
            var shader = ShaderConfig.GetShader(ShaderConfig.imageShader);
            if (!shader || !shader.isSupported) throw new InvalidOperationException("Unsupported FairyGUI image shader: " + ShaderConfig.imageShader);
            material = new Material(shader) { name = "Contribution image", hideFlags = HideFlags.HideAndDontSave };
            // Installed resources.assets FairyGUI/Image exposes stencil properties but
            // _ClipBox is a uniform, not a ShaderLab property. HasProperty is not a capability test.
            if (!material.HasProperty(ShaderConfig.ID_StencilComp))
                throw new InvalidOperationException("Image shader has no stencil property: " + shader.name);
            material.EnableKeyword("NOT_COMBINED");
            material.EnableKeyword("NOT_GRAYED");
            material.EnableKeyword(clipped ? "CLIPPED" : "NOT_CLIPPED");
            image.texture = texture;
            material.mainTexture = texture.nativeTexture;
            image.material = material;
            // Native custom-material flag 2 runs ApplyClippingProperties. The auto
            // detector uses HasProperty(_ClipBox), which fails for this installed shader.
            image.displayObject.graphics._materialFlags |= 2;
            materials.Add((image, material));
        }
        catch { if (material != null) UnityEngine.Object.Destroy(material); image.Dispose(); throw; }
    }
    internal static void ReleaseUnusedMaterials()
    {
        for (int i = materials.Count - 1; i >= 0; i--)
            if (materials[i].Image.isDisposed) { UnityEngine.Object.Destroy(materials[i].Material); materials.RemoveAt(i); }
    }
    internal static NTexture Get(string name)
    {
        if (textures.TryGetValue(name, out var cached)) return cached;
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Contribution.Art." + name + ".png")
            ?? throw new InvalidOperationException("Missing UI artwork: " + name);
        using var bytes = new MemoryStream(); stream.CopyTo(bytes);
        var texture = new Texture2D(2, 2) { name = "Contribution " + name, hideFlags = HideFlags.HideAndDontSave };
        try
        {
            if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true)) throw new InvalidOperationException("Invalid artwork: " + name);
            var result = new NTexture(texture) { destroyMethod = DestroyMethod.Destroy };
            textures.Add(name, result); return result;
        }
        catch { UnityEngine.Object.Destroy(texture); throw; }
    }
    internal static NTexture Icon(int index)
    {
        if (icons.TryGetValue(index, out var cached)) return cached;
        var atlas = Get("icons");
        var result = new NTexture(atlas, new Rect(index * atlas.width / 5f, atlas.height * .24f, atlas.width / 5f, atlas.height * .52f), false);
        icons.Add(index, result); return result;
    }
    internal static void Dispose()
    {
        ReleaseUnusedMaterials();
        if (materials.Count != 0) throw new InvalidOperationException("Dispose UI components before artwork");
        foreach (var texture in icons.Values) texture.Dispose(); icons.Clear();
        foreach (var texture in textures.Values) texture.Dispose(); textures.Clear();
    }
}

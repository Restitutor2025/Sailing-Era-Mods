using System.Globalization;

namespace Restitutor.Core;

/// <summary>x.y.z comparison without System.Version quirks (missing parts, build/revision).</summary>
public static class VersionText
{
    public static bool AtLeast(string current, string minimum)
    {
        var a = Parse(current); var b = Parse(minimum);
        for (int i = 0; i < 3; i++)
            if (a[i] != b[i]) return a[i] > b[i];
        return true;
    }

    public static int[] Parse(string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var parts = text.Split('-', '+')[0].Split('.');
        if (parts.Length != 3) throw new FormatException("Version must be x.y.z: " + text);
        var result = new int[3];
        for (int i = 0; i < 3; i++)
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out result[i]))
                throw new FormatException("Version must be x.y.z: " + text);
        return result;
    }
}

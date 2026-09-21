using System.Globalization;
namespace Restitutor.Cheats.Contribution;
internal static class Rules {
    internal const string InputRestriction = "[0-9]";
    internal static string ClampInput(string value) {
        if (value.Length == 0 || value.Any(c => c < '0' || c > '9')) return value;
        string digits = value.TrimStart('0');
        return digits.Length > 4 || (digits.Length == 4 && string.CompareOrdinal(digits, "1000") > 0) ? "1000" : value;
    }
    internal static bool TryTarget(string value, out int target) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out target) && target >= 0 && target <= 1000;
}


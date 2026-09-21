using System.Reflection;

namespace Restitutor.Core;

/// <summary>Finds exactly one method or throws. Same matching the mods used before Core:
/// name + optional exact parameter types; declaredOnly off only for mods that relied on inherited lookups.</summary>
public static class MethodLookup
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public static MethodInfo Unique(Type target, string name, Type[]? args = null, bool declaredOnly = true)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        var flags = declaredOnly ? Any | BindingFlags.DeclaredOnly : Any;
        var found = target.GetMethods(flags)
            .Where(m => m.Name == name && (args == null || m.GetParameters().Select(p => p.ParameterType).SequenceEqual(args)))
            .ToArray();
        if (found.Length != 1)
            throw new MissingMethodException($"{target.FullName}.{name}{Describe(args)}: expected 1 match, found {found.Length}");
        return found[0];
    }

    /// <summary>Static handler declared on <paramref name="owner"/> (public or not); must be unique by name.</summary>
    public static MethodInfo Handler(Type owner, string name)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        var found = owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == name).ToArray();
        if (found.Length != 1)
            throw new MissingMethodException($"handler {owner.FullName}.{name}: expected 1 static method, found {found.Length}");
        return found[0];
    }

    private static string Describe(Type[]? args) => args == null ? "" : "(" + string.Join(", ", args.Select(a => a.Name)) + ")";
}

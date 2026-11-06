using System;
using System.Linq;
using System.Reflection;

namespace Ferrum.Loader.Features;

public static class RestartScheduler
{
    /// <summary>Call on the game thread at natural round ending. Do not initiate a round restart here.</summary>
    public static bool Schedule()
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("ServerStatic", false))
            .FirstOrDefault(candidate => candidate != null) ?? throw new TypeLoadException("ServerStatic");
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        FieldInfo? field = type.GetField("StopNextRound", flags);
        PropertyInfo? property = type.GetProperty("StopNextRound", flags);
        object? current = field != null ? field.GetValue(null) : property?.GetValue(null, null);
        if (current == null || current.ToString() != "DoNothing") return false;
        object restart = Enum.Parse(current.GetType(), "Restart");
        if (field != null) field.SetValue(null, restart); else property!.SetValue(null, restart, null);
        return true;
    }
}

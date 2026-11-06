using System;
using System.Linq;
using System.Reflection;

namespace Ferrum.Loader.Features.Logging;

/// <summary>Binds directly to the game's ServerConsole.AddLog without a redistributable game-assembly dependency.</summary>
public static class ServerConsoleWriter
{
    private static readonly Lazy<Action<string, ConsoleColor>> Writer = new(CreateWriter);

    public static void Write(string message, ConsoleColor color) => Writer.Value(message, color);

    private static void Invoke(MethodInfo method, int parameterCount, string message, ConsoleColor color)
    {
        object?[] arguments = new object?[parameterCount];
        arguments[0] = message;
        arguments[1] = color;
        for (int index = 2; index < arguments.Length; index++)
            arguments[index] = Type.Missing;

        try
        {
            // Direct invocation of the game's console, without loader-provided logging wrappers.
            method.Invoke(null, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException != null)
        {
            throw new InvalidOperationException("ServerConsole.AddLog failed.", error.InnerException);
        }
    }

    private static Action<string, ConsoleColor> CreateWriter()
    {
        Type? console = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("ServerConsole", throwOnError: false))
            .FirstOrDefault(type => type != null);

        if (console == null)
            throw new InvalidOperationException("The dedicated server's ServerConsole type is not loaded.");

        MethodInfo? method = console.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(candidate => candidate.Name == "AddLog" && candidate.ReturnType == typeof(void))
            .Where(candidate => IsSupported(candidate.GetParameters()))
            .OrderBy(candidate => candidate.GetParameters().Length)
            .FirstOrDefault();

        if (method == null)
            throw new MissingMethodException("ServerConsole", "AddLog(string, ConsoleColor, ...optional)");

        int parameterCount = method.GetParameters().Length;
        if (parameterCount == 2)
            return (Action<string, ConsoleColor>)Delegate.CreateDelegate(typeof(Action<string, ConsoleColor>), method);

        return (message, color) => Invoke(method, parameterCount, message, color);
    }

    private static bool IsSupported(ParameterInfo[] parameters)
    {
        return parameters.Length >= 2
            && parameters[0].ParameterType == typeof(string)
            && parameters[1].ParameterType == typeof(ConsoleColor)
            && parameters.Skip(2).All(parameter => parameter.IsOptional);
    }
}

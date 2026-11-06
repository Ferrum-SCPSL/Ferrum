using System.IO;
using Ferrum.Interop;

namespace Ferrum.Loader.Features.Paths;

public static class NativePaths
{
    public static string LibraryPath => Path.Combine(FerrumPaths.Dependencies, NativeBridge.LibraryFileName);
}

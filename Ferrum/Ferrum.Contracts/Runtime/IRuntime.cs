using System;

namespace Ferrum.Contracts;

public interface IRuntime : IDisposable
{
    void Initialize(RuntimeContext context);
    void PluginsEnabled();
    void RoundEnding();
}

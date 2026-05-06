// Polyfill required by Unity's Mono runtime to support C# record types and init-only setters.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

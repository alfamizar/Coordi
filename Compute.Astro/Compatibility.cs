#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill enabling C# <c>init</c> accessors (and therefore records) on target
    /// frameworks whose BCL predates them, such as netstandard2.0.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
#endif

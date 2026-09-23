using System;

namespace Livia.Attributes;

/// <summary>
/// Marks an API as requiring explicit opt-in through an Livia MSBuild property.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal sealed class RequiresLiviaOptInAttribute : Attribute
{
    public RequiresLiviaOptInAttribute(LiviaOptIn optIn)
    {
        OptIn = optIn;
    }

    public LiviaOptIn OptIn
    {
        get;
    }
}
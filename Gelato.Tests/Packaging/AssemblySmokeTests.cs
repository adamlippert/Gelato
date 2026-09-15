namespace Gelato.Tests.Packaging;

public class AssemblySmokeTests
{
    [Fact]
    public void GelatoAssembly_LoadsAndIsNamedGelato()
    {
        var asm = typeof(GelatoPlugin).Assembly;

        Assert.Equal("Gelato", asm.GetName().Name);
        Assert.True(File.Exists(asm.Location), $"assembly not on disk: {asm.Location}");
    }

    /// <summary>
    /// Jellyfin calls Assembly.GetTypes() on every plugin DLL at startup and marks the plugin
    /// "Not Supported" if it throws. A decorator missing a member the referenced Jellyfin
    /// interfaces declare surfaces here as a ReflectionTypeLoadException.
    /// </summary>
    [Fact]
    public void GelatoAssembly_AllTypesLoad_AsJellyfinChecksThem()
    {
        var types = typeof(GelatoPlugin).Assembly.GetTypes();

        Assert.NotEmpty(types);
    }
}

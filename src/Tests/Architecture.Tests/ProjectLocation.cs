namespace Architecture.Tests;

/// <summary>Where a project lives, which tells the tests what it is.</summary>
internal enum ProjectLocation
{
    /// <summary><c>src/Core/&lt;project&gt;/</c> - the application itself.</summary>
    Core,

    /// <summary><c>src/Tools/&lt;project&gt;/</c> - a development tool.</summary>
    Tools,

    /// <summary><c>src/Tests/&lt;project&gt;/</c> - a test project.</summary>
    Tests,

    /// <summary><c>src/&lt;project&gt;/</c> - a UI, and therefore a composition root.</summary>
    Ui,

    /// <summary>Anywhere else, which <c>dotnet.layout</c> does not allow.</summary>
    Elsewhere,
}

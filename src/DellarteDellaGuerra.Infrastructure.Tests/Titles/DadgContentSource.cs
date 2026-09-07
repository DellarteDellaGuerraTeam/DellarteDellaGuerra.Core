namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Where the content-integration tests read the 1471 content from. The names are also the values
/// accepted in <c>DADG_CONTENT_SOURCE</c>, case-insensitively.
/// </summary>
internal enum DadgContentSource
{
    /// <summary>
    /// The live files in the sibling DellarteDellaGuerraMap module. The default, so that a change
    /// to the shipped content shows up on the machine that made it.
    /// </summary>
    Module,

    /// <summary>
    /// The snapshot embedded in this test assembly, which needs no game installation and no
    /// sibling checkout.
    /// </summary>
    Embedded
}

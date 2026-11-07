#nullable enable
namespace Nemesis
{
    /// <summary>
    /// Build stamp written to Player.log at startup. Azrael's series hub reads
    /// <see cref="BuildStamp"/> by reflection, so keep the name public and constant.
    /// </summary>
    public static class NemesisBuildInfo
    {
        public const string BuildStamp = "nemesis2-net10";
    }
}

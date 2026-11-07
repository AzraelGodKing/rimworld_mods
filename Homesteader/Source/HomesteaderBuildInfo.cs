#nullable enable
namespace Homesteader
{
    /// <summary>
    /// Series convention (AZR-588): the Azrael hub reads Homesteader.HomesteaderBuildInfo.BuildStamp.
    /// Change it with each build that ships behaviour changes.
    /// </summary>
    public static class HomesteaderBuildInfo
    {
        public const string BuildStamp = "homesteader-2-v1";
    }
}

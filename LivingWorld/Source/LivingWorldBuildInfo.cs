namespace LivingWorld
{
    /// <summary>
    /// Build stamp shown in the load log. Sister mods (the Azrael hub) read it by reflection,
    /// so keep the type name, field name and const string type stable.
    /// </summary>
    public static class LivingWorldBuildInfo
    {
        public const string BuildStamp = "waylith-v1";
    }
}

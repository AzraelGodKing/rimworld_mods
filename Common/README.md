# Common linked source

C# compiled into each mod DLL (`<Import Project="../../Common/Azrael.Common.props" />`).
No shared runtime assembly — every mod stays independently loadable (AZR-130).

Do not copy these files into a mod's `Source/` folder. Call `AzraelCommon.SafePatchAll.Apply`, `AzraelCommon.UpdateNews.TrySend` (version letter from this DLL's `About.xml` + matching `changelog.txt`), `AzraelCommon.ModVersionLog.Write` (Player.log version line: prefix, optional `ModContentPack`, extra build stamp), and `AzraelCommon.ModPack.ForExecutingAssembly` (the running pack that loaded this DLL; UpdateNews and ModVersionLog share it). Wave Survival Reforged has no Harmony, so it links only `ModPack.cs` and `ModVersionLog.cs`. The test harness fails if a second `HarmonyPatchAll.cs` or `SafePatchAll.cs` appears under a mod.

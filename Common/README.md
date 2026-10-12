# Common linked source

C# compiled into each mod DLL (`<Import Project="../../Common/Azrael.Common.props" />`).
No shared runtime assembly — every mod stays independently loadable (AZR-130).

Do not copy these files into a mod's `Source/` folder. Call `AzraelCommon.SafePatchAll.Apply`, `AzraelCommon.UpdateNews.TrySend` (version letter from this DLL's `About.xml` + matching `changelog.txt`), and `AzraelCommon.ModVersionLog.Write` (Player.log version line: prefix, optional `ModContentPack`, extra build stamp). The test harness fails if a second `HarmonyPatchAll.cs` or `SafePatchAll.cs` appears under a mod.

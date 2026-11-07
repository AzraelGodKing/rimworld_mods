using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

namespace Niceties.Tests
{
    /// <summary>
    /// Niceties patches some engine methods by string name or relies on their exact shape.
    /// These read the Krafs ref packs the two mod builds restore (1.6 and 1.7) and fail if a
    /// RimWorld update changes a signature (AZR-532, AZR-571). Run after building
    /// Niceties.csproj and Niceties.1.7.csproj so both packs are in the NuGet cache.
    /// </summary>
    public class SignatureTests
    {
        public static readonly TheoryData<string> Versions = new TheoryData<string> { "1.6.4871", "1.7.4998-unstable" };

        private static ModuleDefinition Game(string version)
        {
            string root = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            string refDir = Path.Combine(root, "krafs.rimworld.ref", version, "ref");
            Assert.True(Directory.Exists(refDir),
                "Krafs.Rimworld.Ref " + version + " is not in " + root + "; build both Niceties projects first.");
            string dll = Directory.GetFiles(refDir, "Assembly-CSharp.dll", SearchOption.AllDirectories).First();
            return ModuleDefinition.ReadModule(dll);
        }

        private static void AssertMethod(ModuleDefinition game, string type, string method, params string[] parameters)
        {
            TypeDefinition? t = game.GetTypes().FirstOrDefault(x => x.FullName == type);
            Assert.True(t != null, type + " is missing");
            bool found = t!.Methods.Any(m => m.Name == method
                && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters));
            Assert.True(found, type + "." + method + "(" + string.Join(", ", parameters) + ") is missing or changed");
        }

        [Theory]
        [MemberData(nameof(Versions))]
        public void ColonistBarTargets(string version)
        {
            using ModuleDefinition game = Game(version);
            AssertMethod(game, "RimWorld.ColonistBarDrawLocsFinder", "CalculateDrawLocs",
                "System.Collections.Generic.List`1<UnityEngine.Vector2>", "System.Single&", "System.Int32");
            AssertMethod(game, "RimWorld.ColonistBar", "get_Visible");
            AssertMethod(game, "RimWorld.ColonistBar", "ColonistBarOnGUI");
        }

        [Theory]
        [MemberData(nameof(Versions))]
        public void SharedRoomTargets(string version)
        {
            using ModuleDefinition game = Game(version);
            AssertMethod(game, "Verse.Pawn", "CheckForDisturbedSleep", "Verse.Pawn");
            AssertMethod(game, "Verse.Room", "GetRoomRoleLabel");
        }

        [Theory]
        [MemberData(nameof(Versions))]
        public void WearAnyOutfitTargets(string version)
        {
            using ModuleDefinition game = Game(version);
            AssertMethod(game, "RimWorld.ApparelProperties", "CorrectGenderForWearing", "Verse.Gender");
            AssertMethod(game, "RimWorld.ThoughtWorker_WrongApparelGender", "CurrentStateInternal", "Verse.Pawn");
            AssertMethod(game, "Verse.PawnGenerator", "GeneratePawn", "Verse.PawnGenerationRequest");
        }
    }
}

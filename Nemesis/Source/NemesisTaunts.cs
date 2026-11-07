#nullable enable
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>Personal taunt / letter copy. Names the nemesis and optional fixation target.</summary>
    public static class NemesisTaunts
    {
        public static string TargetPhrase(NemesisData data)
        {
            if (data.targetMode == NemesisTargetMode.Pawn && data.targetPawnName is string name && name.Length > 0)
                return name;
            return "Nemesis_Phrase_YourColony".Translate();
        }

        /// <summary>Pick a comms taunt, note it in the dossier (as key + args), and return it translated.</summary>
        public static string PickCommsLine(NemesisData data)
        {
            string target = TargetPhrase(data);
            string name = data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();

            string[] line = PickVoiceLine(data, name, target);
            string key = line[0];
            string[] args = new string[line.Length - 1];
            System.Array.Copy(line, 1, args, 0, args.Length);
            NemesisTells.RecordNote(data, key, args);
            return NemesisNote.Render(key, args);
        }

        /// <summary>Key first, then its arguments.</summary>
        static string[] PickVoiceLine(NemesisData data, string name, string target)
        {
            bool high = data.EffectiveAggression >= 3f;
            return data.voice switch
            {
                NemesisVoice.Mocking => high
                    ? (Rand.Bool ? new[] { "Nemesis_Taunt_High0", name, target }
                        : new[] { "Nemesis_Taunt_Low5", name, target })
                    : (Rand.Bool ? new[] { "Nemesis_Taunt_Low1", name, target }
                        : SoftHomesteaderTaunt(data, name, target)
                            ?? new[] { "Nemesis_Taunt_Low5", name, target }),
                NemesisVoice.Zealot => high
                    ? (Rand.Bool
                        ? new[] { "Nemesis_Taunt_High1", name, target }
                        : new[] { "Nemesis_Taunt_High5", name, target })
                    : new[] { "Nemesis_Taunt_Low4", name, target },
                NemesisVoice.Casual => high
                    ? (Rand.Bool
                        ? new[] { "Nemesis_Taunt_High4", name, target }
                        : SoftStormFlavor(name, target)
                            ?? new[] { "Nemesis_Taunt_High3", name, target, data.escapeCount.ToString() })
                    : new[] { "Nemesis_Taunt_Low3", name, target },
                _ => high
                    ? (Rand.Bool
                        ? new[] { "Nemesis_Taunt_High2", name, target }
                        : new[] { "Nemesis_Taunt_Low2", name, target })
                    : new[] { "Nemesis_Taunt_Low0", name, target },
            };
        }

        private static string[]? SoftHomesteaderTaunt(NemesisData data, string name, string target)
        {
            if (!SoftCompat.HomesteaderActive) return null;
            Pawn? victim = GameComponent_Nemesis.Instance?.FindTargetPawn(data);
            string? fav = SoftCompat.TryFavoriteFoodLabel(victim);
            if (fav != null)
                return new[] { "Nemesis_Taunt_FavoriteFood", name, target, fav };
            return new[] { "Nemesis_Taunt_Cellar", name };
        }

        private static string[]? SoftStormFlavor(string name, string target)
        {
            if (!SoftCompat.StormproofActive) return null;
            return new[] { "Nemesis_Taunt_Ion", name, target };
        }

        public static string EscapeLetterBody(NemesisData data)
        {
            int n = data.escapeCount;
            string? name = data.nemesisName;
            if (n == 1) return "Nemesis_Letter_Escape1".Translate(name);
            if (n == 2) return "Nemesis_Letter_Escape2".Translate(name);
            if (n <= 4) return "Nemesis_Letter_EscapeN".Translate(name, n);
            return "Nemesis_Letter_EscapeMany".Translate(name, n);
        }

        /// <summary>BFV-style theatrical return-with-army raid letters by escape stage.</summary>
        public static string VengeanceReturnTitle(NemesisData data, int stage)
        {
            string name = data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            stage = UnityEngine.Mathf.Clamp(stage, 1, 5);
            return $"Nemesis_Letter_VengeanceReturnTitle{stage}".Translate(name);
        }

        public static string VengeanceReturnBody(NemesisData data, int stage)
        {
            string name = data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            string target = TargetPhrase(data);
            stage = UnityEngine.Mathf.Clamp(stage, 1, 5);
            string focus = data.FocusLabelKey.Translate();
            int tier = data.progressionLevel;
            string body = $"Nemesis_Letter_VengeanceReturnBody{stage}".Translate(name, target);
            if (tier > 0)
                body += "\n\n" + "Nemesis_Letter_VengeanceCaptainLine".Translate(name, tier, focus);
            return body;
        }
    }
}

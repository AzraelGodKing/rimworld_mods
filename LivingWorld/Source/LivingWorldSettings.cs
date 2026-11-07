using UnityEngine;
using Verse;

namespace LivingWorld
{
    public class LivingWorldSettings : ModSettings
    {
        public bool enabled = true;
        public bool morphEnabled = true;
        public bool chronicleEnabled = true;
        public bool diplomacyEnabled = true;
        public bool falloutEnabled = true;
        public bool tradeBlackoutEnabled = true;
        public bool warSitesEnabled = true;
        public bool warbandFalloutEnabled = false;
        public bool roadHazardsEnabled = true;
        public bool trafficEnabled = true;

        /// <summary>0 = major only, 1 = medium, 2 = high.</summary>
        public int newsVerbosity = 1;

        public int tickInterval = 10000;
        public int resolutionsPerPulse = 1;
        public int maxLettersPerQuadrum = 8;
        public int maxMorphsPerYear = 6;
        public int proximityTiles = 30;
        public int maxConcurrentWars = 2;

        /// <summary>0.25–2 multiplier on escalate / battle / betrayal chances.</summary>
        public float warRate = 1f;

        /// <summary>How many entries the chronicle keeps (48–240).</summary>
        public int chronicleCapacity = 96;

        /// <summary>
        /// World-map overlay. A per-player view setting: not part of the game's settings
        /// copy and never synced (see <see cref="ClientOnlyFields"/>).
        /// </summary>
        public bool overlaysEnabled;

        /// <summary>Comma-separated FactionDef names left out of the simulation.</summary>
        public string excludedFactionDefs = string.Empty;

        // Parsed excludedFactionDefs, rebuilt when the string changes. Not saved.
        private string excludedParsedFrom;
        private readonly System.Collections.Generic.HashSet<string> excludedSet =
            new System.Collections.Generic.HashSet<string>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref morphEnabled, "morphEnabled", true);
            Scribe_Values.Look(ref chronicleEnabled, "chronicleEnabled", true);
            Scribe_Values.Look(ref diplomacyEnabled, "diplomacyEnabled", true);
            Scribe_Values.Look(ref falloutEnabled, "falloutEnabled", true);
            Scribe_Values.Look(ref tradeBlackoutEnabled, "tradeBlackoutEnabled", true);
            Scribe_Values.Look(ref warSitesEnabled, "warSitesEnabled", true);
            Scribe_Values.Look(ref warbandFalloutEnabled, "warbandFalloutEnabled", false);
            Scribe_Values.Look(ref roadHazardsEnabled, "roadHazardsEnabled", true);
            Scribe_Values.Look(ref trafficEnabled, "trafficEnabled", true);
            Scribe_Values.Look(ref newsVerbosity, "newsVerbosity", 1);
            Scribe_Values.Look(ref tickInterval, "tickInterval", 10000);
            Scribe_Values.Look(ref resolutionsPerPulse, "resolutionsPerPulse", 1);
            Scribe_Values.Look(ref maxLettersPerQuadrum, "maxLettersPerQuadrum", 8);
            Scribe_Values.Look(ref maxMorphsPerYear, "maxMorphsPerYear", 6);
            Scribe_Values.Look(ref proximityTiles, "proximityTiles", 30);
            Scribe_Values.Look(ref maxConcurrentWars, "maxConcurrentWars", 2);
            Scribe_Values.Look(ref warRate, "warRate", 1f);
            Scribe_Values.Look(ref chronicleCapacity, "chronicleCapacity", 96);
            Scribe_Values.Look(ref overlaysEnabled, "overlaysEnabled", false);
            Scribe_Values.Look(ref excludedFactionDefs, "excludedFactionDefs", string.Empty);
            excludedFactionDefs ??= string.Empty;
        }

        public bool IsExcluded(RimWorld.FactionDef def)
        {
            if (def == null || string.IsNullOrEmpty(excludedFactionDefs))
            {
                return false;
            }
            if (!ReferenceEquals(excludedParsedFrom, excludedFactionDefs))
            {
                excludedParsedFrom = excludedFactionDefs;
                excludedSet.Clear();
                foreach (string name in excludedFactionDefs.Split(','))
                {
                    if (name.Length > 0)
                    {
                        excludedSet.Add(name);
                    }
                }
            }
            return excludedSet.Contains(def.defName);
        }

        public void SetExcluded(RimWorld.FactionDef def, bool excluded)
        {
            if (def == null || IsExcluded(def) == excluded)
            {
                return;
            }
            var names = new System.Collections.Generic.List<string>(excludedSet);
            if (excluded)
            {
                names.Add(def.defName);
            }
            else
            {
                names.Remove(def.defName);
            }
            names.Sort(string.CompareOrdinal);
            excludedFactionDefs = string.Join(",", names);
        }

        public void ResetToDefaults()
        {
            enabled = true;
            morphEnabled = true;
            chronicleEnabled = true;
            diplomacyEnabled = true;
            falloutEnabled = true;
            tradeBlackoutEnabled = true;
            warSitesEnabled = true;
            warbandFalloutEnabled = false;
            roadHazardsEnabled = true;
            trafficEnabled = true;
            newsVerbosity = 1;
            tickInterval = 10000;
            resolutionsPerPulse = 1;
            maxLettersPerQuadrum = 8;
            maxMorphsPerYear = 6;
            proximityTiles = 30;
            maxConcurrentWars = 2;
            warRate = 1f;
            chronicleCapacity = 96;
            excludedFactionDefs = string.Empty;
        }

        public enum Preset
        {
            Quiet,
            Default,
            Turbulent,
        }

        /// <summary>Pace presets change numbers only; feature toggles stay as they are.</summary>
        public void ApplyPreset(Preset preset)
        {
            switch (preset)
            {
                case Preset.Quiet:
                    newsVerbosity = 0;
                    tickInterval = 20000;
                    resolutionsPerPulse = 1;
                    maxLettersPerQuadrum = 4;
                    maxMorphsPerYear = 3;
                    maxConcurrentWars = 1;
                    warRate = 0.5f;
                    break;
                case Preset.Turbulent:
                    newsVerbosity = 2;
                    tickInterval = 6000;
                    resolutionsPerPulse = 2;
                    maxLettersPerQuadrum = 14;
                    maxMorphsPerYear = 12;
                    maxConcurrentWars = 4;
                    warRate = 1.6f;
                    break;
                default:
                    newsVerbosity = 1;
                    tickInterval = 10000;
                    resolutionsPerPulse = 1;
                    maxLettersPerQuadrum = 8;
                    maxMorphsPerYear = 6;
                    maxConcurrentWars = 2;
                    warRate = 1f;
                    break;
            }
        }

        public LivingWorldSettings Clone()
        {
            var copy = new LivingWorldSettings();
            copy.ReadFrom(Serialize());
            return copy;
        }

        // Sync payload for Multiplayer: every public bool/int/float/string field as
        // name=value; invariant culture so every client parses the same values. String
        // values (defName lists) never contain ';' or '='.
        /// <summary>View-only settings each player keeps for themselves.</summary>
        private static readonly string[] ClientOnlyFields = { nameof(overlaysEnabled) };

        private static readonly System.Reflection.FieldInfo[] syncedFields = SyncedFields();

        private static System.Reflection.FieldInfo[] SyncedFields()
        {
            var list = new System.Collections.Generic.List<System.Reflection.FieldInfo>();
            foreach (System.Reflection.FieldInfo f in typeof(LivingWorldSettings).GetFields(
                         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                         | System.Reflection.BindingFlags.DeclaredOnly))
            {
                if (System.Array.IndexOf(ClientOnlyFields, f.Name) >= 0)
                {
                    continue;
                }
                if (f.FieldType == typeof(bool) || f.FieldType == typeof(int) || f.FieldType == typeof(float)
                    || f.FieldType == typeof(string))
                {
                    list.Add(f);
                }
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return list.ToArray();
        }

        public string Serialize()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < syncedFields.Length; i++)
            {
                System.Reflection.FieldInfo f = syncedFields[i];
                object v = f.GetValue(this);
                sb.Append(f.Name).Append('=').Append(
                    v is float fl ? fl.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : v?.ToString() ?? string.Empty)
                    .Append(';');
            }
            return sb.ToString();
        }

        public void ReadFrom(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return;
            }
            foreach (string part in payload.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }
                string name = part.Substring(0, eq);
                string value = part.Substring(eq + 1);
                for (int i = 0; i < syncedFields.Length; i++)
                {
                    System.Reflection.FieldInfo f = syncedFields[i];
                    if (f.Name != name)
                    {
                        continue;
                    }
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    if (f.FieldType == typeof(bool) && bool.TryParse(value, out bool b))
                    {
                        f.SetValue(this, b);
                    }
                    else if (f.FieldType == typeof(int)
                             && int.TryParse(value, System.Globalization.NumberStyles.Integer, inv, out int n))
                    {
                        f.SetValue(this, n);
                    }
                    else if (f.FieldType == typeof(float)
                             && float.TryParse(value, System.Globalization.NumberStyles.Float, inv, out float x))
                    {
                        f.SetValue(this, x);
                    }
                    else if (f.FieldType == typeof(string))
                    {
                        f.SetValue(this, value);
                    }
                    break;
                }
            }
        }

        private static Vector2 scrollPos;
        private static float lastViewHeight = 1200f;

        public static void DoSettingsWindow(Rect inRect)
        {
            LivingWorldSettings s = LivingWorldMod.Global;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, lastViewHeight);
            Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            if (Current.ProgramState == ProgramState.Playing)
            {
                listing.Label("LivingWorld_Settings_AppliesOnClose".Translate());
                listing.Gap(6f);
            }

            listing.Label("LivingWorld_Settings_Presets".Translate());
            Rect row = listing.GetRect(28f);
            float w = (row.width - 16f) / 3f;
            if (Widgets.ButtonText(new Rect(row.x, row.y, w, row.height), "LivingWorld_Preset_Quiet".Translate()))
            {
                s.ApplyPreset(Preset.Quiet);
            }
            if (Widgets.ButtonText(new Rect(row.x + w + 8f, row.y, w, row.height), "LivingWorld_Preset_Default".Translate()))
            {
                s.ApplyPreset(Preset.Default);
            }
            if (Widgets.ButtonText(new Rect(row.x + 2f * (w + 8f), row.y, w, row.height), "LivingWorld_Preset_Turbulent".Translate()))
            {
                s.ApplyPreset(Preset.Turbulent);
            }
            listing.GapLine();

            listing.CheckboxLabeled("LivingWorld_Settings_Enabled".Translate(), ref s.enabled,
                "LivingWorld_Settings_EnabledDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Chronicle".Translate(), ref s.chronicleEnabled,
                "LivingWorld_Settings_ChronicleDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Morph".Translate(), ref s.morphEnabled,
                "LivingWorld_Settings_MorphDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Diplomacy".Translate(), ref s.diplomacyEnabled,
                "LivingWorld_Settings_DiplomacyDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Fallout".Translate(), ref s.falloutEnabled,
                "LivingWorld_Settings_FalloutDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_TradeBlackout".Translate(), ref s.tradeBlackoutEnabled,
                "LivingWorld_Settings_TradeBlackoutDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_WarSites".Translate(), ref s.warSitesEnabled,
                "LivingWorld_Settings_WarSitesDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Warband".Translate(), ref s.warbandFalloutEnabled,
                "LivingWorld_Settings_WarbandDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_RoadHazards".Translate(), ref s.roadHazardsEnabled,
                "LivingWorld_Settings_RoadHazardsDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Traffic".Translate(), ref s.trafficEnabled,
                "LivingWorld_Settings_TrafficDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Overlay_Toggle".Translate(), ref s.overlaysEnabled,
                "LivingWorld_Overlay_Desc".Translate());
            listing.GapLine();

            listing.Label("LivingWorld_Settings_Verbosity".Translate() + ": "
                + VerbosityLabel(s.newsVerbosity));
            s.newsVerbosity = Mathf.RoundToInt(listing.Slider(s.newsVerbosity, 0f, 2f));
            listing.Label("LivingWorld_Settings_TickInterval".Translate(s.tickInterval));
            s.tickInterval = Mathf.RoundToInt(listing.Slider(s.tickInterval, 5000f, 30000f) / 1000f) * 1000;
            listing.Label("LivingWorld_Settings_Resolutions".Translate(s.resolutionsPerPulse));
            s.resolutionsPerPulse = Mathf.RoundToInt(listing.Slider(s.resolutionsPerPulse, 1f, 3f));
            listing.Label("LivingWorld_Settings_MaxLetters".Translate(s.maxLettersPerQuadrum));
            s.maxLettersPerQuadrum = Mathf.RoundToInt(listing.Slider(s.maxLettersPerQuadrum, 2f, 20f));
            listing.Label("LivingWorld_Settings_MaxMorphs".Translate(s.maxMorphsPerYear));
            s.maxMorphsPerYear = Mathf.RoundToInt(listing.Slider(s.maxMorphsPerYear, 1f, 20f));
            listing.Label("LivingWorld_Settings_Proximity".Translate(s.proximityTiles));
            s.proximityTiles = Mathf.RoundToInt(listing.Slider(s.proximityTiles, 10f, 80f));
            listing.Label("LivingWorld_Settings_MaxWars".Translate(s.maxConcurrentWars));
            s.maxConcurrentWars = Mathf.RoundToInt(listing.Slider(s.maxConcurrentWars, 1f, 5f));
            listing.Label("LivingWorld_Settings_WarRate".Translate(s.warRate.ToString("F2")));
            s.warRate = listing.Slider(s.warRate, 0.25f, 2f);
            listing.Label("LivingWorld_Settings_ChronicleCapacity".Translate(s.chronicleCapacity));
            s.chronicleCapacity = Mathf.RoundToInt(listing.Slider(s.chronicleCapacity, 48f, 240f) / 8f) * 8;
            listing.GapLine();

            listing.Label("LivingWorld_Settings_Excluded".Translate(), -1f,
                "LivingWorld_Settings_ExcludedDesc".Translate());
            foreach (RimWorld.FactionDef def in SimFactionDefs())
            {
                bool included = !s.IsExcluded(def);
                bool was = included;
                listing.CheckboxLabeled(def.LabelCap, ref included);
                if (included != was)
                {
                    s.SetExcluded(def, !included);
                }
            }
            listing.GapLine();

            if (listing.ButtonText("LivingWorld_Settings_Reset".Translate(), null, 0.25f))
            {
                s.ResetToDefaults();
            }

            if (Current.ProgramState == ProgramState.Playing && GameComponent_LivingWorld.Get != null)
            {
                listing.Gap();
                if (listing.ButtonText("LivingWorld_Settings_PrepareRemoval".Translate(), null, 0.25f))
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "LivingWorld_Settings_PrepareRemovalConfirm".Translate(),
                        LivingWorldRemoval.Confirm,
                        destructive: true));
                }
                listing.Label("LivingWorld_Settings_PrepareRemovalDesc".Translate());
            }

            lastViewHeight = listing.CurHeight + 12f;
            listing.End();
            Widgets.EndScrollView();
        }

        private static System.Collections.Generic.List<RimWorld.FactionDef> simFactionDefs;

        /// <summary>Faction kinds the sim can track: humanlike, not hidden, not the player's.</summary>
        private static System.Collections.Generic.List<RimWorld.FactionDef> SimFactionDefs()
        {
            if (simFactionDefs == null)
            {
                simFactionDefs = new System.Collections.Generic.List<RimWorld.FactionDef>();
                foreach (RimWorld.FactionDef def in DefDatabase<RimWorld.FactionDef>.AllDefsListForReading)
                {
                    if (def.humanlikeFaction && !def.hidden && !def.isPlayer)
                    {
                        simFactionDefs.Add(def);
                    }
                }
                simFactionDefs.SortBy(d => d.label ?? d.defName);
            }
            return simFactionDefs;
        }

        private static string VerbosityLabel(int level)
        {
            switch (level)
            {
                case 0: return "LivingWorld_Verbosity_Low".Translate();
                case 2: return "LivingWorld_Verbosity_High".Translate();
                default: return "LivingWorld_Verbosity_Medium".Translate();
            }
        }
    }
}

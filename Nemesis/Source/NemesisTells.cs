#nullable enable
using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public enum NemesisVoice
    {
        Cold,
        Mocking,
        Zealot,
        Casual,
    }

    public enum NemesisMark
    {
        CallingCard,
        ArrangedCorpse,
        Scrawl,
        SabotageToken,
    }

    public enum NemesisHabit
    {
        PowerFirst,
        FoodFirst,
        LeaveOneAlive,
        EarlyRetreat,
        SameBuilding,
    }

    /// <summary>
    /// A dossier note, stored as a translation key plus arguments and translated when drawn,
    /// so notes follow the language setting (AZR-555). An argument that starts with
    /// <see cref="KeyArg"/> is itself a key; one that starts with <see cref="ThingArg"/> is a
    /// ThingDef name shown by its label. Notes from 1.x saves keep their stored text.
    /// </summary>
    public class NemesisNote : IExposable
    {
        public const string KeyArg = "@k:";
        public const string ThingArg = "@t:";

        public int tick;
        public string? text;
        public string? key;
        public List<string>? args;

        public static string Key(string key) => KeyArg + key;
        public static string Thing(string defName) => ThingArg + defName;

        public string Text => key == null ? text ?? "" : Render(key, args);

        public static string Render(string key, IList<string>? args)
        {
            if (args == null || args.Count == 0) return key.Translate();
            NamedArgument[] named = new NamedArgument[args.Count];
            for (int i = 0; i < args.Count; i++)
                named[i] = RenderArg(args[i]);
            return key.Translate(named);
        }

        private static string RenderArg(string arg)
        {
            if (arg.StartsWith(KeyArg, StringComparison.Ordinal))
                return arg.Substring(KeyArg.Length).Translate();
            if (arg.StartsWith(ThingArg, StringComparison.Ordinal))
            {
                ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(arg.Substring(ThingArg.Length));
                return def != null ? (string)def.LabelCap : "Nemesis_Weapon_Unknown".Translate();
            }
            return arg;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref tick, "tick", 0);
            Scribe_Values.Look(ref text, "text");
            Scribe_Values.Look(ref key, "key");
            Scribe_Collections.Look(ref args, "args", LookMode.Value);
        }
    }

    public class NemesisEpitaph : IExposable
    {
        public string? name;
        public string? factionName;
        public string? focusKey;
        public string? voiceKey;
        public string? markKey;
        public string? habitKey;
        public string? targetName;
        public string? endKey;
        public int escapes;
        public int days;
        public int tick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref factionName, "factionName");
            Scribe_Values.Look(ref focusKey, "focusKey");
            Scribe_Values.Look(ref voiceKey, "voiceKey");
            Scribe_Values.Look(ref markKey, "markKey");
            Scribe_Values.Look(ref habitKey, "habitKey");
            Scribe_Values.Look(ref targetName, "targetName");
            Scribe_Values.Look(ref endKey, "endKey");
            Scribe_Values.Look(ref escapes, "escapes", 0);
            Scribe_Values.Look(ref days, "days", 0);
            Scribe_Values.Look(ref tick, "tick", 0);
        }

        public string SummaryLine()
        {
            return "Nemesis_Epitaph_Line".Translate(
                name ?? "Nemesis_Phrase_Someone".Translate(),
                endKey != null ? endKey.Translate() : "—",
                days,
                escapes);
        }
    }

    /// <summary>Persistent signatures for one hunt. Never required to end it.</summary>
    public static class NemesisTells
    {
        // Every value of each enum, so adding one never needs a hand-kept count (AZR-535).
        private static readonly NemesisVoice[] Voices = (NemesisVoice[])System.Enum.GetValues(typeof(NemesisVoice));
        private static readonly NemesisMark[] Marks = (NemesisMark[])System.Enum.GetValues(typeof(NemesisMark));
        private static readonly NemesisHabit[] Habits = (NemesisHabit[])System.Enum.GetValues(typeof(NemesisHabit));

        public static void RollIfNeeded(NemesisData? data, Pawn? pawn)
        {
            if (data == null || data.tellsRolled)
                return;

            data.voice = Voices.RandomElement();
            data.mark = Marks.RandomElement();
            data.habit = Habits.RandomElement();
            if (string.IsNullOrEmpty(data.weaponDefName) && pawn != null)
            {
                ThingDef? weapon = NemesisProgression.PeekWeaponDef(data.combatFocus,
                    pawn.Faction?.def?.techLevel ?? TechLevel.Industrial);
                data.weaponDefName = weapon?.defName;
            }
            if (data.huntStartTick <= 0)
                data.huntStartTick = Find.TickManager.TicksGame;
            data.tellsRolled = true;
            RecordNote(data, "Nemesis_Note_TellsRolled",
                NemesisNote.Key(VoiceKey(data.voice)),
                data.weaponDefName is string w && w.Length > 0 ? NemesisNote.Thing(w) : NemesisNote.Key("Nemesis_Weapon_Unknown"),
                NemesisNote.Key(MarkKey(data.mark)),
                NemesisNote.Key(HabitKey(data.habit)));
        }

        public static string VoiceKey(NemesisVoice v) => v switch
        {
            NemesisVoice.Mocking => "Nemesis_Voice_Mocking",
            NemesisVoice.Zealot => "Nemesis_Voice_Zealot",
            NemesisVoice.Casual => "Nemesis_Voice_Casual",
            _ => "Nemesis_Voice_Cold",
        };

        public static string MarkKey(NemesisMark m) => m switch
        {
            NemesisMark.ArrangedCorpse => "Nemesis_Mark_ArrangedCorpse",
            NemesisMark.Scrawl => "Nemesis_Mark_Scrawl",
            NemesisMark.SabotageToken => "Nemesis_Mark_SabotageToken",
            _ => "Nemesis_Mark_CallingCard",
        };

        public static string HabitKey(NemesisHabit h) => h switch
        {
            NemesisHabit.FoodFirst => "Nemesis_Habit_FoodFirst",
            NemesisHabit.LeaveOneAlive => "Nemesis_Habit_LeaveOneAlive",
            NemesisHabit.EarlyRetreat => "Nemesis_Habit_EarlyRetreat",
            NemesisHabit.SameBuilding => "Nemesis_Habit_SameBuilding",
            _ => "Nemesis_Habit_PowerFirst",
        };

        public static string WeaponLabel(NemesisData data)
        {
            if (data?.weaponDefName is string defName && defName.Length > 0)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def != null)
                    return def.LabelCap;
            }
            return "Nemesis_Weapon_Unknown".Translate();
        }

        /// <summary>Add a dossier note: a translation key and its arguments (see <see cref="NemesisNote"/>).</summary>
        public static void RecordNote(NemesisData data, string key, params string[] args)
        {
            if (data == null || string.IsNullOrEmpty(key))
                return;
            if (data.notes == null)
                data.notes = new List<NemesisNote>();
            data.notes.Add(new NemesisNote
            {
                tick = Find.TickManager.TicksGame,
                key = key,
                args = args.Length > 0 ? new List<string>(args) : null,
            });
            while (data.notes.Count > 24)
                data.notes.RemoveAt(0);
        }

        public static void RecordGear(NemesisData data, Pawn? pawn)
        {
            if (data == null || pawn?.equipment?.Primary == null)
                return;
            data.lastGearSeen = pawn.equipment.Primary.LabelCap;
            RecordNote(data, "Nemesis_Note_Gear", NemesisNote.Thing(pawn.equipment.Primary.def.defName));
        }

        public static void MaybeLeaveMark(NemesisData data, Map map, IntVec3? near, bool always = false)
        {
            if (data == null || map == null || !always && Rand.Value > 0.45f)
                return;

            RecordNote(data, "Nemesis_Note_Mark", NemesisNote.Key(MarkKey(data.mark)));

            IntVec3 cell = near ?? map.Center;
            if (!cell.InBounds(map) || !cell.Standable(map))
                cell = CellFinderLoose.RandomCellWith(c => c.Standable(map), map, 40);
            if (!cell.IsValid)
                return;

            switch (data.mark)
            {
                case NemesisMark.CallingCard:
                case NemesisMark.SabotageToken:
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(NemesisDefOf.Nemesis_CallingCard), cell, map, ThingPlaceMode.Near);
                    break;
                case NemesisMark.Scrawl:
                    // Their own art now, not just a note (AZR-536).
                    FilthMaker.TryMakeFilth(cell, map, NemesisDefOf.Nemesis_Scrawl, 1);
                    break;
                case NemesisMark.ArrangedCorpse:
                    // A ring of blood around their sign.
                    FilthMaker.TryMakeFilth(cell, map, NemesisDefOf.Nemesis_Scrawl, 1);
                    foreach (IntVec3 c in GenRadial.RadialCellsAround(cell, 1.9f, false))
                    {
                        if (c.InBounds(map) && c.Standable(map))
                            FilthMaker.TryMakeFilth(c, map, ThingDefOf.Filth_Blood, 1);
                    }
                    break;
            }
        }

        public static float FleeHealth(NemesisData data)
        {
            return data?.habit == NemesisHabit.EarlyRetreat ? 0.45f : 0.3f;
        }
    }
}

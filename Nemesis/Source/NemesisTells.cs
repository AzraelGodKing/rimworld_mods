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

    public class NemesisNote : IExposable
    {
        public int tick;
        public string text;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tick, "tick", 0);
            Scribe_Values.Look(ref text, "text");
        }
    }

    public class NemesisEpitaph : IExposable
    {
        public string name;
        public string factionName;
        public string focusKey;
        public string voiceKey;
        public string markKey;
        public string habitKey;
        public string targetName;
        public string endKey;
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

    /// <summary>Persistent signatures for one hunt (AZR-146). Never required to end it.</summary>
    public static class NemesisTells
    {
        public static void RollIfNeeded(NemesisData data, Pawn pawn)
        {
            if (data == null || data.tellsRolled)
                return;

            data.voice = (NemesisVoice)Rand.RangeInclusive(0, 3);
            data.mark = (NemesisMark)Rand.RangeInclusive(0, 3);
            data.habit = (NemesisHabit)Rand.RangeInclusive(0, 4);
            if (string.IsNullOrEmpty(data.weaponDefName) && pawn != null)
            {
                ThingDef weapon = NemesisProgression.PeekWeaponDef(data.combatFocus,
                    pawn.Faction?.def?.techLevel ?? TechLevel.Industrial);
                data.weaponDefName = weapon?.defName;
            }
            if (data.huntStartTick <= 0)
                data.huntStartTick = Find.TickManager.TicksGame;
            data.tellsRolled = true;
            RecordNote(data, "Nemesis_Note_TellsRolled".Translate(
                VoiceKey(data.voice).Translate(),
                WeaponLabel(data),
                MarkKey(data.mark).Translate(),
                HabitKey(data.habit).Translate()));
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
            if (!string.IsNullOrEmpty(data?.weaponDefName))
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(data.weaponDefName);
                if (def != null)
                    return def.LabelCap;
            }
            return "Nemesis_Weapon_Unknown".Translate();
        }

        public static void RecordNote(NemesisData data, string text)
        {
            if (data == null || string.IsNullOrEmpty(text))
                return;
            if (data.notes == null)
                data.notes = new List<NemesisNote>();
            data.notes.Add(new NemesisNote
            {
                tick = Find.TickManager.TicksGame,
                text = text,
            });
            while (data.notes.Count > 24)
                data.notes.RemoveAt(0);
        }

        public static void RecordSighting(NemesisData data, Map map)
        {
            if (data == null || map == null)
                return;
            data.lastKnownTile = map.Tile;
            data.lastKnownTileLabel = Find.WorldGrid?.LongLatOf(map.Tile).ToString()
                ?? map.Parent?.LabelCap
                ?? map.ToString();
            RecordNote(data, "Nemesis_Note_Sighting".Translate(data.lastKnownTileLabel));
        }

        public static void RecordGear(NemesisData data, Pawn pawn)
        {
            if (data == null || pawn?.equipment?.Primary == null)
                return;
            data.lastGearSeen = pawn.equipment.Primary.LabelCap;
            if (!string.IsNullOrEmpty(data.lastGearSeen))
                RecordNote(data, "Nemesis_Note_Gear".Translate(data.lastGearSeen));
        }

        public static void MaybeLeaveMark(NemesisData data, Map map, IntVec3? near)
        {
            if (data == null || map == null || Rand.Value > 0.45f)
                return;

            RecordNote(data, "Nemesis_Note_Mark".Translate(MarkKey(data.mark).Translate()));

            if (data.mark != NemesisMark.CallingCard && data.mark != NemesisMark.SabotageToken)
                return;

            ThingDef card = DefDatabase<ThingDef>.GetNamedSilentFail("Nemesis_CallingCard");
            if (card == null)
                return;
            IntVec3 cell = near ?? map.Center;
            if (!cell.InBounds(map) || !cell.Standable(map))
                cell = CellFinderLoose.RandomCellWith(c => c.Standable(map), map, 40);
            if (!cell.IsValid)
                return;
            Thing thing = ThingMaker.MakeThing(card);
            GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
        }

        public static float FleeHealth(NemesisData data)
        {
            return data?.habit == NemesisHabit.EarlyRetreat ? 0.45f : 0.3f;
        }
    }
}

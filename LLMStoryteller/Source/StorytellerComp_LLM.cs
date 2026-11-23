using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LLMStoryteller
{
    public class StorytellerCompProperties_LLM : StorytellerCompProperties
    {
        /// <summary>No threats are offered before this many days, like Cassandra's 11.</summary>
        public float minDaysForThreats = 11f;
        /// <summary>No misc incidents are offered before this many days.</summary>
        public float minDaysForMisc = 5f;

        public StorytellerCompProperties_LLM()
        {
            compClass = typeof(StorytellerComp_LLM);
        }
    }

    /// <summary>
    /// The model's turn: about once a day per home map, offer it the incidents that can fire now and
    /// fire the one it picks through the normal incident worker. The model only ever names a defName
    /// from the list; everything it says is checked here before anything happens.
    /// </summary>
    public class StorytellerComp_LLM : StorytellerComp
    {
        StorytellerCompProperties_LLM Props => (StorytellerCompProperties_LLM)props;

        static readonly IncidentCategoryDef[] Categories =
        {
            IncidentCategoryDefOf.ThreatBig, IncidentCategoryDefOf.ThreatSmall, IncidentCategoryDefOf.Misc
        };

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            if (!(target is Map map) || !map.IsPlayerHome) yield break;
            GameComponent_LLMStoryteller state = GameComponent_LLMStoryteller.Get;
            LLMStorytellerSettings s = LLMStorytellerMod.Settings;
            if (state == null || !s.useModel) yield break;

            PendingDecision pending = state.Pending;
            if (pending != null)
            {
                if (Find.Maps.All(m => m.uniqueID != pending.MapId)) state.Pending = null;
                else if (pending.MapId != map.uniqueID || !pending.Task.IsCompleted) yield break;
                else
                {
                    state.Pending = null;
                    FiringIncident fi = Conclude(state, map, pending, s);
                    if (fi != null) yield return fi;
                    yield break;
                }
            }

            if (!state.DecisionDue(map)) yield break;
            Ask(state, map, s);
        }

        void Ask(GameComponent_LLMStoryteller state, Map map, LLMStorytellerSettings s)
        {
            List<MenuOption> menu = BuildMenu(map, s.maxMenuOptions);
            if (menu.Count == 0)
            {
                state.ScheduleNext(map);
                return;
            }
            string name = Find.Storyteller.def.label;
            var p = new PendingDecision
            {
                MapId = map.uniqueID,
                Menu = menu,
                System = StoryPrompt.System(name, s.personality, LLMStorytellerSettings.MinIntensity, s.maxIntensity),
                User = StoryPrompt.User(ColonySnapshotReader.Read(map), menu, state.RecentDecisions),
                Attempt = 1
            };
            p.Task = LlmClient.Ask(s, p.System, p.User);
            state.Pending = p;
        }

        FiringIncident Conclude(GameComponent_LLMStoryteller state, Map map, PendingDecision p, LLMStorytellerSettings s)
        {
            LlmResult r = p.Task.Result;
            string error = r.Error;
            StoryDecision d = null;
            if (r.Ok) StoryDecision.TryParse(r.Content, p.Menu, LLMStorytellerSettings.MinIntensity, s.maxIntensity, out d, out error);

            if (d == null)
            {
                if (p.Attempt < GameComponent_LLMStoryteller.MaxAttempts)
                {
                    Log.Message("[LLMStoryteller] Retrying once: " + error);
                    p.Attempt++;
                    p.Task = LlmClient.Ask(s, p.System, p.User);
                    state.Pending = p;
                }
                else state.EnterFallback(map, error);
                return null;
            }

            string teller = Find.Storyteller.def.LabelCap;
            if (d.Quiet)
            {
                state.RecordDecision(map, "a quiet day - " + d.Reason);
                if (s.reasoningLetters)
                    Messages.Message("LLMStoryteller_QuietDay".Translate(teller, d.Reason), MessageTypeDefOf.NeutralEvent, historical: false);
                return null;
            }

            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(d.Option.DefName);
            IncidentParms parms = def != null && Allowed(def, map) ? StorytellerUtility.DefaultParmsNow(def.category, map) : null;
            if (parms == null)
            {
                state.EnterFallback(map, "\"" + d.Option.DefName + "\" is not allowed now.");
                return null;
            }
            if (def.pointsScaleable) parms.points *= d.Intensity;
            if (!def.Worker.CanFireNow(parms))
            {
                state.EnterFallback(map, "\"" + def.defName + "\" can no longer fire.");
                return null;
            }

            state.RecordDecision(map, def.defName + (def.pointsScaleable ? " at intensity " + d.Intensity.ToString("0.##") : "") + " - " + d.Reason);
            if (s.reasoningLetters)
                Find.LetterStack.ReceiveLetter("LLMStoryteller_LetterLabel".Translate(teller),
                    "LLMStoryteller_LetterText".Translate(teller, def.LabelCap, d.Reason), LetterDefOf.NeutralEvent);
            return new FiringIncident(def, this, parms);
        }

        bool Allowed(IncidentDef def, Map map)
        {
            if (!Categories.Contains(def.category)) return false;
            if (def.targetTags == null || !def.targetTags.Contains(IncidentTargetTagDefOf.Map_PlayerHome)) return false;
            float days = GenDate.DaysPassedFloat;
            bool threat = def.category == IncidentCategoryDefOf.ThreatBig || def.category == IncidentCategoryDefOf.ThreatSmall;
            if (days < (threat ? Props.minDaysForThreats : Props.minDaysForMisc)) return false;
            return IncidentChanceFinal(def, map) > 0f;
        }

        /// <summary>
        /// Incidents that can fire on this map now, sampled by their usual chance so the list stays short
        /// and still looks like what Cassandra would roll from.
        /// </summary>
        List<MenuOption> BuildMenu(Map map, int max)
        {
            var pool = new List<(IncidentDef def, float weight)>();
            foreach (IncidentDef def in DefDatabase<IncidentDef>.AllDefsListForReading)
            {
                if (!Allowed(def, map)) continue;
                if (!def.Worker.CanFireNow(StorytellerUtility.DefaultParmsNow(def.category, map))) continue;
                pool.Add((def, IncidentChanceFinal(def, map)));
            }
            var menu = new List<MenuOption>();
            while (pool.Count > 0 && menu.Count < max)
            {
                var pick = pool.RandomElementByWeight(x => x.weight);
                pool.Remove(pick);
                menu.Add(new MenuOption(pick.def.defName, pick.def.label, pick.def.category.defName));
            }
            return menu;
        }
    }
}

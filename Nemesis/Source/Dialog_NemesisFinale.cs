using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Nemesis
{
    /// <summary>Staged finale choice when escape count hits the cap: duel or refuse (largest assault).</summary>
    public class Dialog_NemesisFinale : Window
    {
        private readonly NemesisData _data;

        public override Vector2 InitialSize => new Vector2(520f, 360f);

        public Dialog_NemesisFinale(NemesisData data)
        {
            _data = data;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("Nemesis_Finale_Title".Translate(_data.nemesisName));
            Text.Font = GameFont.Small;
            listing.Gap(8f);
            listing.Label("Nemesis_Finale_Body".Translate(
                _data.nemesisName, NemesisTaunts.TargetPhrase(_data), _data.PhaseLabelKeyed()));
            listing.Gap(18f);

            if (listing.ButtonText("Nemesis_Finale_AcceptDuel".Translate()))
            {
                AcceptDuel();
                Close();
            }
            listing.Gap(6f);
            if (listing.ButtonText("Nemesis_Finale_Refuse".Translate()))
            {
                RefuseAssault();
                Close();
            }

            listing.End();
        }

        private void AcceptDuel()
        {
            _data.finaleDuelActive = true;
            Map map = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            Pawn nemesis = comp?.FindNemesisPawn();
            if (map == null || nemesis == null || nemesis.Dead)
            {
                RefuseAssault();
                return;
            }

            if (nemesis.Spawned)
                nemesis.DeSpawn(DestroyMode.WillReplace);
            if (nemesis.IsWorldPawn())
                Find.WorldPawns.RemovePawn(nemesis);

            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out IntVec3 cell))
                cell = CellFinder.RandomEdgeCell(map);

            // Prefer a marked cell near the target if present.
            Pawn target = comp.FindTargetPawn();
            if (target != null && target.Spawned && target.Map == map)
            {
                if (CellFinder.TryFindRandomCellNear(target.Position, map, 18,
                        c => c.Standable(map) && !c.Fogged(map) && c.DistanceTo(target.Position) >= 6f, out IntVec3 near))
                    cell = near;
            }

            GenSpawn.Spawn(nemesis, cell, map);
            NemesisRegistry.CachedNemesis = nemesis;
            NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;

            if (nemesis.Faction != null && !nemesis.Faction.IsPlayer)
            {
                LordMaker.MakeNewLord(
                    nemesis.Faction,
                    new LordJob_AssaultColony(nemesis.Faction, canKidnap: false, canTimeoutOrFlee: false),
                    map,
                    new[] { nemesis });
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_DuelTitle".Translate(_data.nemesisName),
                "Nemesis_Letter_DuelBody".Translate(_data.nemesisName, NemesisTaunts.TargetPhrase(_data)),
                LetterDefOf.ThreatBig,
                nemesis);
        }

        private void RefuseAssault()
        {
            _data.finaleDuelActive = false;
            Map map = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            if (map == null)
            {
                NemesisActions.CommsTaunt(_data, Find.AnyPlayerHomeMap);
                return;
            }

            // Largest assault variant: personal spawn + heavy raid.
            NemesisActions.ExecuteFinaleAssault(_data, map);
        }
    }
}

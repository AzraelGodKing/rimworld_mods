using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public class Dialog_NemesisResolution : Window
    {
        private readonly NemesisData _data;
        private readonly Pawn _nemesis;

        public override Vector2 InitialSize => new Vector2(540f, 560f);

        public Dialog_NemesisResolution(NemesisData data, Pawn nemesis)
        {
            _data = data;
            _nemesis = nemesis;
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
            listing.Label("Nemesis_Dialog_Title".Translate(_data.nemesisName));
            Text.Font = GameFont.Small;
            listing.Gap(8f);

            string body = _data.trigger switch
            {
                NemesisTrigger.KilledAlly => "Nemesis_Dialog_Body_KilledAlly".Translate(),
                NemesisTrigger.PrisonerEscaped => "Nemesis_Dialog_Body_Prisoner".Translate(),
                NemesisTrigger.SlaveEscaped => "Nemesis_Dialog_Body_Slave".Translate(),
                NemesisTrigger.Fixation => "Nemesis_Dialog_Body_Fixation".Translate(),
                NemesisTrigger.WoundedAndEscaped => "Nemesis_Dialog_Body_Wounded".Translate(),
                _ => "Nemesis_Dialog_Body_Default".Translate(_data.nemesisName),
            };
            listing.Label(body);
            listing.Gap(14f);

            if (listing.ButtonText("Nemesis_Dialog_Execute".Translate()))
            {
                ApplyOutcome(NemesisOutcome.Execute);
                Close();
            }
            listing.Gap(4f);
            if (listing.ButtonText("Nemesis_Dialog_Release".Translate()))
            {
                ApplyOutcome(NemesisOutcome.Release);
                Close();
            }
            listing.Gap(4f);
            if (listing.ButtonText("Nemesis_Dialog_Keep".Translate()))
            {
                ApplyOutcome(NemesisOutcome.KeepPrisoner);
                Close();
            }
            listing.Gap(4f);
            int truceDays = NemesisMod.Settings?.truceDurationDays ?? 30;
            if (listing.ButtonText("Nemesis_Dialog_Truce".Translate(truceDays)))
            {
                ApplyOutcome(NemesisOutcome.Truce);
                Close();
            }

            if (ResistanceBroken())
            {
                listing.Gap(4f);
                if (listing.ButtonText("Nemesis_Dialog_Recruit".Translate()))
                {
                    ApplyOutcome(NemesisOutcome.Recruit);
                    Close();
                }
            }

            Faction homeFaction = ResolveHomeFaction();
            if (homeFaction != null)
            {
                listing.Gap(4f);
                int silver = RansomSilver();
                if (listing.ButtonText("Nemesis_Dialog_Ransom".Translate(silver)))
                {
                    ApplyOutcome(NemesisOutcome.Ransom);
                    Close();
                }
            }

            Faction enemyBuyer = FindHostileToNemesisFaction(homeFaction);
            if (enemyBuyer != null)
            {
                listing.Gap(4f);
                int bounty = BountySilver();
                if (listing.ButtonText("Nemesis_Dialog_HandToEnemies".Translate(enemyBuyer.Name, bounty)))
                {
                    ApplyOutcome(NemesisOutcome.HandToEnemies);
                    Close();
                }
            }

            listing.End();
        }

        private bool ResistanceBroken()
        {
            if (_nemesis?.guest == null || _nemesis.Dead) return false;
            return _nemesis.guest.resistance <= 0.01f;
        }

        private Faction ResolveHomeFaction()
        {
            Faction faction = (_data.faction != null && !_data.faction.IsPlayer)
                ? _data.faction
                : NemesisActions.FindFaction(_data);
            if (faction == null || faction.IsPlayer || faction.defeated) return null;
            return faction;
        }

        private static Faction FindHostileToNemesisFaction(Faction nemesisFaction)
        {
            if (nemesisFaction == null) return null;
            foreach (Faction f in Find.FactionManager.AllFactions)
            {
                if (f == null || f.IsPlayer || f.defeated || f.Hidden || f == nemesisFaction) continue;
                if (f.HostileTo(nemesisFaction))
                    return f;
            }
            return null;
        }

        private int RansomSilver() =>
            Mathf.Clamp(200 + (int)(_data.EffectiveAggression * 80f), 200, 1200);

        private int BountySilver() =>
            Mathf.Clamp(400 + (int)(_data.EffectiveAggression * 120f), 400, 2000);

        private void ApplyOutcome(NemesisOutcome outcome)
        {
            Faction faction = ResolveHomeFaction();

            switch (outcome)
            {
                case NemesisOutcome.Execute:
                    if (_nemesis != null && !_nemesis.Dead)
                        _nemesis.Kill(null);

                    faction?.TryAffectGoodwillWith(Faction.OfPlayer, -30, canSendMessage: true, canSendHostilityLetter: true);

                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_ExecutedTitle".Translate(_data.nemesisName),
                        "Nemesis_Letter_ExecutedBody".Translate(_data.nemesisName, faction != null ? _data.factionName : ""),
                        LetterDefOf.NeutralEvent);
                    break;

                case NemesisOutcome.Release:
                    SendNemesisAway(PawnDiscardDecideMode.Decide);

                    faction?.TryAffectGoodwillWith(Faction.OfPlayer, 20, canSendMessage: true, canSendHostilityLetter: false);

                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_ReleasedTitle".Translate(_data.nemesisName),
                        "Nemesis_Letter_ReleasedBody".Translate(_data.nemesisName, faction != null ? _data.factionName : ""),
                        LetterDefOf.PositiveEvent);
                    break;

                case NemesisOutcome.KeepPrisoner:
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_KeptTitle".Translate(_data.nemesisName),
                        "Nemesis_Letter_KeptBody".Translate(_data.nemesisName),
                        LetterDefOf.NeutralEvent,
                        _nemesis);
                    break;

                case NemesisOutcome.Truce:
                    SendNemesisAway(PawnDiscardDecideMode.KeepForever);

                    int days = NemesisMod.Settings?.truceDurationDays ?? 30;
                    _data.truceUntilTick = Find.TickManager.TicksGame + days * 60000;
                    _data.nextTruceEventTick = Find.TickManager.TicksGame + 120000;

                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_TruceTitle".Translate(_data.nemesisName),
                        "Nemesis_Letter_TruceBody".Translate(_data.nemesisName, days),
                        LetterDefOf.NeutralEvent);
                    break;

                case NemesisOutcome.Recruit:
                    if (!ResistanceBroken())
                    {
                        ApplyOutcome(NemesisOutcome.KeepPrisoner);
                        return;
                    }
                    if (_nemesis != null && !_nemesis.Dead)
                    {
                        // Attempt: high chance when resistance is broken; failure keeps prisoner.
                        if (Rand.Chance(0.65f))
                        {
                            _nemesis.SetFaction(Faction.OfPlayer);
                            _nemesis.guest?.SetGuestStatus(null);
                            Find.LetterStack.ReceiveLetter(
                                "Nemesis_Letter_RecruitedTitle".Translate(_data.nemesisName),
                                "Nemesis_Letter_RecruitedBody".Translate(_data.nemesisName),
                                LetterDefOf.PositiveEvent,
                                _nemesis);
                        }
                        else
                        {
                            Find.LetterStack.ReceiveLetter(
                                "Nemesis_Letter_RecruitFailTitle".Translate(_data.nemesisName),
                                "Nemesis_Letter_RecruitFailBody".Translate(_data.nemesisName),
                                LetterDefOf.NegativeEvent,
                                _nemesis);
                            // Stay prisoner — hunt already ended at dialog open.
                        }
                    }
                    break;

                case NemesisOutcome.Ransom:
                    if (faction == null)
                    {
                        ApplyOutcome(NemesisOutcome.KeepPrisoner);
                        return;
                    }
                    {
                        int pay = RansomSilver();
                        DeliverSilver(pay);
                        faction.TryAffectGoodwillWith(Faction.OfPlayer, 15 + (int)_data.EffectiveAggression,
                            canSendMessage: true, canSendHostilityLetter: false);
                        if (_nemesis != null && !_nemesis.Dead)
                        {
                            _nemesis.guest?.SetGuestStatus(null);
                            if (faction != null)
                                _nemesis.SetFaction(faction);
                            SendNemesisAway(PawnDiscardDecideMode.Decide);
                        }
                        Find.LetterStack.ReceiveLetter(
                            "Nemesis_Letter_RansomTitle".Translate(_data.nemesisName),
                            "Nemesis_Letter_RansomBody".Translate(_data.nemesisName, faction.Name, pay),
                            LetterDefOf.PositiveEvent);
                    }
                    break;

                case NemesisOutcome.HandToEnemies:
                    {
                        Faction buyer = FindHostileToNemesisFaction(faction);
                        if (buyer == null)
                        {
                            ApplyOutcome(NemesisOutcome.KeepPrisoner);
                            return;
                        }
                        int pay = BountySilver();
                        DeliverSilver(pay);
                        buyer.TryAffectGoodwillWith(Faction.OfPlayer, 25 + (int)_data.EffectiveAggression,
                            canSendMessage: true, canSendHostilityLetter: false);
                        if (_nemesis != null && !_nemesis.Dead)
                        {
                            // Permanent end — leave the map in the buyer's custody.
                            _nemesis.SetFaction(buyer);
                            SendNemesisAway(PawnDiscardDecideMode.Decide);
                        }
                        Find.LetterStack.ReceiveLetter(
                            "Nemesis_Letter_BountyTitle".Translate(_data.nemesisName),
                            "Nemesis_Letter_BountyBody".Translate(_data.nemesisName, buyer.Name, pay),
                            LetterDefOf.NeutralEvent);
                    }
                    break;
            }

            NemesisRegistry.Clear();
        }

        private static void DeliverSilver(int amount)
        {
            Map map = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            if (map == null || amount <= 0) return;
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = amount;
            IntVec3 spot = DropCellFinder.TradeDropSpot(map);
            if (!spot.IsValid)
                spot = DropCellFinder.RandomDropSpot(map);
            DropPodUtility.DropThingsNear(spot, map, new List<Thing> { silver }, 110, canInstaDropDuringInit: false, leaveSlag: false);
        }

        private void SendNemesisAway(PawnDiscardDecideMode discardMode)
        {
            if (_nemesis == null || _nemesis.Dead) return;

            _nemesis.guest?.SetGuestStatus(null);

            if (_nemesis.Spawned)
                _nemesis.DeSpawn(DestroyMode.WillReplace);
            if (!_nemesis.IsWorldPawn())
                Find.WorldPawns.PassToWorld(_nemesis, discardMode);
        }
    }
}

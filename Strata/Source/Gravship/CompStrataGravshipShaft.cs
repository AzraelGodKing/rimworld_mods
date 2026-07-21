using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    public class CompProperties_StrataGravshipShaft : CompProperties
    {
        public CompProperties_StrataGravshipShaft()
        {
            compClass = typeof(CompStrataGravshipShaft);
        }
    }

    // G2: stable identity for gravship host shafts across pack/land so reconnect
    // never opens a second empty "New" underdeck when packed stairs replace temps.
    public class CompStrataGravshipShaft : ThingComp
    {
        public string shaftId;
        public string stackGuid;
        public int lastPocketMapId = -1;
        // G4: when false, pocket does not follow launch (host-side entitlement).
        public bool travelsWithShip = true;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureShaftId();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref shaftId, "strataShaftId");
            Scribe_Values.Look(ref stackGuid, "strataStackGuid");
            Scribe_Values.Look(ref lastPocketMapId, "strataLastPocketMapId", -1);
            Scribe_Values.Look(ref travelsWithShip, "strataTravelsWithShip", true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureShaftId();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (parent is not IStrataGravshipPortal
                || !StrataGravshipUtility.IsGravshipHostShaft(parent))
            {
                yield break;
            }
            yield return new Command_Toggle
            {
                defaultLabel = travelsWithShip
                    ? "Strata_TravelWithShipOn".Translate()
                    : "Strata_TravelWithShipOff".Translate(),
                defaultDesc = "Strata_TravelWithShipDesc".Translate(),
                isActive = () => travelsWithShip,
                toggleAction = () => travelsWithShip = !travelsWithShip,
                icon = TexCommand.ForbidOff,
            };
        }

        public void EnsureShaftId()
        {
            if (shaftId.NullOrEmpty())
            {
                shaftId = Guid.NewGuid().ToString("N");
            }
        }

        public void RememberPocket(Map pocket)
        {
            if (pocket != null)
            {
                lastPocketMapId = pocket.uniqueID;
            }
        }

        public void BindStack(string guid)
        {
            if (!guid.NullOrEmpty())
            {
                stackGuid = guid;
            }
        }
    }

    public static class StrataGravshipShaftIdentity
    {
        public static CompStrataGravshipShaft CompOf(Thing thing)
        {
            return thing?.TryGetComp<CompStrataGravshipShaft>();
        }

        public static string GetOrMintShaftId(Thing thing)
        {
            CompStrataGravshipShaft comp = CompOf(thing);
            if (comp == null)
            {
                return null;
            }
            comp.EnsureShaftId();
            return comp.shaftId;
        }

        public static Map FindMappedPocket(Thing shaft)
        {
            CompStrataGravshipShaft comp = CompOf(shaft);
            if (comp == null)
            {
                return null;
            }
            comp.EnsureShaftId();

            if (comp.lastPocketMapId >= 0)
            {
                Map byComp = FindMapById(comp.lastPocketMapId);
                if (byComp != null && Find.Maps.Contains(byComp))
                {
                    return byComp;
                }
            }

            foreach (StrataGravshipPortalTravel.PortalSnapshot snap
                in StrataGravshipPortalTravel.PeekSnapshots())
            {
                if (snap == null || snap.shaftId.NullOrEmpty()
                    || snap.shaftId != comp.shaftId || snap.pocketMapId < 0)
                {
                    continue;
                }
                Map bySnap = FindMapById(snap.pocketMapId);
                if (bySnap != null && Find.Maps.Contains(bySnap))
                {
                    comp.RememberPocket(bySnap);
                    return bySnap;
                }
            }

            return null;
        }

        public static MapPortal FindHostShaftById(Map host, string shaftId)
        {
            if (host == null || shaftId.NullOrEmpty())
            {
                return null;
            }
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not MapPortal portal || !portal.Spawned
                    || !StrataGravshipUtility.IsGravshipHostShaft(portal))
                {
                    continue;
                }
                CompStrataGravshipShaft comp = CompOf(portal);
                if (comp != null && comp.shaftId == shaftId)
                {
                    return portal;
                }
            }
            return null;
        }

        public static MapPortal FindPackedShaftById(RimWorld.Planet.Gravship ship, string shaftId)
        {
            if (ship == null || shaftId.NullOrEmpty())
            {
                return null;
            }
            var packed = HarmonyLib.AccessTools.Field(typeof(RimWorld.Planet.Gravship), "things")
                .GetValue(ship) as System.Collections.Generic.Dictionary<Thing, RimWorld.Planet.PositionData>;
            if (packed == null)
            {
                return null;
            }
            foreach (Thing thing in packed.Keys)
            {
                if (thing is MapPortal portal
                    && StrataGravshipUtility.IsGravshipHostShaft(portal)
                    && CompOf(portal)?.shaftId == shaftId)
                {
                    return portal;
                }
            }
            return null;
        }

        private static Map FindMapById(int uniqueId)
        {
            return StrataGravshipOrphanLevels.FindMapById(uniqueId);
        }
    }
}

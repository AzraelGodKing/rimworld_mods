using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_StrataShaftLink : CompProperties
    {
        public CompProperties_StrataShaftLink()
        {
            compClass = typeof(CompStrataShaftLink);
        }
    }

    // Persistent pair identity for a shaft↔landing portal. Survives broken
    // entrance/exit wiring so RelinkPortalsByPairId / DevMode can rewire.
    public class CompStrataShaftLink : ThingComp
    {
        public string pairGuid;

        public string ShortId
        {
            get
            {
                EnsurePairGuid();
                if (pairGuid.NullOrEmpty() || pairGuid.Length < 8)
                {
                    return pairGuid ?? "?";
                }
                return pairGuid.Substring(0, 8);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsurePairGuid();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref pairGuid, "strataPairGuid");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsurePairGuid();
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!Prefs.DevMode)
            {
                return null;
            }
            return "Pair ID: " + ShortId;
        }

        public void EnsurePairGuid()
        {
            CompStrataGravshipShaft grav = parent.TryGetComp<CompStrataGravshipShaft>();
            if (grav != null)
            {
                grav.EnsureShaftId();
                if (pairGuid.NullOrEmpty())
                {
                    pairGuid = grav.shaftId;
                }
                else if (grav.shaftId.NullOrEmpty())
                {
                    grav.shaftId = pairGuid;
                }
                else if (grav.shaftId != pairGuid && parent is not PocketMapExit)
                {
                    // Host gravship shaftId is the source of truth.
                    pairGuid = grav.shaftId;
                }
            }

            if (pairGuid.NullOrEmpty())
            {
                pairGuid = Guid.NewGuid().ToString("N");
            }
        }

        public void SetPairGuid(string guid)
        {
            if (guid.NullOrEmpty())
            {
                return;
            }
            pairGuid = guid;
            CompStrataGravshipShaft grav = parent.TryGetComp<CompStrataGravshipShaft>();
            if (grav != null && parent is not PocketMapExit)
            {
                grav.shaftId = guid;
            }
        }

        public static CompStrataShaftLink CompOf(Thing thing)
        {
            return thing?.TryGetComp<CompStrataShaftLink>();
        }

        public static string GetOrMintPairGuid(Thing thing)
        {
            CompStrataShaftLink comp = CompOf(thing);
            if (comp == null)
            {
                return null;
            }
            comp.EnsurePairGuid();
            return comp.pairGuid;
        }

        public static void SyncPairIds(Thing shaft, Thing landing)
        {
            CompStrataShaftLink shaftLink = CompOf(shaft);
            CompStrataShaftLink landingLink = CompOf(landing);
            if (shaftLink == null && landingLink == null)
            {
                return;
            }

            shaftLink?.EnsurePairGuid();
            landingLink?.EnsurePairGuid();

            string guid = null;
            CompStrataGravshipShaft grav = shaft?.TryGetComp<CompStrataGravshipShaft>();
            if (grav != null)
            {
                grav.EnsureShaftId();
                guid = grav.shaftId;
            }
            if (guid.NullOrEmpty() && shaftLink != null && !shaftLink.pairGuid.NullOrEmpty())
            {
                guid = shaftLink.pairGuid;
            }
            if (guid.NullOrEmpty() && landingLink != null && !landingLink.pairGuid.NullOrEmpty())
            {
                guid = landingLink.pairGuid;
            }
            if (guid.NullOrEmpty())
            {
                guid = Guid.NewGuid().ToString("N");
            }

            shaftLink?.SetPairGuid(guid);
            landingLink?.SetPairGuid(guid);
        }
    }

    // Ensure every Strata MapPortal def carries CompStrataShaftLink (including
    // ancient/quest shafts that have no comps block in XML).
    [StaticConstructorOnStartup]
    public static class CompStrataShaftLinkInjector
    {
        static CompStrataShaftLinkInjector()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!IsStrataPortalDef(def))
                {
                    continue;
                }
                if (def.comps == null)
                {
                    def.comps = new List<CompProperties>();
                }
                bool has = false;
                for (int i = 0; i < def.comps.Count; i++)
                {
                    if (def.comps[i] is CompProperties_StrataShaftLink)
                    {
                        has = true;
                        break;
                    }
                }
                if (!has)
                {
                    def.comps.Add(new CompProperties_StrataShaftLink());
                }
            }
        }

        public static bool IsStrataPortalDef(ThingDef def)
        {
            if (def?.thingClass == null || def.defName.NullOrEmpty())
            {
                return false;
            }
            if (!def.defName.StartsWith("Strata_"))
            {
                return false;
            }
            return typeof(MapPortal).IsAssignableFrom(def.thingClass);
        }
    }
}

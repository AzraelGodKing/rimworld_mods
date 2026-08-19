using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    public class CompProperties_FeedSpawner : CompProperties
    {
        public ThingDef thingToSpawn;
        public int spawnCount = 8;
        public IntRange spawnIntervalRange = new IntRange(90000, 150000);
        public int feedRadius = 8;
        public int feedConsumed = 2;

        public CompProperties_FeedSpawner()
        {
            compClass = typeof(CompFeedSpawner);
        }
    }

    public class CompFeedSpawner : ThingComp
    {
        private int ticksUntilSpawn;

        public CompProperties_FeedSpawner Props => (CompProperties_FeedSpawner)props;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref ticksUntilSpawn, "feedTicksUntilSpawn");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (ticksUntilSpawn <= 0)
            {
                ResetTimer();
            }
        }

        public override void CompTick()
        {
            if (!parent.Spawned)
            {
                return;
            }

            ticksUntilSpawn--;
            if (ticksUntilSpawn > 0)
            {
                return;
            }

            TrySpawn();
            ResetTimer();
        }

        public override string CompInspectStringExtra()
        {
            string feed = HasFeed(out _)
                ? "Homesteader_DairyFeedOk".Translate()
                : "Homesteader_DairyNeedsFeed".Translate();
            int hours = Mathf.Max(1, ticksUntilSpawn / 2500);
            return feed + "\n" + "Homesteader_NextSpawnHours".Translate(hours);
        }

        private void ResetTimer()
        {
            float factor = 1f;
            int q = GameComponent_HomesteaderYard.Get()?.GetPrizeQuality(parent) ?? 1;
            if (q >= 3)
            {
                factor = 0.75f;
            }
            else if (q >= 2)
            {
                factor = 0.9f;
            }

            ticksUntilSpawn = Mathf.RoundToInt(Props.spawnIntervalRange.RandomInRange * factor);
        }

        private void TrySpawn()
        {
            if (!HasFeed(out Thing feed))
            {
                return;
            }

            if (Props.thingToSpawn == null)
            {
                return;
            }

            Thing product = ThingMaker.MakeThing(Props.thingToSpawn);
            product.stackCount = Props.spawnCount;
            if (GenPlace.TryPlaceThing(product, parent.Position, parent.Map, ThingPlaceMode.Near))
            {
                feed.SplitOff(Mathf.Min(Props.feedConsumed, feed.stackCount)).Destroy(DestroyMode.Vanish);
                if (Rand.Chance(0.08f))
                {
                    GameComponent_HomesteaderYard.Get()?.BumpPrizeQuality(parent);
                }
            }
            else
            {
                product.Destroy();
            }
        }

        private bool HasFeed(out Thing feed)
        {
            feed = null;
            if (parent.Map == null)
            {
                return false;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(parent.Position, Props.feedRadius, true))
            {
                if (!cell.InBounds(parent.Map))
                {
                    continue;
                }

                List<Thing> things = parent.Map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t.def == ThingDefOf.Hay || t.def.defName == "Homesteader_AnimalMash")
                    {
                        feed = t;
                        return t.stackCount >= 1;
                    }
                }
            }

            return false;
        }
    }

    public class CompProperties_GuardGeese : CompProperties
    {
        public float farmRadius = 28f;
        public int edgeDepth = 8;
        public int cooldownTicks = 30000;

        public CompProperties_GuardGeese()
        {
            compClass = typeof(CompGuardGeese);
        }
    }

    public class CompGuardGeese : ThingComp
    {
        private int cooldownUntil;

        public CompProperties_GuardGeese Props => (CompProperties_GuardGeese)props;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref cooldownUntil, "geeseCooldownUntil");
        }

        public override void CompTickRare()
        {
            if (!parent.Spawned || Find.TickManager.TicksGame < cooldownUntil)
            {
                return;
            }

            Map map = parent.Map;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p.Dead || p.Downed || p.Faction == null || !p.HostileTo(Faction.OfPlayer))
                {
                    continue;
                }

                if (!p.Position.CloseToEdge(map, Props.edgeDepth))
                {
                    continue;
                }

                if (p.Position.DistanceTo(parent.Position) > Props.farmRadius)
                {
                    continue;
                }

                cooldownUntil = Find.TickManager.TicksGame + Props.cooldownTicks;
                Find.LetterStack.ReceiveLetter(
                    "Homesteader_GeeseLetterLabel".Translate(),
                    "Homesteader_GeeseLetterText".Translate(p.LabelShort),
                    LetterDefOf.ThreatSmall,
                    p);
                return;
            }
        }
    }

    public class CompProperties_Farmstand : CompProperties
    {
        public float visitorRange = 10f;
        public int checkInterval = 18000;

        public CompProperties_Farmstand()
        {
            compClass = typeof(CompFarmstand);
        }
    }

    public class CompFarmstand : ThingComp
    {
        public CompProperties_Farmstand Props => (CompProperties_Farmstand)props;

        public override void CompTick()
        {
            if (!parent.Spawned || Find.TickManager.TicksGame % Props.checkInterval != 0)
            {
                return;
            }

            Map map = parent.Map;
            if (!HasVisitor(map))
            {
                return;
            }

            Thing stock = FindStock();
            if (stock == null)
            {
                return;
            }

            int take = Mathf.Clamp(stock.stackCount / 5, 1, 8);
            int silver = Mathf.Max(1, Mathf.RoundToInt(stock.MarketValue * take * 0.85f));
            stock.SplitOff(take).Destroy(DestroyMode.Vanish);
            Thing money = ThingMaker.MakeThing(ThingDefOf.Silver);
            money.stackCount = silver;
            GenPlace.TryPlaceThing(money, parent.Position, map, ThingPlaceMode.Near);
            GameComponent_HomesteaderYard.Get()?.AddBrand(0.6f);
            Messages.Message(
                "Homesteader_FarmstandSale".Translate(take, stock.LabelNoCount, silver),
                parent,
                MessageTypeDefOf.PositiveEvent);
        }

        public override string CompInspectStringExtra()
        {
            float brand = GameComponent_HomesteaderYard.Get()?.brand ?? 0f;
            return "Homesteader_FarmstandBrand".Translate(brand.ToString("F0"));
        }

        private bool HasVisitor(Map map)
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (!p.RaceProps.Humanlike || p.Faction == Faction.OfPlayer || p.HostileTo(Faction.OfPlayer))
                {
                    continue;
                }

                if (p.Position.DistanceTo(parent.Position) <= Props.visitorRange)
                {
                    return true;
                }
            }

            return false;
        }

        private Thing FindStock()
        {
            foreach (IntVec3 cell in parent.OccupiedRect())
            {
                List<Thing> things = parent.Map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t != parent && HomesteaderPantry.IsPreservedFood(t.def) && t.stackCount > 0)
                    {
                        return t;
                    }
                }
            }

            return null;
        }
    }

    public class CompProperties_WaterSense : CompProperties
    {
        public CompProperties_WaterSense()
        {
            compClass = typeof(CompWaterSense);
        }
    }

    public class CompWaterSense : ThingComp
    {
        public override void CompTick()
        {
            if (!parent.Spawned || Find.TickManager.TicksGame % CompSpawnerBias.Pulse != 0)
            {
                return;
            }

            CompSpawnerBias.ApplyFactor(parent, RainAware.FillFactor(parent));
        }

        public override string CompInspectStringExtra()
        {
            return RainAware.Inspect(parent);
        }
    }

    public class CompProperties_BeehiveBloom : CompProperties
    {
        public CompProperties_BeehiveBloom()
        {
            compClass = typeof(CompBeehiveBloom);
        }
    }

    public class CompBeehiveBloom : ThingComp
    {
        public override void CompTick()
        {
            if (!parent.Spawned || Find.TickManager.TicksGame % CompSpawnerBias.Pulse != 0)
            {
                return;
            }

            CompSpawnerBias.ApplyFactor(parent, BloomUtility.HasBloom(parent) ? 1f : 2f);
        }

        public override string CompInspectStringExtra()
        {
            return BloomUtility.Inspect(parent);
        }
    }

    public class CompProperties_PrizeFlock : CompProperties
    {
        public CompProperties_PrizeFlock()
        {
            compClass = typeof(CompPrizeFlock);
        }
    }

    public class CompPrizeFlock : ThingComp
    {
        public override void CompTick()
        {
            if (!parent.Spawned || Find.TickManager.TicksGame % CompSpawnerBias.Pulse != 0)
            {
                return;
            }

            if (parent.def?.defName == "Homesteader_ChickenCoop")
            {
                float factor = HomesteaderMod.Settings?.coopEggIntervalFactor ?? 1f;
                int q = GameComponent_HomesteaderYard.Get()?.GetPrizeQuality(parent) ?? 1;
                if (q >= 3)
                {
                    factor *= 0.75f;
                }
                else if (q >= 2)
                {
                    factor *= 0.9f;
                }

                CompSpawnerBias.ApplyFactor(parent, factor);
            }

            if (Rand.Chance(0.02f))
            {
                GameComponent_HomesteaderYard.Get()?.BumpPrizeQuality(parent);
            }
        }

        public override string CompInspectStringExtra()
        {
            GameComponent_HomesteaderYard yard = GameComponent_HomesteaderYard.Get();
            if (yard == null)
            {
                return null;
            }

            int q = yard.GetPrizeQuality(parent);
            if (!yard.FestivalActive && q < 2)
            {
                return null;
            }

            return "Homesteader_PrizeQuality".Translate(q);
        }
    }

    public class CompProperties_PowerPlantWaterwheel : CompProperties_Power
    {
        public float droughtFactor = 0.25f;

        public CompProperties_PowerPlantWaterwheel()
        {
            compClass = typeof(CompPowerPlantWaterwheel);
        }
    }

    public class CompPowerPlantWaterwheel : CompPowerPlant
    {
        public CompProperties_PowerPlantWaterwheel WheelProps => (CompProperties_PowerPlantWaterwheel)props;

        protected override float DesiredPowerOutput
        {
            get
            {
                float desired = base.DesiredPowerOutput;
                if (parent.Map != null
                    && StormproofSoftCompat.IsDrought(parent.Map)
                    && !StormproofSoftCompat.DroughtProtected(parent.Map))
                {
                    return desired * WheelProps.droughtFactor;
                }

                return desired;
            }
        }
    }

    public class PlaceWorker_OnMovingWater : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef checkingDef,
            IntVec3 loc,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            foreach (IntVec3 cell in GenAdj.OccupiedRect(loc, rot, checkingDef.Size))
            {
                if (IsMovingWater(cell, map))
                {
                    return true;
                }

                foreach (IntVec3 adj in GenAdj.CellsAdjacentCardinal(cell, Rot4.North, IntVec2.One))
                {
                    if (adj.InBounds(map) && IsMovingWater(adj, map))
                    {
                        return true;
                    }
                }
            }

            return "Homesteader_PlaceOnMovingWater".Translate();
        }

        private static bool IsMovingWater(IntVec3 cell, Map map)
        {
            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain == null)
            {
                return false;
            }

            string name = terrain.defName ?? string.Empty;
            return name.IndexOf("Moving") >= 0 || name.IndexOf("River") >= 0;
        }
    }
}

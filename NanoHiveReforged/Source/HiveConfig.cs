using Verse;

namespace NanoHiveReforged
{
    public enum HiveToggle
    {
        Buildings = 0,
        Items = 1,
        Gear = 2,
        Inventory = 3,
    }

    /// <summary>
    /// One hive's gameplay values. Saved with the hive (and once more as the
    /// colony default in <see cref="GameComponent_NanoHive"/>), so every
    /// Multiplayer client ticks the same numbers whatever its own mod settings say.
    /// </summary>
    public class HiveConfig : IExposable
    {
        public const int MinRange = 1;
        public const int MaxRange = 30;
        public const int MinRate = 1;
        public const int MaxRate = 10;

        public float range = 12f;
        public int repairPerTarget = 1;
        public bool repairBuildings = true;
        public bool repairItems = true;
        public bool repairGear = true;
        public bool repairInventory = true;

        public HiveConfig Copy()
        {
            var copy = new HiveConfig();
            copy.CopyFrom(this);
            return copy;
        }

        public void CopyFrom(HiveConfig other)
        {
            range = other.range;
            repairPerTarget = other.repairPerTarget;
            repairBuildings = other.repairBuildings;
            repairItems = other.repairItems;
            repairGear = other.repairGear;
            repairInventory = other.repairInventory;
            Clamp();
        }

        public bool Get(HiveToggle toggle)
        {
            switch (toggle)
            {
                case HiveToggle.Buildings: return repairBuildings;
                case HiveToggle.Items: return repairItems;
                case HiveToggle.Gear: return repairGear;
                default: return repairInventory;
            }
        }

        public void Set(HiveToggle toggle, bool value)
        {
            switch (toggle)
            {
                case HiveToggle.Buildings: repairBuildings = value; break;
                case HiveToggle.Items: repairItems = value; break;
                case HiveToggle.Gear: repairGear = value; break;
                default: repairInventory = value; break;
            }
        }

        public void Clamp()
        {
            if (range < MinRange) range = MinRange;
            if (range > MaxRange) range = MaxRange;
            if (repairPerTarget < MinRate) repairPerTarget = MinRate;
            if (repairPerTarget > MaxRate) repairPerTarget = MaxRate;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref range, "range", 12f);
            Scribe_Values.Look(ref repairPerTarget, "repairPerTarget", 1);
            Scribe_Values.Look(ref repairBuildings, "repairBuildings", true);
            Scribe_Values.Look(ref repairItems, "repairItems", true);
            Scribe_Values.Look(ref repairGear, "repairGear", true);
            Scribe_Values.Look(ref repairInventory, "repairInventory", true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Clamp();
            }
        }
    }
}

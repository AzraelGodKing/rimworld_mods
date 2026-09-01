using System.Collections.Generic;
using System.IO;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    /// <summary>AZR-71 — export the Legacy tab as a written colony chronicle.</summary>
    public static class ChronicleUtility
    {
        public static string BuildText()
        {
            var sb = new StringBuilder();
            var gc = GameComp_DeepColony.Instance;
            string colony = Find.World?.info?.name ?? "Colony";
            string surname = gc?.GetFounderSurname() ?? "—";
            sb.AppendLine(colony);
            sb.AppendLine("DC_ChronicleFounder".Translate(surname));
            sb.AppendLine();

            var living = new List<Pawn>();
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.FreeColonists == null) continue;
                living.AddRange(map.mapPawns.FreeColonists);
            }
            living.Sort((a, b) => a.LabelShort.CompareTo(b.LabelShort));

            sb.AppendLine("DC_ChroniclePeople".Translate(living.Count));
            for (int i = 0; i < living.Count; i++)
            {
                Pawn p = living[i];
                var comp = p.TryGetComp<Comp_DeepColony>();
                sb.Append("- ").Append(p.Name?.ToStringFull ?? p.LabelShort);
                if (ElderUtility.IsElder(p)) sb.Append(" [elder]");
                sb.AppendLine();
                if (comp?.mentor != null)
                    sb.Append("    ").AppendLine("DC_InspectMentor".Translate(comp.mentor.LabelShort));
                string lineage = comp?.TeachingLineageInspect();
                if (!lineage.NullOrEmpty())
                    sb.Append("    ").AppendLine(lineage);
                if (comp != null && comp.unlockedPerkDefNames.Count > 0)
                    sb.Append("    ").AppendLine("DC_InspectPerks".Translate(comp.unlockedPerkDefNames.Count));
                string triggers = TraumaTriggerUtility.InspectLine(p);
                if (!triggers.NullOrEmpty())
                    sb.Append("    ").AppendLine(triggers);
                Pawn heir = EstateUtility.ResolveNamedHeir(p);
                if (heir != null)
                    sb.Append("    ").AppendLine("DC_InspectWillHeir".Translate(heir.LabelShort));
            }

            sb.AppendLine();
            var remembrance = gc?.remembranceEntries;
            if (remembrance != null && remembrance.Count > 0)
            {
                sb.AppendLine("DC_LegacyRemembrance".Translate());
                for (int i = 0; i < remembrance.Count; i++)
                    sb.Append("- ").AppendLine(remembrance[i].name);
                sb.AppendLine();
            }

            var letters = gc?.familyLetters;
            if (letters != null && letters.Count > 0)
            {
                sb.AppendLine("DC_LegacyLetters".Translate());
                for (int i = 0; i < letters.Count; i++)
                {
                    sb.Append("- ").AppendLine(letters[i].title);
                    if (!letters[i].body.NullOrEmpty())
                        sb.Append("    ").AppendLine(letters[i].body);
                }
                sb.AppendLine();
            }

            if (gc != null)
            {
                string heirlooms = gc.FormatHeirloomChronicle();
                if (!heirlooms.NullOrEmpty())
                {
                    sb.AppendLine("DC_ChronicleHeirlooms".Translate());
                    sb.AppendLine(heirlooms);
                }
            }

            sb.AppendLine("DC_ChronicleFooter".Translate());
            return sb.ToString();
        }

        public static void Export()
        {
            string text = BuildText();
            GUIUtility.systemCopyBuffer = text;

            string colony = Find.World?.info?.name ?? "colony";
            string safe = colony;
            foreach (char c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            if (safe.NullOrEmpty()) safe = "colony";
            string path = Path.Combine(GenFilePaths.SaveDataFolderPath,
                "DeepColony_" + safe + "_chronicle.txt");
            try
            {
                File.WriteAllText(path, text, Encoding.UTF8);
            }
            catch (System.Exception e)
            {
                Log.Warning("[DeepColony] Chronicle write failed: " + e.Message);
                path = null;
            }

            if (path.NullOrEmpty())
            {
                Messages.Message("DC_ChronicleCopied".Translate(),
                    MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message("DC_ChronicleExported".Translate(path.Named("PATH")),
                    MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }
}

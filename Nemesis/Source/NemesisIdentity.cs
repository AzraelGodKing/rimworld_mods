using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>Persistent apparel/weapon tint so the antagonist is recognizable across raids.</summary>
    public static class NemesisIdentity
    {
        public static Color ColorFor(NemesisData data)
        {
            if (data == null) return new Color(0.45f, 0.12f, 0.12f);
            if (data.tintR >= 0f)
                return new Color(data.tintR, data.tintG, data.tintB);
            return RollAndStore(data);
        }

        public static Color RollAndStore(NemesisData data)
        {
            Color c = data.archetype switch
            {
                NemesisArchetype.Butcher => new Color(0.55f, 0.08f, 0.08f),
                NemesisArchetype.Saboteur => new Color(0.15f, 0.35f, 0.22f),
                NemesisArchetype.Trickster => new Color(0.45f, 0.22f, 0.55f),
                _ => new Color(0.25f, 0.25f, 0.32f),
            };
            // Slight per-hunt variance.
            c.r = Mathf.Clamp01(c.r + Rand.Range(-0.05f, 0.05f));
            c.g = Mathf.Clamp01(c.g + Rand.Range(-0.05f, 0.05f));
            c.b = Mathf.Clamp01(c.b + Rand.Range(-0.05f, 0.05f));
            data.tintR = c.r;
            data.tintG = c.g;
            data.tintB = c.b;
            return c;
        }

        public static void Apply(Pawn nemesis, NemesisData data)
        {
            if (nemesis == null || data == null) return;
            Color c = ColorFor(data);
            try
            {
                if (nemesis.apparel?.WornApparel != null)
                {
                    List<Apparel> worn = nemesis.apparel.WornApparel;
                    for (int i = 0; i < worn.Count; i++)
                    {
                        Apparel a = worn[i];
                        if (a == null) continue;
                        CompColorable colorable = a.TryGetComp<CompColorable>();
                        if (colorable != null)
                            colorable.SetColor(c);
                        else
                            a.DrawColor = c;
                    }
                }

                if (nemesis.equipment?.Primary != null)
                {
                    ThingWithComps w = nemesis.equipment.Primary;
                    CompColorable wc = w.TryGetComp<CompColorable>();
                    if (wc != null)
                        wc.SetColor(c);
                    else
                        w.DrawColor = c;
                }
            }
            catch
            {
                // fail-open
            }
        }
    }
}

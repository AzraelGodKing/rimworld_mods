using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    /// <summary>One page of the Deep Colony main tab.</summary>
    public abstract class DeepColonyHubPanel
    {
        public abstract string LabelKey { get; }
        public virtual Vector2 Size => new Vector2(760f, 560f);
        public virtual bool Visible => true;
        public abstract void DoWindowContents(Rect inRect);
    }

    /// <summary>
    /// The single Deep Colony main button (AZR-622). Perks, Legacy, Reputation and
    /// Body &amp; Codex are sub-tabs; a sub-tab hides while its system is off.
    /// </summary>
    public class MainTabWindow_DeepColony : MainTabWindow
    {
        private const float TabBarH = 32f;

        private static readonly DeepColonyHubPanel[] Panels =
        {
            new HubPanel_Perks(),
            new HubPanel_Legacy(),
            new HubPanel_Reputation(),
            new HubPanel_BodyCodex(),
        };

        // Kept across openings so the tab reopens where the player left it.
        private static int selected;
        private readonly List<TabRecord> tabs = new List<TabRecord>();

        public override Vector2 RequestedTabSize
        {
            get
            {
                Vector2 size = Vector2.zero;
                for (int i = 0; i < Panels.Length; i++)
                {
                    size.x = Mathf.Max(size.x, Panels[i].Size.x);
                    size.y = Mathf.Max(size.y, Panels[i].Size.y);
                }
                return new Vector2(size.x, size.y + TabBarH);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (!Panels[selected].Visible)
                selected = FirstVisible();

            tabs.Clear();
            for (int i = 0; i < Panels.Length; i++)
            {
                if (!Panels[i].Visible) continue;
                int index = i;
                tabs.Add(new TabRecord(Panels[i].LabelKey.Translate(), () => selected = index, selected == index));
            }

            Rect body = new Rect(inRect.x, inRect.y + TabBarH, inRect.width, inRect.height - TabBarH);
            Widgets.DrawMenuSection(body);
            TabDrawer.DrawTabs(body, tabs);
            Panels[selected].DoWindowContents(body.ContractedBy(10f));
        }

        private static int FirstVisible()
        {
            for (int i = 0; i < Panels.Length; i++)
            {
                if (Panels[i].Visible) return i;
            }
            return 1; // Legacy is always visible.
        }
    }
}

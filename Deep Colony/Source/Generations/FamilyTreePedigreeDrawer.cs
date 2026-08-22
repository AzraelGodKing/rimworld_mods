using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Pedigree layout: ancestors above, descendants below, orthogonal kin lines.
    /// </summary>
    public static class FamilyTreePedigreeDrawer
    {
        private const float NodeW = FamilyTreeDrawer.NodeW;
        private const float NodeH = FamilyTreeDrawer.NodeH;
        private const float GapX = 10f;
        private const float CoupleGap = 20f;
        private const float Stem = 22f;
        private const float Pad = 12f;
        private const float TitleH = 26f;
        private const int MaxInRow = 10;

        private static readonly Color LineColor = new Color(0.62f, 0.55f, 0.42f, 0.92f);

        private struct Slot
        {
            public Pawn pawn;
            public float x;
            public float y;
            public Rect Rect => new Rect(x, y, NodeW, NodeH);
            public float Cx => x + NodeW * 0.5f;
            public float Cy => y + NodeH * 0.5f;
            public float Top => y;
            public float Bottom => y + NodeH;
        }

        private struct HSeg
        {
            public float x1, x2, y;
        }

        private struct VSeg
        {
            public float x, y1, y2;
        }

        private class Layout
        {
            public float width;
            public float height;
            public float teachHeaderY = -1f;
            public readonly List<Slot> nodes = new List<Slot>();
            public readonly List<HSeg> hs = new List<HSeg>();
            public readonly List<VSeg> vs = new List<VSeg>();
        }

        public static Vector2 Measure(FamilyTreeSnapshot snap, bool includeTitle = true)
        {
            Layout lay = Build(snap, includeTitle);
            return new Vector2(lay.width, lay.height);
        }

        public static void Draw(Rect rect, FamilyTreeSnapshot snap, Action<Pawn> onClick, bool drawTitle = true)
        {
            if (snap?.focus == null) return;
            Layout lay = Build(snap, drawTitle);
            float ox = rect.x;
            float oy = rect.y;

            if (drawTitle)
            {
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(ox + Pad, oy, Mathf.Max(1f, lay.width - Pad * 2f), 24f),
                    "DC_FamilyTree_Title".Translate(snap.focus.LabelShort.Named("PAWN")));
            }

            if (!FamilyTreeUtility.HasAnyKin(snap))
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(ox + Pad, oy + (drawTitle ? TitleH : 0f), lay.width - Pad * 2f, 24f),
                    "DC_FamilyTree_Empty".Translate());
                GUI.color = Color.white;
                return;
            }

            Color old = GUI.color;
            GUI.color = LineColor;
            for (int i = 0; i < lay.hs.Count; i++)
            {
                HSeg s = lay.hs[i];
                float x1 = Mathf.Min(s.x1, s.x2) + ox;
                float len = Mathf.Abs(s.x2 - s.x1);
                if (len >= 0.5f)
                    Widgets.DrawLineHorizontal(x1, s.y + oy, len);
            }
            for (int i = 0; i < lay.vs.Count; i++)
            {
                VSeg s = lay.vs[i];
                float y1 = Mathf.Min(s.y1, s.y2) + oy;
                float len = Mathf.Abs(s.y2 - s.y1);
                if (len >= 0.5f)
                    Widgets.DrawLineVertical(s.x + ox, y1, len);
            }
            GUI.color = old;

            if (lay.teachHeaderY >= 0f)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(
                    new Rect(ox + Pad, oy + lay.teachHeaderY, lay.width - Pad * 2f, FamilyTreeDrawer.HeaderH),
                    "DC_FamilyTree_Teaching".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }

            for (int i = 0; i < lay.nodes.Count; i++)
            {
                Slot n = lay.nodes[i];
                Rect box = n.Rect;
                box.x += ox;
                box.y += oy;
                FamilyTreeDrawer.DrawNode(box, n.pawn, snap.focus, onClick);
            }
        }

        private static Layout Build(FamilyTreeSnapshot snap, bool includeTitle = true)
        {
            var lay = new Layout();
            if (snap?.focus == null)
            {
                lay.width = 280f;
                lay.height = 80f;
                return lay;
            }
            if (!FamilyTreeUtility.HasAnyKin(snap))
            {
                lay.width = 360f;
                lay.height = (includeTitle ? TitleH : 0f) + 48f;
                return lay;
            }

            SplitParents(snap.parents, out Pawn mother, out Pawn father);
            var momGps = new List<Pawn>();
            var dadGps = new List<Pawn>();
            if (mother != null) CopyCapped(momGps, FamilyTreeUtility.DirectParents(mother), 4);
            if (father != null) CopyCapped(dadGps, FamilyTreeUtility.DirectParents(father), 4);
            if (mother != null && father != null)
                AddLeftovers(snap.grandparents, momGps, dadGps);
            else if (mother != null)
                AddLeftoversTo(momGps, snap.grandparents);
            else if (father != null)
                AddLeftoversTo(dadGps, snap.grandparents);

            var blood = new List<Pawn>();
            CopyCapped(blood, snap.siblings, MaxInRow - 1);
            SortByAge(blood);
            blood.Add(snap.focus);

            var partners = new List<Pawn>();
            CopyCapped(partners, snap.partners, 3);

            var children = new List<Pawn>();
            CopyCapped(children, snap.children, MaxInRow);
            SortByAge(children);

            var gcByChild = new List<List<Pawn>>();
            bool hasGc = false;
            for (int i = 0; i < children.Count; i++)
            {
                var gcs = FamilyTreeUtility.DirectChildren(children[i]);
                SortByAge(gcs);
                if (gcs.Count > MaxInRow)
                    gcs.RemoveRange(MaxInRow, gcs.Count - MaxInRow);
                gcByChild.Add(gcs);
                if (gcs.Count > 0) hasGc = true;
            }

            var teach = new List<Pawn>();
            if (snap.mentor != null) teach.Add(snap.mentor);
            CopyCapped(teach, snap.apprentices, MaxInRow);

            bool hasGp = momGps.Count > 0 || dadGps.Count > 0;
            bool hasPar = mother != null || father != null;
            bool hasKids = children.Count > 0;
            bool hasTeach = teach.Count > 0;

            float y = includeTitle ? TitleH : 4f;
            float gpY = 0f, parY = 0f, egoY, kidY = 0f, gcY = 0f;
            if (hasGp)
            {
                gpY = y;
                y += NodeH + Stem;
            }
            if (hasPar)
            {
                parY = y;
                y += NodeH + Stem;
            }
            egoY = y;
            y += NodeH;
            if (hasKids)
            {
                y += Stem;
                kidY = y;
                y += NodeH;
            }
            if (hasGc)
            {
                y += Stem;
                gcY = y;
                y += NodeH;
            }

            float momGpW = RowW(momGps.Count);
            float dadGpW = RowW(dadGps.Count);
            int nPar = (mother != null ? 1 : 0) + (father != null ? 1 : 0);
            float parCenterDist = NodeW + CoupleGap;
            if (nPar == 2 && hasGp)
            {
                float need = momGpW * 0.5f + dadGpW * 0.5f + GapX;
                if (need > parCenterDist) parCenterDist = need;
            }

            float leftStick = nPar == 0 ? 0f : Mathf.Max(NodeW * 0.5f, mother != null ? momGpW * 0.5f : dadGpW * 0.5f);
            float rightStick = nPar < 2 ? leftStick : Mathf.Max(NodeW * 0.5f, dadGpW * 0.5f);
            float ancestorW = nPar == 2
                ? leftStick + parCenterDist + rightStick
                : (nPar == 1 ? Mathf.Max(NodeW, mother != null ? momGpW : dadGpW) : 0f);

            float bloodW = RowW(blood.Count);
            float partnerW = partners.Count == 0 ? 0f : CoupleGap + RowW(partners.Count);
            float egoW = bloodW + partnerW;

            float descW = 0f;
            if (hasKids)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    if (i > 0) descW += GapX;
                    descW += Mathf.Max(NodeW, RowW(gcByChild[i].Count));
                }
            }

            float contentW = Mathf.Max(Mathf.Max(ancestorW, egoW), descW);
            contentW = Mathf.Max(contentW, RowW(teach.Count), 280f);
            float totalW = contentW + Pad * 2f;
            float mid = totalW * 0.5f;

            Slot motherSlot = default;
            Slot fatherSlot = default;
            if (nPar == 2)
            {
                float blockStart = (totalW - ancestorW) * 0.5f;
                float motherCx = blockStart + leftStick;
                motherSlot = Place(lay, mother, motherCx - NodeW * 0.5f, parY);
                fatherSlot = Place(lay, father, motherSlot.x + parCenterDist, parY);
            }
            else if (mother != null)
            {
                motherSlot = Place(lay, mother, mid - NodeW * 0.5f, parY);
            }
            else if (father != null)
            {
                fatherSlot = Place(lay, father, mid - NodeW * 0.5f, parY);
            }

            if (momGps.Count > 0 && mother != null)
                PlaceCentered(lay, momGps, gpY, motherSlot.Cx);
            if (dadGps.Count > 0 && father != null)
                PlaceCentered(lay, dadGps, gpY, fatherSlot.Cx);

            float bloodAnchor = mid;
            if (nPar == 2) bloodAnchor = (motherSlot.Cx + fatherSlot.Cx) * 0.5f;
            else if (mother != null) bloodAnchor = motherSlot.Cx;
            else if (father != null) bloodAnchor = fatherSlot.Cx;

            float bloodStart = bloodAnchor - bloodW * 0.5f;
            var bloodSlots = PlaceRow(lay, blood, egoY, bloodStart, GapX);
            Slot focusSlot = bloodSlots[bloodSlots.Count - 1];
            var partnerSlots = new List<Slot>();
            if (partners.Count > 0)
            {
                partnerSlots = PlaceRow(lay, partners, egoY, focusSlot.x + NodeW + CoupleGap, GapX);
            }

            var childSlots = new List<Slot>();
            if (hasKids)
            {
                float coupleMid = focusSlot.Cx;
                if (partnerSlots.Count > 0)
                    coupleMid = (focusSlot.Cx + partnerSlots[partnerSlots.Count - 1].Cx) * 0.5f;
                float descStart = coupleMid - descW * 0.5f;
                float cursor = descStart;
                for (int i = 0; i < children.Count; i++)
                {
                    float clusterW = Mathf.Max(NodeW, RowW(gcByChild[i].Count));
                    float childX = cursor + (clusterW - NodeW) * 0.5f;
                    childSlots.Add(Place(lay, children[i], childX, kidY));
                    if (hasGc && gcByChild[i].Count > 0)
                    {
                        float gcStart = cursor + (clusterW - RowW(gcByChild[i].Count)) * 0.5f;
                        List<Slot> gcSlots = PlaceRow(lay, gcByChild[i], gcY, gcStart, GapX);
                        ConnectDownTo(lay, childSlots[i].Cx, childSlots[i].Bottom, gcSlots);
                    }
                    cursor += clusterW + GapX;
                }
            }

            if (hasGp)
            {
                if (momGps.Count > 0 && mother != null)
                    ConnectManyToOne(lay, momGps, motherSlot);
                if (dadGps.Count > 0 && father != null)
                    ConnectManyToOne(lay, dadGps, fatherSlot);
            }

            if (nPar == 2)
            {
                ConnectCouple(lay, motherSlot, fatherSlot, out float dropX, out float dropY);
                ConnectDownTo(lay, dropX, dropY, bloodSlots);
            }
            else if (mother != null)
            {
                ConnectDownTo(lay, motherSlot.Cx, motherSlot.Bottom, bloodSlots);
            }
            else if (father != null)
            {
                ConnectDownTo(lay, fatherSlot.Cx, fatherSlot.Bottom, bloodSlots);
            }

            if (hasKids)
            {
                float fromX = focusSlot.Cx;
                float fromY = focusSlot.Bottom;
                if (partnerSlots.Count > 0)
                {
                    ConnectCouple(lay, focusSlot, partnerSlots[0], out fromX, out fromY);
                    for (int i = 1; i < partnerSlots.Count; i++)
                        ConnectCouple(lay, partnerSlots[i - 1], partnerSlots[i], out _, out _);
                }
                ConnectDownTo(lay, fromX, fromY, childSlots);
            }

            if (hasTeach)
            {
                y += 12f;
                lay.teachHeaderY = y;
                y += FamilyTreeDrawer.HeaderH;
                PlaceCentered(lay, teach, y, mid);
            }

            Normalize(lay);
            return lay;
        }

        private static void ConnectManyToOne(Layout lay, List<Pawn> gps, Slot child)
        {
            var slots = new List<Slot>();
            for (int i = 0; i < gps.Count; i++)
            {
                Slot s = Find(lay, gps[i]);
                if (s.pawn != null) slots.Add(s);
            }
            if (slots.Count == 0) return;
            if (slots.Count == 2)
            {
                ConnectCouple(lay, slots[0], slots[1], out float dropX, out float dropY);
                ConnectDownTo(lay, dropX, dropY, new List<Slot> { child });
                return;
            }
            if (slots.Count == 1)
            {
                ConnectDownTo(lay, slots[0].Cx, slots[0].Bottom, new List<Slot> { child });
                return;
            }
            float barY = (slots[0].Bottom + child.Top) * 0.5f;
            float left = child.Cx;
            float right = child.Cx;
            for (int i = 0; i < slots.Count; i++)
            {
                left = Mathf.Min(left, slots[i].Cx);
                right = Mathf.Max(right, slots[i].Cx);
                lay.vs.Add(new VSeg { x = slots[i].Cx, y1 = slots[i].Bottom, y2 = barY });
            }
            lay.hs.Add(new HSeg { x1 = left, x2 = right, y = barY });
            lay.vs.Add(new VSeg { x = child.Cx, y1 = barY, y2 = child.Top });
        }

        private static void ConnectCouple(Layout lay, Slot a, Slot b, out float dropX, out float dropY)
        {
            Slot left = a.x <= b.x ? a : b;
            Slot right = a.x <= b.x ? b : a;
            lay.hs.Add(new HSeg { x1 = left.x + NodeW, x2 = right.x, y = left.Cy });
            dropX = (left.Cx + right.Cx) * 0.5f;
            dropY = left.Cy;
        }

        private static void ConnectDownTo(Layout lay, float fromX, float fromY, List<Slot> kids)
        {
            if (kids == null || kids.Count == 0) return;
            float barY = (fromY + kids[0].Top) * 0.5f;
            lay.vs.Add(new VSeg { x = fromX, y1 = fromY, y2 = barY });
            float left = fromX;
            float right = fromX;
            for (int i = 0; i < kids.Count; i++)
            {
                left = Mathf.Min(left, kids[i].Cx);
                right = Mathf.Max(right, kids[i].Cx);
                lay.vs.Add(new VSeg { x = kids[i].Cx, y1 = barY, y2 = kids[i].Top });
            }
            if (Mathf.Abs(right - left) >= 0.5f)
                lay.hs.Add(new HSeg { x1 = left, x2 = right, y = barY });
        }

        private static Slot Place(Layout lay, Pawn pawn, float x, float y)
        {
            var s = new Slot { pawn = pawn, x = x, y = y };
            lay.nodes.Add(s);
            return s;
        }

        private static List<Slot> PlaceRow(Layout lay, List<Pawn> pawns, float y, float startX, float gap)
        {
            var list = new List<Slot>();
            float x = startX;
            for (int i = 0; i < pawns.Count; i++)
            {
                list.Add(Place(lay, pawns[i], x, y));
                x += NodeW + gap;
            }
            return list;
        }

        private static void PlaceCentered(Layout lay, List<Pawn> pawns, float y, float cx)
        {
            if (pawns == null || pawns.Count == 0) return;
            float start = cx - RowW(pawns.Count) * 0.5f;
            PlaceRow(lay, pawns, y, start, GapX);
        }

        private static Slot Find(Layout lay, Pawn pawn)
        {
            for (int i = 0; i < lay.nodes.Count; i++)
            {
                if (lay.nodes[i].pawn == pawn) return lay.nodes[i];
            }
            return default;
        }

        private static void Normalize(Layout lay)
        {
            if (lay.nodes.Count == 0)
            {
                lay.width = 360f;
                lay.height = TitleH + 48f;
                return;
            }
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = 0f;
            for (int i = 0; i < lay.nodes.Count; i++)
            {
                minX = Mathf.Min(minX, lay.nodes[i].x);
                maxX = Mathf.Max(maxX, lay.nodes[i].x + NodeW);
                maxY = Mathf.Max(maxY, lay.nodes[i].y + NodeH);
            }
            float dx = 0f;
            if (minX < Pad) dx = Pad - minX;
            if (dx != 0f)
            {
                for (int i = 0; i < lay.nodes.Count; i++)
                {
                    Slot s = lay.nodes[i];
                    s.x += dx;
                    lay.nodes[i] = s;
                }
                for (int i = 0; i < lay.hs.Count; i++)
                {
                    HSeg s = lay.hs[i];
                    s.x1 += dx;
                    s.x2 += dx;
                    lay.hs[i] = s;
                }
                for (int i = 0; i < lay.vs.Count; i++)
                {
                    VSeg s = lay.vs[i];
                    s.x += dx;
                    lay.vs[i] = s;
                }
                maxX += dx;
            }
            lay.width = Mathf.Max(maxX + Pad, 360f);
            lay.height = maxY + Pad;
        }

        private static float RowW(int n)
        {
            if (n <= 0) return 0f;
            return n * NodeW + (n - 1) * GapX;
        }

        private static void SplitParents(List<Pawn> parents, out Pawn mother, out Pawn father)
        {
            mother = null;
            father = null;
            if (parents == null) return;
            for (int i = 0; i < parents.Count; i++)
            {
                Pawn p = parents[i];
                if (p == null) continue;
                if (p.gender == Gender.Female && mother == null) mother = p;
                else if (p.gender == Gender.Male && father == null) father = p;
            }
            for (int i = 0; i < parents.Count; i++)
            {
                Pawn p = parents[i];
                if (p == null || p == mother || p == father) continue;
                if (mother == null) mother = p;
                else if (father == null) father = p;
            }
        }

        private static void CopyCapped(List<Pawn> dest, List<Pawn> src, int max)
        {
            if (src == null) return;
            int n = Math.Min(src.Count, max);
            for (int i = 0; i < n; i++)
            {
                if (src[i] != null && !dest.Contains(src[i]))
                    dest.Add(src[i]);
            }
        }

        private static void AddLeftovers(List<Pawn> all, List<Pawn> a, List<Pawn> b)
        {
            if (all == null) return;
            for (int i = 0; i < all.Count; i++)
            {
                Pawn p = all[i];
                if (p == null || a.Contains(p) || b.Contains(p)) continue;
                if (a.Count <= b.Count) a.Add(p);
                else b.Add(p);
            }
        }

        private static void AddLeftoversTo(List<Pawn> dest, List<Pawn> all)
        {
            if (all == null) return;
            for (int i = 0; i < all.Count; i++)
            {
                Pawn p = all[i];
                if (p == null || dest.Contains(p)) continue;
                dest.Add(p);
            }
        }

        private static void SortByAge(List<Pawn> list)
        {
            if (list == null || list.Count < 2) return;
            list.Sort((a, b) =>
            {
                long aa = a?.ageTracker?.AgeBiologicalTicks ?? 0L;
                long bb = b?.ageTracker?.AgeBiologicalTicks ?? 0L;
                return bb.CompareTo(aa);
            });
        }
    }
}

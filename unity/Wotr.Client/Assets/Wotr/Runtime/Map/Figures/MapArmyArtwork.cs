using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-126: an army on the map is a token on the whole map, or up to three figures standing on a base when
    // zoomed in. The base or token picture follows the side, garrison, level and state classes of the button
    // (Styles/Armies.uss); this class places the figures, emblems and the count. UI-185/UI-192: about 50% larger than
    // UI-126. UI-400 (TABLETOP_FIGURES_UX): the figures are miniatures — they stand on a painted oval base (a crenellated
    // one for a garrison) with a contact shadow on the table, their feet on one line, two or three staggered in depth
    // with the farther one behind, each as tall as its painted stature; the state is a rim round the base, the nation
    // emblems and the count a plaque on its front, and only the base and the figures take a click (MiniatureButton).
    // UI-401 (TABLETOP_FIGURES_UX, "Scale"): the miniature is designed in board units (StandFootprint.Army) — its stand is
    // the 40 × 16 unit base and an elite stands 40 units tall, as tall as the base is wide — at DesignUnit points per
    // unit; the map stands its feet on the region's place and scales it (MapStand, MapStandScale). UI-406
    // (TABLETOP_FIGURES_UX, decision 3): far out the army is a token that lies on the table too — a painted enamel puck
    // (top face, side band, rim; a garrison's is walled with battlements) over the same contact shadow, its nation emblem
    // and count on it, and the state as a rim drawn round the puck instead of one whole picture per state.
    public sealed class MapArmyArtwork
    {
        public const string FiguresClass = "army-figures";
        public const string TokenClass = "army-token";
        public const string FieldClass = "field-force";
        public const string MovedClass = "moved";
        // UI-334: an army that appears or grows flares, emerges and then shows its number (Armies.uss).
        public const string FlaringClass = "army-flaring";
        public const string HiddenClass = "army-emerge-hidden";
        public const long FlareMilliseconds = 150;
        public const long EmergeMilliseconds = 500;
        // UI-338: a marker whose figures all left fades out; a shrinking army dims its figures for a moment (Armies.uss).
        public const string LeavingClass = "army-leaving";
        public const string ShrinkingClass = "army-shrinking";
        public const long LeaveMilliseconds = 700;
        public const long ShrinkMilliseconds = 250;
        // Unused parts are hidden by class, so the level rules of Styles/Armies.uss can still hide the rest.
        public const string UnusedClass = "army-part-unused";
        public const int MaximumEmblems = 2;
        // Design points per board unit: an elite stands 54 points tall at the design size, as since UI-192.
        public const float DesignUnit = 1.35f;
        // The army's base (40 units) and the room beside it for the state rim and the halo.
        private const float BaseWidth = 54;
        private const float Room = 6;
        private const float BoxWidth = BaseWidth + 2 * Room;
        private const float BoxHeight = 86;
        // The point under the feet, the place: the middle of the stand's top face.
        private static readonly Vector2 StandFeet = new Vector2(BoxWidth / 2, 64);
        // The painted stands at this width (112 × 44 and 112 × 48 pictures); the feet line lies on their top face.
        private const float FieldStand = 21;
        private const float GarrisonStand = 23;
        private const float FeetOnTopFace = .46f;
        // The nearer figures stand a little in front of the middle of the top face.
        private const float FrontFeet = 2;
        // An elite's height (40 units); the painted cells carry each figure's own stature (FigureStature).
        private const float FigureHeight = 54;
        // A figure may overhang the stand's rim this much; a wider cell is drawn smaller, its feet on the line.
        private const float Overhang = 4;
        // UI-406: the token's box; the puck (40 × 32 art drawn 44 wide) lies in it with room for its shadow and count. A
        // garrison's walled puck has the same size.
        private static readonly Vector2 TokenSize = new Vector2(44, 44);
        // UI-425: the puck's lower edge in its box (Styles/Armies.uss: 2 points from the top, 35.2 tall), and the middle of
        // its top face above the side band (the painted puck's face fills its upper 27.5 points).
        private const float PuckBottom = 37.2f;
        private const float TopFaceMiddle = 16;
        // The holder a marker stands in (MapStand): room for either level.
        public static readonly Vector2 HolderSize = new Vector2(BoxWidth, BoxHeight);
        // Two or three figures stand staggered: the first farther back (higher, a little smaller), the others in front.
        private static readonly Slot[][] Slots =
        {
            new[] { new Slot(0, 0, 1) },
            new[] { new Slot(-9, -4, .94f), new Slot(9, 0, 1) },
            new[] { new Slot(0, -6, .9f), new Slot(-13, 0, 1), new Slot(13, 0, 1) }
        };

        public VisualElement Root { get; } = Ignored("army-artwork");
        private readonly Image[] silhouettes = new Image[3];
        private readonly float[] statures = new float[3];
        private readonly VisualElement[] emblems = new VisualElement[MaximumEmblems];
        private readonly Label more = new Label("+") { name = "army-more-nations", pickingMode = PickingMode.Ignore };
        private readonly Label count = new Label { name = "army-count", pickingMode = PickingMode.Ignore };
        // UI-274: the crown over a force led by an Initial king.
        private readonly VisualElement crown = Ignored("army-crown");
        // UI-400: the nation emblems and the count on the stand's front; UI-401: it hangs below the stand's rim.
        private readonly VisualElement plaque = Ignored("army-plaque");
        private readonly Button button;
        // UI-322: the flare joins the marker only while it shows and leaves once it has faded, so a resting marker carries
        // nothing of it.
        private readonly VisualElement flare = Ignored("army-flare");
        private const long FlareFadeMilliseconds = 300;
        private IVisualElementScheduledItem dropFlare;
        private int shown;
        // UI-334: the number the army shows once it has emerged; null while nothing emerges.
        private string emergingCount;
        private IVisualElementScheduledItem reveal;
        private IVisualElementScheduledItem settle;
        private IVisualElementScheduledItem shrink;

        public MapArmyArtwork(Button button)
        {
            this.button = button;
            button.AddToClassList("illustrated-army");
            // UI-301: the ChoiceGlow halo of an army a click may choose, behind the artwork (Armies.uss).
            button.Add(Ignored("army-choice-glow"));
            // UI-400: the table under the miniature — its contact shadow, the painted stand and the state rim round it.
            // UI-406: the token's puck and its state rim lie over the same shadow.
            Root.Add(Ignored("army-contact-shadow"));
            Root.Add(Ignored("army-puck"));
            Root.Add(Ignored("army-token-rim"));
            Root.Add(Ignored("army-stand"));
            Root.Add(Ignored("army-rim"));
            for (var i = 0; i < silhouettes.Length; i++)
            {
                silhouettes[i] = FigureArtwork.Image(FigureAppearance.FreeRegular);
                silhouettes[i].AddToClassList("army-silhouette");
                Root.Add(silhouettes[i]);
            }
            Root.Add(plaque);
            var row = Ignored("army-emblems");
            for (var i = 0; i < emblems.Length; i++)
            {
                emblems[i] = Ignored("army-emblem-" + i);
                emblems[i].AddToClassList("army-emblem");
                row.Add(emblems[i]);
            }
            more.AddToClassList("army-more-nations");
            row.Add(more);
            Root.Add(row);
            count.AddToClassList("army-count");
            Root.Add(count);
            Root.Add(crown);
            Root.Add(Ignored("army-moved"));
            button.Add(Root);
            if (button is MiniatureButton miniature)
                miniature.Shape = Contains;
        }

        public static Vector2 Size(bool garrison, bool figures) => figures ? HolderSize : TokenSize;

        // UI-401: the design point a level stands on its place: the stand's top face, or the token's middle. UI-425: a field
        // force's token stands on the lower edge of its puck, as its figures stand on their feet, so it rises above the place
        // and the settlement's gem below the place stays in view; a garrison's walled token lies with the middle of its top
        // face on its gate, since the gate is on the fortress drawing and a raised puck would reach over the stone above some
        // of them (Moria, Minas Morgul).
        public static Vector2 Feet(bool garrison, bool figures) =>
            figures ? StandFeet : new Vector2(TokenSize.x / 2, garrison ? TopFaceMiddle : PuckBottom);

        // UI-401: what a level shows, in design points: the stand with its figures and plaque, or the whole token.
        public static Rect Visible(bool garrison, bool figures) => figures
            ? Rect.MinMaxRect(Room, StandFeet.y + FrontFeet - FigureHeight - 2, BoxWidth - Room, BoxHeight - 1)
            : new Rect(Vector2.zero, Size(garrison, false));

        public void Apply(ArmyMarker army)
        {
            var appearances = Appearances(army);
            shown = Math.Min(silhouettes.Length, appearances.Count);
            for (var i = 0; i < shown; i++)
            {
                silhouettes[i].sprite = FigureArtwork.Sprite(appearances[i]);
                statures[i] = FigureStature.Of(appearances[i]);
            }
            for (var i = 0; i < silhouettes.Length; i++)
                silhouettes[i].EnableInClassList(UnusedClass, i >= shown);
            PlaceSilhouettes();
            for (var i = 0; i < emblems.Length; i++)
                ShowEmblem(emblems[i], i < army.Nations.Count ? army.Nations[i].Nation : null);
            more.EnableInClassList(UnusedClass, army.Nations.Count <= emblems.Length);
            if (emergingCount != null)
                emergingCount = army.FigureCount.ToString();
            else
                count.text = army.FigureCount.ToString();
            crown.EnableInClassList(UnusedClass, army.CrownedTitles.Count == 0);
            button.EnableInClassList(FieldClass, !army.Garrison);
        }

        public bool Emerging => emergingCount != null;

        // UI-334 (owner 2026-10-25: "first a flare, then a gradual appearing, as if the occlusion changed until the picture
        // is fully clear; the new number shows and the flare disappears"): a new army (`before` null) flares, then emerges
        // from transparent and soft to clear; a growing one keeps its figures and its old number. Once clear, the number
        // steps and the flare fades.
        public void Emerge(int? before)
        {
            EndEmerge();
            emergingCount = count.text;
            count.text = before?.ToString() ?? count.text;
            dropFlare?.Pause();
            if (flare.parent == null)
                button.Insert(1, flare);
            button.AddToClassList(FlaringClass);
            button.EnableInClassList(HiddenClass, before == null);
            reveal = button.schedule.Execute(() => button.RemoveFromClassList(HiddenClass));
            reveal.ExecuteLater(FlareMilliseconds);
            settle = button.schedule.Execute(EndEmerge);
            settle.ExecuteLater(FlareMilliseconds + EmergeMilliseconds);
        }

        // UI-338 (owner 2026-10-25: "a unit that disappears from the map should fade out slowly and smoothly instead of
        // vanishing all at once"): the counterpart of Emerge — from clear to transparent and soft; `gone` removes it then.
        public void Leave(Action gone)
        {
            EndEmerge();
            button.RemoveFromClassList("eligible-army");
            button.RemoveFromClassList("armed-force");
            button.AddToClassList(LeavingClass);
            button.schedule.Execute(gone).ExecuteLater(LeaveMilliseconds);
        }

        // A shrinking army keeps its figures; they dim for a moment as its number steps down.
        public void Shrink()
        {
            shrink?.Pause();
            button.AddToClassList(ShrinkingClass);
            shrink = button.schedule.Execute(() => button.RemoveFromClassList(ShrinkingClass));
            shrink.ExecuteLater(ShrinkMilliseconds);
        }

        // Its final state: clear, its number, no flare (also when Skip ends it).
        public void EndEmerge()
        {
            reveal?.Pause();
            settle?.Pause();
            shrink?.Pause();
            button.RemoveFromClassList(ShrinkingClass);
            button.RemoveFromClassList(HiddenClass);
            button.RemoveFromClassList(FlaringClass);
            if (flare.parent != null)
            {
                dropFlare = button.schedule.Execute(() => flare.RemoveFromHierarchy());
                dropFlare.ExecuteLater(FlareFadeMilliseconds);
            }
            if (emergingCount == null)
                return;
            count.text = emergingCount;
            emergingCount = null;
        }

        public void Layout(bool figures)
        {
            button.EnableInClassList(FiguresClass, figures);
            button.EnableInClassList(TokenClass, !figures);
        }

        // UI-400: each figure's box is its painted cell's shape, so its feet are the box's bottom edge; they stand on the
        // base's top face, the farther figure higher and a little smaller, drawn behind the nearer ones. UI-401: the group
        // keeps within the stand and a little overhang, so it stays on the army's footprint.
        private void PlaceSilhouettes()
        {
            if (shown == 0)
                return;
            var slots = Slots[shown - 1];
            for (var i = 0; i < shown; i++)
            {
                var size = FigureSize(statures[i] * slots[i].Scale, Aspect(silhouettes[i].sprite));
                var x = Mathf.Clamp(slots[i].X, -Reach(size.x), Reach(size.x));
                silhouettes[i].style.width = size.x;
                silhouettes[i].style.height = size.y;
                silhouettes[i].style.left = StandFeet.x + x - size.x / 2;
                silhouettes[i].style.top = StandFeet.y + FrontFeet + slots[i].Feet - size.y;
            }
        }

        // A figure's box at its stature; a cell wider than the stand and its overhang is drawn smaller.
        private static Vector2 FigureSize(float stature, float aspect)
        {
            var height = FigureHeight * stature;
            var width = Mathf.Min(height * aspect, BaseWidth + 2 * Overhang);
            return new Vector2(width, width / aspect);
        }

        // How far from the middle a figure of this width may stand and keep within the stand and its overhang.
        private static float Reach(float width) => Mathf.Max(0, BaseWidth / 2 + Overhang - width / 2);

        private static float Aspect(Sprite sprite) =>
            sprite == null || sprite.rect.height <= 0 ? .8f : sprite.rect.width / sprite.rect.height;

        // UI-400: only the miniature takes a press — the stand's ellipse, the figures' boxes and the plaque; a token takes
        // its disc.
        private bool Contains(Vector2 local)
        {
            if (!button.ClassListContains(FiguresClass))
                return true;
            if (plaque.layout.Contains(local))
                return true;
            for (var i = 0; i < shown; i++)
                if (silhouettes[i].layout.Contains(local))
                    return true;
            var stand = button.ClassListContains("garrison") ? GarrisonStand : FieldStand;
            var centre = new Vector2(StandFeet.x, StandFeet.y - stand * FeetOnTopFace + stand / 2);
            var offset = new Vector2((local.x - centre.x) / (BaseWidth / 2), (local.y - centre.y) / (stand / 2));
            return offset.sqrMagnitude <= 1;
        }

        private readonly struct Slot
        {
            public Slot(float x, float feet, float scale)
            {
                X = x;
                Feet = feet;
                Scale = scale;
            }

            public float X { get; }
            public float Feet { get; }
            public float Scale { get; }
        }

        private static void ShowEmblem(VisualElement emblem, string nation)
        {
            emblem.EnableInClassList(UnusedClass, nation == null);
            var previous = emblem.userData as string;
            if (previous == nation)
                return;
            if (previous != null)
                emblem.RemoveFromClassList("nation-emblem-" + previous);
            if (nation != null)
                emblem.AddToClassList("nation-emblem-" + nation);
            emblem.userData = nation;
        }

        private static FigureArt Of(ArmyMarker army, UnitKind kind, Func<NationForce, int> count)
        {
            var nation = army.Nations.FirstOrDefault(n => count(n) > 0)?.Nation ?? "";
            return FigureAppearances.Unit(nation, kind, army.Side);
        }

        // The element's name doubles as its class, which Styles/Armies.uss selects.
        private static VisualElement Ignored(string name)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.AddToClassList(name);
            return element;
        }

        // ART-01: each figure is of the nation with the most figures of its kind (Nations come largest first). UI-402: a
        // ghost's carried characters stand first, each its own figure (one beside units, up to three alone); a board army
        // carries none.
        private static List<FigureArt> Appearances(ArmyMarker army)
        {
            var result = new List<FigureArt>(3);
            var characters = army.Total > 0 ? 1 : 3;
            foreach (var character in army.Carried.Take(characters))
                result.Add(character.Art);
            if (army.Elite > 0 && result.Count < 3)
                result.Add(Of(army, UnitKind.Elite, n => n.Elite));
            if (army.Regular > 0 && result.Count < 3)
                result.Add(Of(army, UnitKind.Regular, n => n.Regular));
            if (army.Leaders > 0 && result.Count < 3)
                result.Add(army.Nazgul > 0 ? Of(army, UnitKind.Nazgul, n => n.Nazgul)
                    : Of(army, UnitKind.Leader, n => n.Leaders));
            if (result.Count == 1 && army.Total > 1)
                result.Add(result[0]);
            return result;
        }
    }
}

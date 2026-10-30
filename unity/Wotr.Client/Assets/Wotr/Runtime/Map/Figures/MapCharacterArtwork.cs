using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-402 (TABLETOP_FIGURES_UX, decision 5): a character on the map is its own miniature — its painted figure at its
    // stature on a small round stand in its side's enamel, over a contact shadow, with the state rim round the stand
    // (Styles/Armies.uss). It is designed in board units at the army's design unit (StandFootprint.Character: a 14-unit
    // base, figures 36 units tall), so it stands at the scale of the armies beside it. When a region holds more
    // characters than its places, the last place shows a stacked pair — a second figure on its own stand behind the
    // first — and a code-drawn "+n" for the characters behind the front one. Far out the miniature is a token: the
    // character's portrait in its side's medallion. UI-406: that medallion is a thick coin on the table — a darker copy of
    // it below gives its edge, and the contact shadow lies under it. Only the stand, the figures' columns and the "+n"
    // take a press (MiniatureButton), so the headroom beside a slim figure passes the press on.
    public sealed class MapCharacterArtwork
    {
        public const string MiniatureClass = "character-miniature";
        public const string FiguresClass = "character-figures";
        public const string TokenClass = "character-token";
        public const string EligibleClass = "eligible-character";
        public const string SelectedClass = "selected-character";
        public const string StackedClass = "character-stacked";
        public const string UnusedClass = MapArmyArtwork.UnusedClass;
        public const float DesignUnit = MapArmyArtwork.DesignUnit;
        // The design box (66 × 86 for an army): room for the widest figure, a stand behind, the rim and the shadow.
        private const float BoxWidth = 56;
        private const float BoxHeight = 70;
        private const float TokenSize = 24;
        // UI-406: the medallion's darkened edge lies this far below its face (Styles/Armies.uss).
        private const float CoinEdge = 2.5f;
        // The point under the feet, the place: the middle of the stand's top face.
        private static readonly Vector2 StandFeet = new Vector2(BoxWidth / 2, 56);
        // The 14-unit base (40 × 28 points of art drawn 19 wide); its top face's middle is 0.357 of its height down.
        private const float BaseWidth = 19;
        private const float StandHeight = 13.3f;
        private const float FeetOnTopFace = .357f;
        // An elite's height at the footprint (36 units); the painted cells carry each figure's stature (FigureStature).
        private const float FigureHeight = 48.6f;
        // A figure is never wider than an army's stand; a wider cell is drawn smaller, its feet on the line.
        private const float MaximumWidth = 54;
        // The column that takes a press: the footprint's figure width (16 units).
        private const float ColumnWidth = 21.6f;
        // The stacked pair: the second figure stands this far behind and to the right, a little smaller.
        private static readonly Vector2 BehindOffset = new Vector2(7, -4);
        private const float BehindScale = .94f;
        private const float FrontShift = -3;
        // The holder a miniature stands in (MapStand): room for either level.
        public static readonly Vector2 HolderSize = new Vector2(BoxWidth, BoxHeight);

        public VisualElement Root { get; } = Ignored("character-artwork");
        private readonly Button button;
        private readonly VisualElement behindGroup = Ignored("character-behind");
        private readonly Image behind = Figure("character-behind-figure");
        private readonly VisualElement front = Ignored("character-front");
        private readonly Image figure = Figure("character-figure");
        private readonly Label more = new Label { name = "character-more", pickingMode = PickingMode.Ignore };
        private readonly Image portrait = new Image { name = "character-portrait", pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
        // UI-322, UI-334: the flare joins the miniature only while it shows.
        private readonly VisualElement flare = Ignored("character-flare");
        private const long FlareFadeMilliseconds = 300;
        private IVisualElementScheduledItem reveal;
        private IVisualElementScheduledItem settle;
        private IVisualElementScheduledItem dropFlare;
        private Rect frontBox;
        private Rect behindBox;
        private bool stacked;

        public MapCharacterArtwork(Button button)
        {
            this.button = button;
            button.AddToClassList(MiniatureClass);
            // The ChoiceGlow capsule of a character a click may choose, behind the artwork.
            button.Add(Ignored("character-choice-glow"));
            behindGroup.Add(Ignored("character-behind-stand"));
            behindGroup.Add(behind);
            Root.Add(behindGroup);
            front.Add(Ignored("character-contact-shadow"));
            front.Add(Ignored("character-stand"));
            front.Add(Ignored("character-rim"));
            front.Add(figure);
            Root.Add(front);
            more.AddToClassList("character-more");
            Root.Add(more);
            Root.Add(Ignored("character-token-shadow"));
            Root.Add(Ignored("character-token-edge"));
            portrait.AddToClassList("character-portrait");
            Root.Add(portrait);
            Root.Add(Ignored("character-moved"));
            button.Add(Root);
            if (button is MiniatureButton miniature)
                miniature.Shape = Contains;
        }

        public static Vector2 Size(bool figures) => figures ? HolderSize : new Vector2(TokenSize, TokenSize);

        // The design point a level stands on its place: the stand's top face, or (UI-425) the medallion's lower edge — the
        // coin's edge 2.5 points below its face (Styles/Armies.uss) — so it rises above the place like the figure.
        public static Vector2 Feet(bool figures) => figures ? StandFeet : new Vector2(TokenSize / 2, TokenSize + CoinEdge);

        // What a level shows, in design points: the stand with its figures (and the stacked one), or the whole token.
        public Rect Visible(bool figures)
        {
            if (!figures)
                return new Rect(Vector2.zero, Size(false));
            var stand = StandRect(FrontShift * (stacked ? 1 : 0));
            var shown = Union(stand, frontBox);
            return stacked ? Union(shown, behindBox) : shown;
        }

        // One character, or the front of a stacked pair with the one behind it and how many more stand there.
        public void Apply(CharacterFigure character, CharacterFigure second, int hidden)
        {
            stacked = second != null;
            button.EnableInClassList("fp", character.Side == Side.FreePeoples);
            button.EnableInClassList("sh", character.Side == Side.Shadow);
            button.EnableInClassList(StackedClass, stacked);
            figure.sprite = FigureArtwork.Sprite(character.Art);
            portrait.sprite = PortraitArtwork.Sprite(character.DefinitionId) ?? figure.sprite;
            var shift = new Vector2(stacked ? FrontShift : 0, 0);
            front.style.translate = new Translate(shift.x, 0);
            frontBox = PlaceFigure(figure, FigureStature.Of(character.Art), shift);
            behindGroup.EnableInClassList(UnusedClass, !stacked);
            if (stacked)
                ShowBehind(second);
            var extra = stacked ? hidden + 1 : 0;
            more.text = "+" + extra;
            more.EnableInClassList(UnusedClass, extra == 0);
        }

        public void Layout(bool figures)
        {
            button.EnableInClassList(FiguresClass, figures);
            button.EnableInClassList(TokenClass, !figures);
        }

        // UI-334 for a character: it flares, then emerges from transparent to clear, and the flare fades.
        public void Emerge()
        {
            EndEmerge();
            dropFlare?.Pause();
            if (flare.parent == null)
                button.Insert(1, flare);
            button.AddToClassList(MapArmyArtwork.FlaringClass);
            button.AddToClassList(MapArmyArtwork.HiddenClass);
            reveal = button.schedule.Execute(() => button.RemoveFromClassList(MapArmyArtwork.HiddenClass));
            reveal.ExecuteLater(MapArmyArtwork.FlareMilliseconds);
            settle = button.schedule.Execute(EndEmerge);
            settle.ExecuteLater(MapArmyArtwork.FlareMilliseconds + MapArmyArtwork.EmergeMilliseconds);
        }

        // UI-338 for a character: it fades out where it stood; `gone` removes it then.
        public void Leave(Action gone)
        {
            EndEmerge();
            button.RemoveFromClassList(EligibleClass);
            button.RemoveFromClassList(SelectedClass);
            button.AddToClassList(MapArmyArtwork.LeavingClass);
            button.schedule.Execute(gone).ExecuteLater(MapArmyArtwork.LeaveMilliseconds);
        }

        // Its final state: clear, no flare (also when Skip ends it).
        public void EndEmerge()
        {
            reveal?.Pause();
            settle?.Pause();
            button.RemoveFromClassList(MapArmyArtwork.HiddenClass);
            button.RemoveFromClassList(MapArmyArtwork.FlaringClass);
            if (flare.parent == null)
                return;
            dropFlare = button.schedule.Execute(() => flare.RemoveFromHierarchy());
            dropFlare.ExecuteLater(FlareFadeMilliseconds);
        }

        private void ShowBehind(CharacterFigure second)
        {
            behind.sprite = FigureArtwork.Sprite(second.Art);
            behindGroup.style.translate = new Translate(BehindOffset.x, BehindOffset.y);
            behindBox = PlaceFigure(behind, FigureStature.Of(second.Art) * BehindScale, BehindOffset);
        }

        // A figure's box at its stature on its cell's shape, its feet on its stand's top face, in its group (which the
        // shift moves); the box it takes in the miniature's design points.
        private static Rect PlaceFigure(Image image, float stature, Vector2 shift)
        {
            var size = FigureSize(stature, Aspect(image.sprite));
            image.style.width = size.x;
            image.style.height = size.y;
            image.style.left = StandFeet.x - size.x / 2;
            image.style.top = StandFeet.y - size.y;
            return new Rect(StandFeet.x + shift.x - size.x / 2, StandFeet.y + shift.y - size.y, size.x, size.y);
        }

        private static Vector2 FigureSize(float stature, float aspect)
        {
            var height = FigureHeight * stature;
            var width = Mathf.Min(height * aspect, MaximumWidth);
            return new Vector2(width, width / aspect);
        }

        private static float Aspect(Sprite sprite) =>
            sprite == null || sprite.rect.height <= 0 ? .5f : sprite.rect.width / sprite.rect.height;

        // The stand's box, shifted with its figure.
        private static Rect StandRect(float shift) =>
            new Rect(StandFeet.x - BaseWidth / 2 + shift, StandFeet.y - StandHeight * FeetOnTopFace, BaseWidth, StandHeight);

        // UI-400's hit shape for a character: the stand's ellipse, each figure's column from its head to its feet, the
        // "+n"; a token takes its disc.
        private bool Contains(Vector2 local)
        {
            if (!button.ClassListContains(FiguresClass))
                return (local - Size(false) / 2).sqrMagnitude <= TokenSize * TokenSize / 4;
            if (stacked && (more.layout.Contains(local) || InColumn(behindBox, local)))
                return true;
            if (InColumn(frontBox, local))
                return true;
            var stand = StandRect(stacked ? FrontShift : 0);
            var offset = new Vector2((local.x - stand.center.x) / (stand.width / 2), (local.y - stand.center.y) / (stand.height / 2));
            return offset.sqrMagnitude <= 1;
        }

        private static bool InColumn(Rect box, Vector2 local) =>
            Mathf.Abs(local.x - box.center.x) <= ColumnWidth / 2 && local.y >= box.yMin && local.y <= box.yMax;

        private static Rect Union(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        private static Image Figure(string name)
        {
            var image = new Image { name = name, pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            image.AddToClassList("figure-art-image");
            image.AddToClassList(name);
            return image;
        }

        // The element's name doubles as its class, which Styles/Armies.uss selects.
        private static VisualElement Ignored(string name)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.AddToClassList(name);
            return element;
        }
    }
}

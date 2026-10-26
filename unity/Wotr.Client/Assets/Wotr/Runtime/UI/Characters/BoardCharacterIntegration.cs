using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Wires existing inspection surfaces; never selects a die or submits a command.
    internal sealed class BoardCharacterIntegration
    {
        public BoardCharacterIntegration(BoardScreen board, VisualElement stage)
        {
            var characters = board.Characters;
            characters.Actions.OpenReaction = board.Notice.Open;
            stage.Add(characters.Tab);
            stage.Add(characters.Root);
            stage.Add(characters.Inspector.Root);
            characters.AddSovereigns(board.Sovereigns.Root);
            board.Sovereigns.FallbackAnchor = () => characters.Tab;
            board.Sovereigns.Applied += () => characters.ShowSovereigns(board.Sovereigns.HasShownPortraits);
            board.Sovereigns.Highlighted += any => characters.Tab.EnableInClassList("legal-target", any);
            characters.InspectorWindow = new FloatingWindow(characters.Inspector.Root, board.Windows,
                () => characters.InspectorPinned, characters.PinInspection, characters.PositionInspector, rememberInitial: false);
            characters.IsRegionSource = element => element != null && board.Hover.Panel.Root.Contains(element);
            board.Hover.Panel.CharacterInspected += id => characters.Show(id, true,
                board.Hover.Panel.Root.Q<Button>("hover-figure-" + id));
            board.Hover.RelatedInspection = element => characters.InspectingRegion && element != null &&
                (element == characters.Inspector.Root || characters.Inspector.Root.Contains(element));
            board.Sovereigns.InspectCharacter = (id, pin, source) =>
            {
                characters.Show(id, pin, source);
                return characters.Inspector.Root.style.display.value != DisplayStyle.None;
            };
            characters.Opening += board.Hover.Close;
            characters.Opening += board.Nations.CloseTransient;
            characters.Opening += board.Sovereigns.CloseTransient;
            characters.Opening += board.Cards.CloseTransient;
            characters.Opening += board.Hunt.CloseTransient;
            characters.Opening += board.Fellowship.CloseTransient;
            characters.Opening += board.HandDrawer.CloseTransient;
            characters.Opening += board.TableEffects.CloseTransient;
            characters.Inspecting += board.Cards.CloseTransient;
            characters.Inspecting += board.Log.CloseTransient;
            board.Nations.Opening += characters.CloseTransient;
            board.Cards.Opening += characters.CloseTransient;
            board.Hunt.Opening += characters.CloseTransient;
            board.Fellowship.Opening += characters.CloseTransient;
            board.HandDrawer.Opening += characters.CloseTransient;
            board.TableEffects.Opening += characters.CloseTransient;
            board.Victory.Opening += characters.CloseTransient;
            board.Battles.Opening += characters.CloseTransient;
            board.Sovereigns.Opening += characters.CloseTransient;
            board.Hover.Opening += () => { if (!characters.InspectingRegion) characters.CloseTransient(); };
            board.Root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape && !FloatingWindow.IsDragging(e.target as VisualElement))
                {
                    var handled = characters.Root.style.display.value != DisplayStyle.None ||
                        characters.Inspector.Root.style.display.value != DisplayStyle.None;
                    characters.Close();
                    if (handled)
                        e.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
            AddOcclusions(board, characters);
        }

        private static void AddOcclusions(BoardScreen board, CharacterRosterPanel characters)
        {
            var overlays = new[] { characters.Tab, characters.Root, characters.Inspector.Root };
            var previous = board.Map.Occlusions;
            board.Map.Occlusions = () => previous().Concat(overlays.Where(Visible)
                .Select(e => new Rect(board.Map.WorldToLocal(e.worldBound.position), e.worldBound.size)));
            var sovereign = board.Sovereigns.Occlusions;
            characters.Occlusions = () => sovereign().Concat(overlays.Where(e => e != characters.Inspector.Root && Visible(e)).Select(e => e.worldBound));
            board.Sovereigns.Occlusions = () => sovereign().Concat(overlays.Where(Visible).Select(e => e.worldBound));
            var fixedControls = board.Sovereigns.FixedOcclusions;
            board.Sovereigns.FixedOcclusions = () => fixedControls().Concat(new[] { characters.Tab.worldBound });
            characters.FixedOcclusions = board.Sovereigns.FixedOcclusions;
            foreach (var overlay in overlays)
                overlay.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    board.Map.Refresh();
                    board.Sovereigns.RequestPosition();
                });
        }

        private static bool Visible(VisualElement element) => !Appearance.Closed(element) && element.resolvedStyle.display != DisplayStyle.None &&
            element.worldBound.width > 0;
    }
}

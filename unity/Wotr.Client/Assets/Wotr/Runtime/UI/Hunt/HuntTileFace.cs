using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-141: the artwork of a printed Hunt tile (graphicalassets/ui/src/hunt): the tile of its pool, its value as a
    // number or a symbol (Eye, die, Sméagol) and the Reveal, Stop and Kings marks. Size comes from the context class.
    public sealed class HuntTileFace
    {
        public const string ClassName = "hunt-face";
        public VisualElement Root { get; } = new VisualElement { pickingMode = PickingMode.Ignore };
        private readonly Label number = new Label { pickingMode = PickingMode.Ignore, enableRichText = false };
        private readonly VisualElement symbol = new VisualElement { pickingMode = PickingMode.Ignore };
        private readonly VisualElement reveal = Mark("hunt-face-reveal");
        private readonly VisualElement stop = Mark("hunt-face-stop");
        private readonly VisualElement kings = Mark("hunt-face-kings");
        private string pool = "";
        private string shape = "";

        public HuntTileFace(string size)
        {
            Root.AddToClassList(ClassName);
            Root.AddToClassList(ClassName + "-" + size);
            number.AddToClassList("hunt-face-number");
            symbol.AddToClassList("hunt-face-symbol");
            Root.Add(number);
            Root.Add(symbol);
            Root.Add(reveal);
            Root.Add(stop);
            Root.Add(kings);
        }

        public void Apply(HuntTileFaceView face)
        {
            Swap("hunt-face-pool-", ref pool, face.Pool);
            var symbolic = face.Pool == HuntTileText.Smeagol ? HuntTileText.Smeagol : face.Value is "eye" or "die" ? face.Value : "";
            Swap("hunt-face-symbol-", ref shape, symbolic);
            symbol.style.display = symbolic.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            number.style.display = symbolic.Length > 0 ? DisplayStyle.None : DisplayStyle.Flex;
            number.text = symbolic.Length > 0 ? "" : HuntTileText.Title(face);
            reveal.style.display = face.Reveal ? DisplayStyle.Flex : DisplayStyle.None;
            stop.style.display = face.Stop ? DisplayStyle.Flex : DisplayStyle.None;
            kings.style.display = face.Source == "kings" ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Swap(string prefix, ref string current, string next)
        {
            Root.RemoveFromClassList(prefix + current);
            Root.AddToClassList(prefix + next);
            current = next;
        }

        private static VisualElement Mark(string name)
        {
            var mark = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            mark.AddToClassList("hunt-face-mark");
            mark.AddToClassList(name);
            return mark;
        }
    }
}

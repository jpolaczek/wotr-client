namespace Wotr.Client
{
    // UI-325 (owner review 2026-10-25: "the dice are to be visible, not only their pips"): a Hunt die is the ivory d6 of
    // the battle window's art kit (UI-117), its face picked by the die-pips-N class (HuntWindow.uss); nothing is painted.
    public sealed class HuntDieFace : DiePips
    {
        public const string FaceClass = "hunt-die-face";

        public HuntDieFace()
        {
            name = "hunt-pips";
            Illustrated = true;
            AddToClassList(FaceClass);
        }
    }
}

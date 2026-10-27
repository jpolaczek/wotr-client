namespace Wotr.Client
{
    // UI-144: what one nation's row offers for the chosen die, read from the server's options.
    internal sealed class NationRowActions
    {
        public bool CanAdvance;
        // The nation of a restored draft or of the token being dragged.
        public bool Highlighted;
        public bool CardTarget;
        // UI-278: what a card target's click does, or "" for the general hint.
        public string CardHint = "";
        public bool ActivationOffered;
        public bool CanActivate;
        public string ActivationHint = "";
        // UI-366: a character's political ability moves this nation: its token, arrow and station say whose and how.
        public string AdvanceHint = "";
    }
}

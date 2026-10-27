using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-278 (EXP-15 D15): a Kings card branch chosen on an object carries that object into the card's next step — a
    // nation token's click on Wisdom's politics chooses that nation, a king's click on Wisdom's inspection that king, a
    // Reserve figure's click on a recruit plans that figure. The carry waits while the branch's decision is open and ends
    // with the next decision, used or not.
    public sealed class BranchCarry
    {
        // The recruit's next step: one card's figures (ReserveFigures) or a plan (RecruitmentPlan).
        public const string Reserve = "Reserve";
        private string decision = "";
        private string step = "";
        private string target = "";

        public void Set(PlayerView view, string nextStep, string objectId)
        {
            decision = view.Decision?.Id ?? "";
            step = nextStep;
            target = objectId;
        }

        // Drops a carry the next decision does not await; called once per view before the objects take theirs.
        public void Expire(PlayerView view)
        {
            if (step.Length > 0 && Moved(view) && !Awaits(view))
                Clear();
        }

        // The carried object when this view is the awaited step, or ""; taking it ends the carry.
        public string Take(PlayerView view, string awaited)
        {
            if (step != awaited || !Moved(view) || !Awaits(view) || view.DecisionOwner != view.You)
                return "";
            var carried = target;
            Clear();
            return carried;
        }

        private bool Moved(PlayerView view) => view?.Decision?.Id != decision;

        private bool Awaits(PlayerView view)
        {
            var presentation = view?.Decision?.Presentation;
            return step == Reserve ? presentation is "ReserveFigures" or "RecruitmentPlan" : presentation == step;
        }

        private void Clear()
        {
            decision = "";
            step = "";
            target = "";
        }
    }
}

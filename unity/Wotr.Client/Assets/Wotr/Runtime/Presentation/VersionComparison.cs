using System;

namespace Wotr.Client
{
    // UI-269: which companion's two versions the Fellowship panel compares. A click on a portrait chooses it and the
    // version cards the tray shows follow (the latest rendering listens); the cards' own default is reported back so
    // the portrait shows it.
    public sealed class VersionComparison
    {
        public string Figure { get; private set; } = "";
        public Action<string> Show { get; set; }
        public event Action<string> Changed;

        public void Select(string figure)
        {
            Figure = figure;
            Show?.Invoke(figure);
            Changed?.Invoke(figure);
        }

        // The cards compare this companion on their own (their default); the portraits follow.
        public void Showing(string figure)
        {
            if (Figure == figure)
                return;
            Figure = figure;
            Changed?.Invoke(figure);
        }

        public void Reset() => Figure = "";
    }
}

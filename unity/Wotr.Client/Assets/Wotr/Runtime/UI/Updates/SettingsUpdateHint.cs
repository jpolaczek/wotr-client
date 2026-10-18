using UnityEngine.UIElements;
using Wotr.Client.Updates;

namespace Wotr.Client
{
    // DIST-17: an update shown on a first menu's Settings button: a gentle glow (USS class update-glow) and a small note
    // that takes no layout space and no pointer, so no control moves and nothing new can be clicked. The button's
    // tooltip carries the full sentence; Settings itself offers the download.
    public sealed class SettingsUpdateHint
    {
        public const string GlowClass = "update-glow";
        private readonly Button settings;
        private readonly string usualTooltip;
        private readonly Label note = new Label { name = "settings-update-note", pickingMode = PickingMode.Ignore };

        public SettingsUpdateHint(Button settings)
        {
            this.settings = settings;
            usualTooltip = settings.tooltip;
            note.AddToClassList("settings-update-note");
            settings.Add(note);
            Show(UpdateNote.None);
        }

        public Label Note => note;

        public void Show(UpdateNote update)
        {
            settings.EnableInClassList(GlowClass, update.Shown);
            settings.tooltip = update.Shown ? update.Detail : usualTooltip;
            note.text = update.Text;
            note.style.display = update.Shown ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

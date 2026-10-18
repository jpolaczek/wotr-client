using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // AUTH-06: the main menu's window for a new account name, confirmed with the current password. The name is checked
    // here first (AccountNameRules); the server's answer stays in the window, so a refusal never leaves the menu. Esc
    // and Cancel close it without a change; Enter in either field sends.
    public sealed class AccountNameDialog
    {
        public VisualElement Root { get; } = new VisualElement { name = "account-name-dialog" };
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        public Label Status { get; } = new Label { name = "account-name-status" };
        public event Action<string, string> Submitted;
        private readonly Label current = new Label { name = "account-name-current" };
        private readonly TextField name = new TextField("New account name") { name = "account-name-new", maxLength = 32 };
        private readonly TextField password = new TextField("Current password") { name = "account-name-password", isPasswordField = true, maxLength = 128 };
        private readonly Button submit;
        private readonly Button cancel;
        private string currentName = "";
        private bool busy;

        public AccountNameDialog()
        {
            Root.AddToClassList("settings-scrim");
            Root.AddToClassList("confirmation");
            Root.style.display = DisplayStyle.None;
            var window = new VisualElement { name = "account-name-window" };
            window.AddToClassList("confirm-window");
            window.AddToClassList("account-name-window");
            window.Add(BoardScreen.Title("Change account name"));
            current.AddToClassList("confirm-note");
            window.Add(current);
            name.tooltip = AccountNameRules.NameRule;
            window.Add(name);
            window.Add(password);
            Status.AddToClassList("status-message");
            window.Add(Status);
            submit = BoardScreen.Button("Change name", Submit);
            submit.name = "account-name-submit";
            cancel = BoardScreen.Button("Cancel", Close);
            cancel.name = "account-name-cancel";
            var actions = BoardScreen.Row("confirm-actions");
            actions.Add(submit);
            actions.Add(cancel);
            window.Add(actions);
            Root.Add(window);
            foreach (var field in new[] { name, password })
                field.RegisterCallback<KeyDownEvent>(SubmitOnEnter);
            Root.RegisterCallback<KeyDownEvent>(CloseOnEscape, TrickleDown.TrickleDown);
        }

        public void Open(string accountName)
        {
            currentName = accountName ?? "";
            current.text = "You sign in as " + currentName + ". The new name is your sign-in name from now on.";
            name.value = "";
            password.value = "";
            Status.text = "";
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            name.Focus();
        }

        public void Close()
        {
            Root.style.display = DisplayStyle.None;
            password.value = "";
        }

        public void Submit()
        {
            if (busy)
                return;
            var problem = AccountNameRules.Problem(currentName, name.value, password.value);
            if (problem.Length > 0)
            {
                Status.text = problem;
                return;
            }
            Status.text = "";
            Submitted?.Invoke(name.value.Trim(), password.value);
        }

        public void SetValues(string newName, string currentPassword)
        {
            name.value = newName;
            password.value = currentPassword;
        }

        public void Busy(bool value)
        {
            busy = value;
            submit.SetEnabled(!value);
            name.SetEnabled(!value);
            password.SetEnabled(!value);
        }

        public void Refused(string error)
        {
            Status.text = AccountNameRules.Refusal(error);
            password.value = "";
            password.Focus();
        }

        private void SubmitOnEnter(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;
            Submit();
            evt.StopPropagation();
        }

        private void CloseOnEscape(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape || !IsOpen)
                return;
            Close();
            evt.StopImmediatePropagation();
        }
    }
}

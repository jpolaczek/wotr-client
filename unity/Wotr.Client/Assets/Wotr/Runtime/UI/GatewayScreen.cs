using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // AUTH-03 (D16): the game only signs in, by name or email. Accounts are made and passwords reset on the game's
    // site, which the two account buttons open in the browser.
    public sealed class GatewayScreen
    {
        public const string RegisterPage = "register";
        public const string ForgotPage = "forgot";

        public VisualElement Root { get; } = new VisualElement { name = "gateway-screen" };
        public Label Status { get; } = new Label { name = "auth-status" };
        public event Action<string, string> Submitted;
        public event Action InterfaceRequested;
        // REPLAY-04: the library opens before sign-in, without an account or a server.
        public event Action ReplaysRequested;
        private readonly TextField login = new TextField("Username or email") { name = "auth-username" };
        private readonly TextField password = new TextField("Password") { name = "auth-password", isPasswordField = true };
        private readonly Button submit;
        private readonly Button create;
        private readonly Button forgot;
        private readonly Func<string> server;
        private readonly Action<string> openPage;
        private bool busy;

        // The server whose site holds the account pages, and how a page opens (the browser unless a test replaces it).
        public GatewayScreen(Func<string> server = null, Action<string> openPage = null)
        {
            this.server = server ?? (() => ServerConfiguration.Address);
            this.openPage = openPage ?? UnityEngine.Application.OpenURL;
            FrontendTheme.Apply(Root);
            Root.AddToClassList("gateway");
            var card = new VisualElement();
            card.AddToClassList("login-card");
            Root.Add(card);
            var heading = BoardScreen.Row();
            heading.Add(BoardScreen.Title("WAR OF THE RING"));
            heading.Add(InterfaceSettingsPanel.OpenButton(() => InterfaceRequested?.Invoke()));
            card.Add(heading);
            card.Add(new Label("A table for two. Return whenever you are ready."));
            card.Add(login);
            card.Add(password);
            login.tooltip = "Your username or the email address of your account";
            password.tooltip = "12–128 characters. Spaces are part of your password.";
            submit = BoardScreen.Button("Login", Submit);
            submit.name = "auth-submit";
            create = BoardScreen.Button("Create account", () => OpenAccountPage(RegisterPage));
            create.name = "auth-create";
            create.tooltip = "Opens the game's site in your browser";
            forgot = BoardScreen.Button("Forgot password?", () => OpenAccountPage(ForgotPage));
            forgot.name = "auth-forgot";
            forgot.tooltip = "Opens the game's site in your browser";
            var actions = BoardScreen.Row();
            actions.Add(submit);
            actions.Add(create);
            actions.Add(forgot);
            card.Add(actions);
            var hint = new Label("Create your account in the browser, then come back and log in.") { name = "auth-browser-hint" };
            hint.AddToClassList("auth-hint");
            card.Add(hint);
            var replays = BoardScreen.Button("Replays", () => ReplaysRequested?.Invoke());
            replays.name = "auth-replays";
            replays.tooltip = "Watch saved games again; no account or server needed.";
            replays.AddToClassList("auth-replays");
            card.Add(replays);
            Status.AddToClassList("status-message");
            card.Add(Status);
            SubmitOnEnter(password);
        }

        // The account page on the server's site, e.g. https://wotr.example.test/account/register.
        public static string AccountPage(string server, string page) => server.Trim().TrimEnd('/') + "/account/" + page;

        public void OpenAccountPage(string page) => openPage(AccountPage(server(), page));

        // Enter in the password signs in.
        private void SubmitOnEnter(TextField field)
        {
            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != UnityEngine.KeyCode.Return && evt.keyCode != UnityEngine.KeyCode.KeypadEnter)
                    return;
                Submit();
                evt.StopPropagation();
            });
        }

        public void Submit()
        {
            if (busy)
                return;
            var name = login.value.Trim();
            if (!AccountInput.ValidUsername(name) && !AccountInput.ValidEmail(name))
                Error(login, "Username or email: enter your username or your account's email address.");
            else if (!AccountInput.ValidPassword(password.value))
                Error(password, "Password: use 12–128 characters.");
            else
            {
                Status.text = "";
                Submitted?.Invoke(name, password.value);
            }
        }

        public void Busy(bool value)
        {
            busy = value;
            submit.SetEnabled(!value);
            login.SetEnabled(!value);
            password.SetEnabled(!value);
            if (!value && Status.text.Length > 0)
                password.Focus();
        }

        public void ClearPassword() => password.value = "";

        public void ShowAuthenticationFailure() =>
            Status.text = "Username, email or password not accepted. Try again, or reset your password in the browser.";

        private void Error(TextField field, string message)
        {
            Status.text = message;
            field.Focus();
        }
    }
}

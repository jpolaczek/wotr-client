using System;

namespace Wotr.Client
{
    public enum DiagnosticCategory
    {
        Transport,
        Authentication,
        Compatibility,
        Presentation
    }

    // QUAL-09: one line per failure — category, match, revision and the error's type, server code and place — so a
    // drawing error is not mistaken for a network loss. It never carries tokens, hands, hidden data or the view.
    public sealed class ClientDiagnostics
    {
        public static readonly ClientDiagnostics Unity = new ClientDiagnostics(message => UnityEngine.Debug.LogWarning(message));

        private readonly Action<string> sink;

        public ClientDiagnostics(Action<string> sink)
        {
            this.sink = sink;
        }

        public void Report(DiagnosticCategory category, string match, long revision, Exception error) =>
            sink(Line(category, match, revision, error));

        public static string Line(DiagnosticCategory category, string match, long revision, Exception error)
        {
            var code = error is ApiException api ? " status=" + api.Status + " code=" + api.Message : "";
            var place = error.TargetSite == null ? "" : " at " + error.TargetSite.DeclaringType?.Name + "." + error.TargetSite.Name;
            return "[wotr] " + category.ToString().ToLowerInvariant() + " match=" + match + " revision=" + revision +
                " error=" + error.GetType().Name + code + place;
        }
    }
}

namespace Wotr.Client
{
    // Only presentation timing. No selected figures, costs or commands live here.
    public sealed class HoverState
    {
        public const double OpenDelay = .2;
        public const double CloseDelay = .15;
        public string Visible { get; private set; } = "";
        private string candidate = "";
        private double openAt = double.PositiveInfinity;
        private double closeAt = double.PositiveInfinity;
        private bool inPanel;
        private bool focused;

        public void Observe(string id, double now)
        {
            id ??= "";
            if (candidate == id)
                return;
            candidate = id;
            openAt = id.Length == 0 ? double.PositiveInfinity : now + OpenDelay;
            closeAt = id == Visible || inPanel || focused ? double.PositiveInfinity : now + CloseDelay;
        }

        public void Panel(bool inside, double now)
        {
            inPanel = inside;
            if (inside)
            {
                candidate = Visible;
                openAt = double.PositiveInfinity;
            }
            else
                candidate = "";
            closeAt = inside || focused ? double.PositiveInfinity : now + CloseDelay;
        }

        public void Focus(string id)
        {
            focused = true;
            Visible = candidate = id;
            openAt = closeAt = double.PositiveInfinity;
        }

        public void Open(string id)
        {
            Focus(id);
            focused = false;
        }

        public void Blur(double now, bool keepHovered = false)
        {
            focused = false;
            if (!inPanel && (!keepHovered || candidate != Visible))
                closeAt = now + CloseDelay;
        }

        public bool Tick(double now)
        {
            var previous = Visible;
            if (now >= closeAt)
                Visible = "";
            if (now >= openAt)
            {
                Visible = candidate;
                openAt = closeAt = double.PositiveInfinity;
            }
            return previous != Visible;
        }

        public void Clear()
        {
            Visible = candidate = "";
            inPanel = focused = false;
            openAt = closeAt = double.PositiveInfinity;
        }
    }
}

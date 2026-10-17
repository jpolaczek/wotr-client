using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-136: a soft green halo (art: frames/glow) around each control the chosen action die can use. The halo is a
    // child drawn just outside its host and never takes a pointer; a die change only switches the host's class.
    // UI-193: a host without a plate of its own (a left rail medallion) lights its own art instead of a halo.
    public sealed class DieOptionGlow
    {
        public const string LitClass = "die-option";
        public const string HaloClass = "die-option-glow";
        private readonly List<(VisualElement Host, Func<DieOptions, bool> Lit)> hosts =
            new List<(VisualElement, Func<DieOptions, bool>)>();

        public void Attach(VisualElement host, Func<DieOptions, bool> lit)
        {
            var halo = new VisualElement { name = "die-option-glow", pickingMode = PickingMode.Ignore };
            halo.AddToClassList(HaloClass);
            host.Insert(0, halo);
            Light(host, lit);
        }

        // Only the host's class changes; its own styles draw the light.
        public void Light(VisualElement host, Func<DieOptions, bool> lit) => hosts.Add((host, lit));

        public void Show(DieOptions options)
        {
            foreach (var (host, lit) in hosts)
                host.EnableInClassList(LitClass, lit(options));
        }
    }
}

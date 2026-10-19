using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-263: one control of the waiting room's rules strip: an expansion's medallion or a variant's chip.
    public sealed class RuleChoice
    {
        public string Id = "";
        public string Name = "";
        public bool On;
        public bool Enabled;
        public string Tooltip = "";
    }

    // UI-263 (owner decision 2026-10-19): the rules of a table are chosen only in its waiting room. The strip shows the
    // three expansions in release order (Fate, Lords, Kings) and the six variants, always in the same places. Only the
    // host of a waiting room changes them, and every change makes both players get ready again (the server resets
    // Ready). A module the server does not offer yet and a variant whose expansion is missing stay in place, disabled,
    // with the reason in the tooltip; every enabled control's tooltip says what its click does.
    public static class RulesChoices
    {
        public const string NewCities = "new-cities";
        public const string Council = "council";
        public const string Dismay = "dismay";
        public const string VisibleCorruption = "visible-corruption";
        public const string SequentialCorruption = "sequential-corruption";
        public const string RemovingRulerTokens = "removing-ruler-tokens";
        public static readonly IReadOnlyList<string> ModuleIds = ExpansionModules.All;
        // Grouped by the expansion they need: Fate, Lords, Kings, then Lords and Kings together.
        public static readonly IReadOnlyList<string> VariantIds = new[]
        {
            NewCities, Council, VisibleCorruption, SequentialCorruption, Dismay, RemovingRulerTokens
        };
        private const string AgainReady = " Both players get ready again.";

        public static string ModuleName(string module) => module switch
        {
            ExpansionModules.Fate => "Fate of Erebor",
            ExpansionModules.Lords => "Lords of Middle-earth",
            _ => "Kings of Middle-earth"
        };

        public static string VariantName(string variant) => variant switch
        {
            NewCities => "New Cities",
            Council => "Council of Rivendell",
            Dismay => "Dismay",
            VisibleCorruption => "Visible Corruption",
            SequentialCorruption => "Sequential Corruption",
            _ => "Removing Ruler Tokens"
        };

        // What the variant changes, in one line (EXPANSIONS.md: F01, L03, X02).
        public static string VariantEffect(string variant) => variant switch
        {
            NewCities => "Ered Luin and South Rhûn count as Cities.",
            Council => "Companion versions stay private, and Companions may start outside the Fellowship; the Shadow then gains Council tokens.",
            Dismay => "A spent Ruler token or a fallen Awakened Sovereign gives the Shadow a Dismay token to re-roll a Ruler die.",
            VisibleCorruption => "Corruption tiles on the sovereigns are face up for both players.",
            SequentialCorruption => "A corruption attempt draws its tiles one at a time and keeps only the last.",
            _ => "No Ruler tokens are used; only a fallen Awakened Sovereign gives Dismay."
        };

        public static bool Editable(RoomView room) =>
            room != null && room.IsHost && room.Status == RoomStatus.Waiting && string.IsNullOrEmpty(room.MatchId);

        // EXP-01: the modules the server releases; a server from before it names none but may offer every expansion.
        public static bool Offered(RoomView room, string module) =>
            room.OfferedModules.Contains(module) || room.OfferedModules.Count == 0 && room.ExpansionsAvailable;

        // readiness: the rules are for players who then get ready (a waiting room); the demo page has no Ready.
        public static IReadOnlyList<RuleChoice> Modules(RoomView room, bool readiness = true) =>
            ModuleIds.Select(module => Module(room, module, readiness)).ToList();

        public static IReadOnlyList<RuleChoice> Variants(RoomView room, bool readiness = true) =>
            VariantIds.Select(variant => Variant(room, variant, readiness)).ToList();

        public static bool IsOn(MatchConfiguration configuration, string id) => id switch
        {
            ExpansionModules.Fate => configuration.Fate,
            ExpansionModules.Lords => configuration.Lords,
            ExpansionModules.Kings => configuration.Kings,
            NewCities => configuration.NewCities,
            Council => configuration.Council,
            Dismay => DismayOn(configuration),
            VisibleCorruption => configuration.VisibleCorruption,
            SequentialCorruption => configuration.SequentialCorruption,
            _ => configuration.RemovingRulerTokens
        };

        // The configuration after one click on a control: a copy with that rule switched and the variants that lost
        // their expansion switched off. Adding or removing Lords or Kings sets Dismay to its default (both modules).
        public static MatchConfiguration Toggle(MatchConfiguration current, string id)
        {
            var next = JsonConvert.DeserializeObject<MatchConfiguration>(JsonConvert.SerializeObject(current));
            var value = !IsOn(current, id);
            switch (id)
            {
                case ExpansionModules.Fate: next.Fate = value; break;
                case ExpansionModules.Lords: next.Lords = value; break;
                case ExpansionModules.Kings: next.Kings = value; break;
                case NewCities: next.NewCities = value; break;
                case Council: next.Council = value; break;
                case Dismay: next.Dismay = value; break;
                case VisibleCorruption: next.VisibleCorruption = value; break;
                case SequentialCorruption: next.SequentialCorruption = value; break;
                default: next.RemovingRulerTokens = value; break;
            }
            Dependencies(next, id == ExpansionModules.Lords || id == ExpansionModules.Kings);
            return next;
        }

        // The variant's expansion is in the game (the condition ConfigurationRules.Conflict checks on the server).
        public static bool Available(MatchConfiguration configuration, string variant) => variant switch
        {
            NewCities => configuration.Fate,
            Council => configuration.Lords,
            Dismay => configuration.Lords && configuration.Kings,
            VisibleCorruption or SequentialCorruption => configuration.Kings,
            _ => configuration.Lords && configuration.Kings && DismayOn(configuration)
        };

        public static bool IsBase(MatchConfiguration configuration) =>
            ModuleIds.Concat(VariantIds).All(id => !IsOn(configuration, id));

        // "Base game", or the expansions in release order, e.g. "Fate of Erebor + Kings of Middle-earth".
        public static string Summary(MatchConfiguration configuration)
        {
            var modules = ModuleIds.Where(module => IsOn(configuration, module)).Select(ModuleName).ToList();
            return modules.Count == 0 ? "Base game" : string.Join(" + ", modules);
        }

        // The variants in play, e.g. "New Cities · Dismay"; empty without any.
        public static string VariantSummary(MatchConfiguration configuration) =>
            string.Join(" · ", VariantIds.Where(variant => IsOn(configuration, variant)).Select(VariantName));

        private static bool DismayOn(MatchConfiguration configuration) =>
            configuration.Dismay ?? (configuration.Lords && configuration.Kings);

        private static RuleChoice Module(RoomView room, string module, bool readiness)
        {
            var on = IsOn(room.Configuration, module);
            var offered = Offered(room, module);
            return new RuleChoice
            {
                Id = module,
                Name = ModuleName(module),
                On = on,
                Enabled = Editable(room) && (offered || on),
                Tooltip = ModuleTooltip(room, module, on, offered, readiness ? AgainReady : "")
            };
        }

        private static string ModuleTooltip(RoomView room, string module, bool on, bool offered, string again)
        {
            var name = ModuleName(module);
            if (!offered && !on)
                return name + " · Not yet available on this server.";
            if (!Editable(room))
                return name + (on ? " · In this game." : " · Not in this game.") + ReadOnlyReason(room);
            if (on)
                return "Click to remove " + name + " from this game." + (HasVariants(room.Configuration, module)
                    ? " Its variants go with it." : "") + again;
            return "Click to add " + name + " to this game." + again;
        }

        private static RuleChoice Variant(RoomView room, string variant, bool readiness)
        {
            var on = IsOn(room.Configuration, variant);
            var available = Available(room.Configuration, variant);
            return new RuleChoice
            {
                Id = variant,
                Name = VariantName(variant),
                On = on,
                Enabled = Editable(room) && available,
                Tooltip = VariantTooltip(room, variant, on, available, readiness ? AgainReady : "")
            };
        }

        private static string VariantTooltip(RoomView room, string variant, bool on, bool available, string again)
        {
            var name = VariantName(variant);
            var effect = "\n" + VariantEffect(variant);
            if (!available)
                return name + " needs " + Requirement(variant) + "." + effect;
            if (!Editable(room))
                return name + (on ? " · In this game." : " · Not in this game.") + ReadOnlyReason(room) + effect;
            return (on ? "Click to play without " : "Click to play with ") + name + "." + again + effect;
        }

        private static string Requirement(string variant) => variant switch
        {
            NewCities => ModuleName(ExpansionModules.Fate),
            Council => ModuleName(ExpansionModules.Lords),
            Dismay => "Lords of Middle-earth and Kings of Middle-earth",
            VisibleCorruption or SequentialCorruption => ModuleName(ExpansionModules.Kings),
            _ => "Lords of Middle-earth, Kings of Middle-earth and Dismay"
        };

        private static string ReadOnlyReason(RoomView room) =>
            room.Status != RoomStatus.Waiting || !string.IsNullOrEmpty(room.MatchId)
                ? " The rules are fixed once the game starts."
                : " Only the host chooses the rules.";

        private static bool HasVariants(MatchConfiguration configuration, string module) =>
            VariantIds.Any(variant => IsOn(configuration, variant) && !Available(Without(configuration, module), variant));

        private static MatchConfiguration Without(MatchConfiguration configuration, string module) =>
            IsOn(configuration, module) ? Toggle(configuration, module) : configuration;

        private static void Dependencies(MatchConfiguration configuration, bool rulerModuleChanged)
        {
            if (!configuration.Lords)
                configuration.Council = false;
            if (rulerModuleChanged)
                configuration.Dismay = configuration.Lords && configuration.Kings;
            if (!DismayOn(configuration))
                configuration.RemovingRulerTokens = false;
            if (!configuration.Kings)
            {
                configuration.VisibleCorruption = false;
                configuration.SequentialCorruption = false;
            }
            if (!configuration.Fate)
                configuration.NewCities = false;
        }
    }
}

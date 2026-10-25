using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-331 (owner rule 2026-10-25: "no codes in the logs or notifications, only human-friendly names; a rule for the whole
    // application"): a recorded or server text as a player reads it. Every identifier in it reads as its name — a card by
    // its title, a character or sovereign by its name, a Hunt tile by its face, a unit by its nation and kind, a region or a
    // nation by its name, a side in words — and one without a known name as a neutral word, never the code. One pass: a
    // name put in is never read again.
    public static class ReadableText
    {
        private static readonly Regex Tokens = new Regex(
            // A dotted identifier (base.sh.character.04, base.hunt.special-sh.1.1.1, kings.sovereign.dain); a sentence's
            // full stop after it is not part of it.
            @"(?<id>\b(?:base|lords|kings|fate|council)(?:\.-?[A-Za-z0-9_]+(?:-[A-Za-z0-9_]+)*)+)" +
            // A side's enum name, as the server writes it.
            @"|(?<side>\bFreePeoples\b)" +
            // A lower-case word or kebab-case id: a region or a nation when it is one.
            @"|(?<word>\b[a-z]+(?:-[a-z]+)*\b)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> Nations = new HashSet<string>
        {
            "dwarves", "elves", "gondor", "north", "rohan", "isengard", "sauron", "southrons"
        };

        private static readonly HashSet<string> UnitKinds = new HashSet<string> { "regular", "elite", "leader", "nazgul" };

        public static string Of(string text, BoardPresentation data)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? "";
            return Tokens.Replace(text, match =>
            {
                if (match.Groups["id"].Success)
                    return Name(match.Value, data);
                if (match.Groups["side"].Success)
                    return BoardScreen.SideName(Side.FreePeoples);
                return Word(match.Value, data);
            });
        }

        // The name of one identifier of the content.
        public static string Name(string id, BoardPresentation data)
        {
            if (data?.HuntTiles != null && data.HuntTiles.TryGetValue(id, out var tile))
                return HuntTileText.Name(new HuntTileFaceView { Pool = tile.Pool, Value = tile.Value, Reveal = tile.Reveal, Stop = tile.Stop });
            if (CardLibrary.Cards.TryGetValue(id, out var card))
                return card.Title;
            if (id.StartsWith("kings.sovereign.", StringComparison.Ordinal))
                return ActionCardText.Source(id);
            if (CharacterLibrary.Find(id) != null)
                return CharacterLibrary.Name(id);
            if (id is ExpansionDraft.CouncilPolitics or ExpansionDraft.CouncilTravel)
                return ExpansionLabels.CouncilToken(id);
            var parts = id.Split('.');
            if (parts.Length == 4 && Nations.Contains(parts[1]) && UnitKinds.Contains(parts[2]))
                return FigureGrid.Name(parts[1]) + " " + (parts[2] == "nazgul" ? "Nazgûl" : FigureGrid.Name(parts[2]));
            return Neutral(id);
        }

        private static string Word(string word, BoardPresentation data)
        {
            if (data?.Regions != null && data.Regions.TryGetValue(word, out var region))
                return region.Name;
            return Nations.Contains(word) ? FigureGrid.Name(word) : word;
        }

        // An identifier without a known name: what kind of thing it is.
        public static string Neutral(string id)
        {
            var parts = id.Split('.');
            // A card names its side second (base.sh.character.04); a character its kind (base.character.strider).
            if (parts.Length > 2 && parts[1] is "fp" or "sh")
                return "a card";
            if (id.Contains(".hunt."))
                return "a Hunt tile";
            if (id.Contains(".character.") || id.Contains(".sovereign."))
                return "a character";
            if (id.Contains(".ring."))
                return "an Elven Ring";
            if (id.Contains(".die."))
                return "a die";
            if (id.Contains(".token.") || id.StartsWith("council.", StringComparison.Ordinal))
                return "a token";
            return "an effect";
        }
    }
}

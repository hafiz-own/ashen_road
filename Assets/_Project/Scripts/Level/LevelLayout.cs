using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>
    /// Parses a level's text grid. One character per 1x1 tile, first line = top row.
    ///
    ///   #  ground            =  one-way platform     ^  spikes          F  fire patch
    ///   D  falling debris    M  moving platform (→)  V  moving platform (↑)
    ///   S  Shambler          R  Runner               B  Brute
    ///   H  heart pickup      C  checkpoint           P  knight start    E  exit gate
    ///   K  boss              Q  princess             |  arena wall      !  tutorial sign
    ///   d  decoration        .  (or space) empty
    ///
    /// Pure C#: no scene needed, so it is unit-tested directly.
    /// </summary>
    public class LevelLayout
    {
        public struct Marker
        {
            public char Symbol;
            public int X;
            public int Y;

            public Marker(char symbol, int x, int y)
            {
                Symbol = symbol;
                X = x;
                Y = y;
            }

            public override string ToString() => $"'{Symbol}' at ({X},{Y})";
        }

        public const string SolidSymbols = "#";
        public const string MarkerSymbols = "^FDMVSRBHCPEKQ|!d";
        public const string EnemySymbols = "SRB";

        readonly char[,] cells; // [x, y], y = 0 is the bottom row

        public int Width { get; }
        public int Height { get; }
        public List<Marker> Markers { get; } = new List<Marker>();

        LevelLayout(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new char[width, height];
        }

        public static LevelLayout Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("Level layout is empty.");
            var lines = new List<string>(text.Replace("\r", "").Split('\n'));
            while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
            while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);

            int width = 0;
            foreach (string l in lines) width = Math.Max(width, l.TrimEnd().Length);
            int height = lines.Count;
            var layout = new LevelLayout(width, height);

            for (int row = 0; row < height; row++)
            {
                string line = lines[row];
                int y = height - 1 - row;
                for (int x = 0; x < width; x++)
                {
                    char c = x < line.Length ? line[x] : '.';
                    if (c == ' ') c = '.';
                    if (c != '.' && c != '=' && SolidSymbols.IndexOf(c) < 0 && MarkerSymbols.IndexOf(c) < 0)
                        throw new FormatException($"Unknown symbol '{c}' at column {x}, row {row}.");
                    layout.cells[x, y] = c;
                    if (MarkerSymbols.IndexOf(c) >= 0) layout.Markers.Add(new Marker(c, x, y));
                }
            }
            return layout;
        }

        public char At(int x, int y) => InBounds(x, y) ? cells[x, y] : '.';

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsSolid(int x, int y) => InBounds(x, y) && SolidSymbols.IndexOf(cells[x, y]) >= 0;

        public bool IsOneWay(int x, int y) => InBounds(x, y) && cells[x, y] == '=';

        public IEnumerable<Marker> All(char symbol)
        {
            foreach (Marker m in Markers)
                if (m.Symbol == symbol) yield return m;
        }

        public int Count(char symbol)
        {
            int n = 0;
            foreach (Marker m in Markers)
                if (m.Symbol == symbol) n++;
            return n;
        }

        public Marker First(char symbol)
        {
            foreach (Marker m in Markers)
                if (m.Symbol == symbol) return m;
            throw new InvalidOperationException($"No '{symbol}' in layout.");
        }

        /// <summary>World position of the bottom-centre of a cell (where a character placed there stands).</summary>
        public static Vector2 CellToWorld(int x, int y) => new Vector2(x + 0.5f, y);

        /// <summary>Checks the rules every playable level must satisfy. Returns the problems found.</summary>
        public List<string> Validate(bool bossLevel)
        {
            var errors = new List<string>();
            if (Count('P') != 1) errors.Add($"Needs exactly one P (knight start), found {Count('P')}.");
            if (bossLevel)
            {
                if (Count('K') != 1) errors.Add("Boss level needs exactly one K.");
                if (Count('Q') != 1) errors.Add("Boss level needs exactly one Q (princess).");
                if (Count('|') != 2) errors.Add("Boss level needs two | arena walls.");
            }
            else
            {
                if (Count('E') != 1) errors.Add($"Needs exactly one E (exit), found {Count('E')}.");
                if (Count('K') > 0 || Count('Q') > 0) errors.Add("Only the boss level may contain K or Q.");
            }
            foreach (Marker m in Markers)
            {
                if ("PECHSRBKQ!".IndexOf(m.Symbol) < 0) continue;
                if (!IsSolid(m.X, m.Y - 1) && !IsOneWay(m.X, m.Y - 1))
                    errors.Add($"{m} must stand on ground.");
            }
            if (Count('P') == 1 && (bossLevel ? Count('Q') == 1 : Count('E') == 1))
            {
                Marker p = First('P');
                Marker goal = First(bossLevel ? 'Q' : 'E');
                if (goal.X <= p.X) errors.Add("The goal must be to the right of the start.");
            }
            return errors;
        }
    }
}

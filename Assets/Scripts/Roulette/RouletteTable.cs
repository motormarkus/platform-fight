using System;
using System.Collections.Generic;
using System.Linq;

namespace TotalFist.Roulette
{
    public enum BetKind { Straight, Split, Street, Trio, Corner, Line, Dozen, Column, Even }

    /// <summary>Yksi panostyyppi: numerot, jotka voittavat, ja kerroin (voitto = panos x Pays, panos palautuu). PosU/PosV = merkin paikka pöytäkuvassa (pikseleinä).</summary>
    public sealed class BetSpec
    {
        public string Key; public BetKind Kind; public int[] Numbers; public int Pays; public string Label; public float PosU, PosV;
        public static BetSpec Make(BetKind kind, string label, float u, float v, params int[] numbers)
        {
            var n = numbers.OrderBy(x => x).ToArray();
            int pays;
            switch (kind)
            {
                case BetKind.Straight: pays = 35; break;
                case BetKind.Split: pays = 17; break;
                case BetKind.Street: case BetKind.Trio: pays = 11; break;
                case BetKind.Corner: pays = 8; break;
                case BetKind.Line: pays = 5; break;
                case BetKind.Dozen: case BetKind.Column: pays = 2; break;
                default: pays = 1; break;
            }
            return new BetSpec { Key = kind + ":" + string.Join(",", n), Kind = kind, Numbers = n, Pays = pays, Label = label, PosU = u, PosV = v };
        }
    }

    public struct BetResult { public BetSpec Spec; public int Stake, Won; }   // Won = kokonaispalautus (panos + voitto), 0 jos hävisi

    /// <summary>Eurooppalaisen ruletin säännöt ja panokset. Ei rahaa eikä Unityä: raha käsitellään kutsujan puolella (Place/Undo/Clear palauttavat summat).</summary>
    public sealed class RouletteTable
    {
        /// <summary>Numerot pyörässä myötäpäivään nollasta (pyöräkuvan järjestys).</summary>
        public static readonly int[] WheelOrder = { 0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26 };
        static readonly HashSet<int> Reds = new HashSet<int> { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        public static bool IsRed(int n) => Reds.Contains(n);
        public static bool IsBlack(int n) => n > 0 && !Reds.Contains(n);
        public static string ColorName(int n) => n == 0 ? "vihreä" : IsRed(n) ? "punainen" : "musta";
        public static int PocketIndex(int n) => Array.IndexOf(WheelOrder, n);

        public const int MaxPerSpot = 10000;
        public sealed class Entry { public BetSpec Spec; public int Amount; }
        readonly List<Entry> entries = new List<Entry>();
        readonly Stack<KeyValuePair<string, int>> history = new Stack<KeyValuePair<string, int>>();
        public IReadOnlyList<Entry> Bets => entries;
        public int TotalStake => entries.Sum(e => e.Amount);
        public int AmountOn(string key) { var e = entries.Find(x => x.Spec.Key == key); return e == null ? 0 : e.Amount; }

        /// <summary>Lisää panoksen (chip). Palauttaa todellisen lisätyn summan (0 jos katto täyttyi).</summary>
        public int Place(BetSpec spec, int chip)
        {
            if (spec == null || chip <= 0) return 0;
            var e = entries.Find(x => x.Spec.Key == spec.Key);
            int cur = e == null ? 0 : e.Amount, add = Math.Min(chip, MaxPerSpot - cur);
            if (add <= 0) return 0;
            if (e == null) entries.Add(new Entry { Spec = spec, Amount = add }); else e.Amount += add;
            history.Push(new KeyValuePair<string, int>(spec.Key, add));
            return add;
        }
        /// <summary>Peruu viimeisen lisäyksen, palauttaa takaisin saatavan summan.</summary>
        public int Undo()
        {
            if (history.Count == 0) return 0;
            var h = history.Pop(); var e = entries.Find(x => x.Spec.Key == h.Key);
            if (e == null) return 0;
            e.Amount -= h.Value; if (e.Amount <= 0) entries.Remove(e);
            return h.Value;
        }
        public int Clear() { int s = TotalStake; entries.Clear(); history.Clear(); return s; }

        /// <summary>Selvittää kierroksen: jokaiselle panokselle voitto (panos + panos x kerroin) tai 0. Panokset tyhjennetään.</summary>
        public List<BetResult> Resolve(int number)
        {
            var res = entries.Select(e => new BetResult { Spec = e.Spec, Stake = e.Amount, Won = e.Spec.Numbers.Contains(number) ? e.Amount * (e.Spec.Pays + 1) : 0 }).ToList();
            entries.Clear(); history.Clear();
            return res;
        }
        public List<BetResult> Preview(int number) => entries.Select(e => new BetResult { Spec = e.Spec, Stake = e.Amount, Won = e.Spec.Numbers.Contains(number) ? e.Amount * (e.Spec.Pays + 1) : 0 }).ToList();
    }

    /// <summary>Pöytäkuvan (panel.png, 2108x1228 px) klikattavat alueet. u oikealle, v alas, pikseleinä.</summary>
    public static class RouletteLayout
    {
        public const float PanelW = 2108, PanelH = 1228;
        const float X0 = 201, DX = 140, Y0 = 233, DY = 116, CW = 137, CH = 101, GapY = 7.5f;
        public static readonly float[] ChipX = { 169, 326, 486, 643 };
        public const float ChipY = 1063, ChipR = 58;
        public static readonly int[] ChipValues = { 1, 5, 25, 100 };
        public static readonly string[] EvenLabels = { "1–18", "Parillinen", "Punainen", "Musta", "Pariton", "19–36" };

        public enum Zone { None, Bet, Chip, Clear, Spin }
        public struct Hit { public Zone Zone; public BetSpec Spec; public int Chip; }

        public static float CellU(int col) => X0 + DX * col + CW * .5f;
        public static float CellV(int row) => Y0 + DY * row + CH * .5f;
        static int Num(int col, int row) => 3 * col + 3 - row;
        public static bool CellRect(int n, out float u, out float v, out float w, out float h)
        {
            if (n == 0) { u = 85; v = 226; w = 113; h = 344; return true; }
            int col = (n - 1) / 3, row = 2 - ((n - 1) % 3);
            u = X0 + DX * col; v = Y0 + DY * row; w = CW; h = CH; return true;
        }

        static BetSpec Straight(int n)
        {
            if (n == 0) return BetSpec.Make(BetKind.Straight, "0", 141, 398, 0);
            int col = (n - 1) / 3, row = 2 - ((n - 1) % 3);
            return BetSpec.Make(BetKind.Straight, n.ToString(), CellU(col), CellV(row), n);
        }
        static float LineU(int k) => X0 + DX * k - 1.5f;
        static float GapV(int k) => Y0 - GapY + DY * k;

        public static Hit Probe(float u, float v)
        {
            // pelimerkit ja napit
            for (int i = 0; i < 4; i++) { float du = u - ChipX[i], dv = v - ChipY; if (du * du + dv * dv <= ChipR * ChipR) return new Hit { Zone = Zone.Chip, Chip = i }; }
            if (u >= 1062 && u <= 1432 && v >= 1002 && v <= 1127) return new Hit { Zone = Zone.Clear };
            if (u >= 1457 && u <= 1976 && v >= 1002 && v <= 1127) return new Hit { Zone = Zone.Spin };
            var bet = ProbeBet(u, v);
            return bet != null ? new Hit { Zone = Zone.Bet, Spec = bet } : new Hit();
        }

        static BetSpec ProbeBet(float u, float v)
        {
            const float mx = .2f, my = .2f;
            // nolla ja sen jaot ykkös-/kakkos-/kolmosruutuihin
            if (u >= 85 && u < X0 + mx * DX && v >= 226 && v <= 572)
            {
                if (u < X0 - 28) return Straight(0);
                float g = (v - (Y0 - GapY)) / DY; int k = (int)Math.Round(g);
                if ((k == 1 || k == 2) && Math.Abs(g - k) < my)
                    return k == 1 ? BetSpec.Make(BetKind.Trio, "0-2-3", LineU(0), GapV(1), 0, 2, 3) : BetSpec.Make(BetKind.Trio, "0-1-2", LineU(0), GapV(2), 0, 1, 2);
                int row = Math.Max(0, Math.Min(2, (int)Math.Floor(g)));
                int n = Num(0, row);
                return BetSpec.Make(BetKind.Split, "0-" + n, LineU(0), CellV(row), 0, n);
            }
            // 2:1 sarakkeet
            if (u >= 1881 && u <= 2006 && v >= Y0 - 8 && v <= Y0 + 3 * DY - 8)
            {
                int row = Math.Max(0, Math.Min(2, (int)Math.Floor((v - (Y0 - GapY)) / DY)));
                int rem = row == 0 ? 0 : row == 1 ? 2 : 1;
                var nums = Enumerable.Range(1, 36).Where(x => x % 3 == rem).ToArray();
                return BetSpec.Make(BetKind.Column, "Sarake 2:1", 1943, CellV(row), nums);
            }
            // tusinat
            if (u >= 201 && u < 1880 && v >= 585 && v <= 685)
            {
                int i = Math.Max(0, Math.Min(2, (int)Math.Floor((u - 201) / 558f)));
                return BetSpec.Make(BetKind.Dozen, (12 * i + 1) + "–" + (12 * i + 12), 201 + 558 * i + 279, 635, Enumerable.Range(12 * i + 1, 12).ToArray());
            }
            // ulkopanokset (1:1)
            if (u >= 201 && u < 1881 && v >= 694 && v <= 796)
            {
                int i = Math.Max(0, Math.Min(5, (int)Math.Floor((u - 201) / 280f)));
                int[] nums;
                switch (i)
                {
                    case 0: nums = Enumerable.Range(1, 18).ToArray(); break;
                    case 1: nums = Enumerable.Range(1, 36).Where(x => x % 2 == 0).ToArray(); break;
                    case 2: nums = Enumerable.Range(1, 36).Where(RouletteTable.IsRed).ToArray(); break;
                    case 3: nums = Enumerable.Range(1, 36).Where(RouletteTable.IsBlack).ToArray(); break;
                    case 4: nums = Enumerable.Range(1, 36).Where(x => x % 2 == 1).ToArray(); break;
                    default: nums = Enumerable.Range(19, 18).ToArray(); break;
                }
                return BetSpec.Make(BetKind.Even, EvenLabels[i], 201 + 280 * i + 140, 745, nums);
            }
            // numeroruudukko
            if (u >= X0 - 40 && u < X0 + 12 * DX && v >= Y0 - 14 && v <= 585)
            {
                float gx = (u - X0) / DX, gy = (v - (Y0 - GapY)) / DY;
                int kx = (int)Math.Round(gx), ky = (int)Math.Round(gy);
                bool nv = Math.Abs(gx - kx) < mx && kx >= 1 && kx <= 11;
                bool nh = Math.Abs(gy - ky) < my && ky >= 1 && ky <= 2;
                bool edge = (ky == 3 && gy > 3 - my && v < 585) || (ky == 0 && gy < my);
                int col = Math.Max(0, Math.Min(11, (int)Math.Floor(gx))), row = Math.Max(0, Math.Min(2, (int)Math.Floor(gy)));
                if (edge)
                {
                    float ev = ky == 3 ? 568 : GapV(0) + 4;
                    if (nv) return BetSpec.Make(BetKind.Line, "Linja " + Num(kx - 1, 2) + "–" + Num(kx, 0), LineU(kx), ev, Num(kx - 1, 0), Num(kx - 1, 1), Num(kx - 1, 2), Num(kx, 0), Num(kx, 1), Num(kx, 2));
                    return BetSpec.Make(BetKind.Street, "Katu " + Num(col, 2) + "-" + Num(col, 0), CellU(col), ev, Num(col, 0), Num(col, 1), Num(col, 2));
                }
                if (nv && nh) return BetSpec.Make(BetKind.Corner, "Kulma", LineU(kx), GapV(ky), Num(kx - 1, ky - 1), Num(kx, ky - 1), Num(kx - 1, ky), Num(kx, ky));
                if (nv) return BetSpec.Make(BetKind.Split, "Jako " + Num(kx - 1, row) + "-" + Num(kx, row), LineU(kx), CellV(row), Num(kx - 1, row), Num(kx, row));
                if (nh) return BetSpec.Make(BetKind.Split, "Jako " + Num(col, ky - 1) + "-" + Num(col, ky), CellU(col), GapV(ky), Num(col, ky - 1), Num(col, ky));
                return Straight(Num(col, row));
            }
            return null;
        }
    }
}

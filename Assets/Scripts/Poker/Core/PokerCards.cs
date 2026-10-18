using System;
using System.Collections.Generic;

namespace TotalFist.Poker
{
    // Card ID = suit * 13 + rank - 2. Suits: clubs, diamonds, hearts, spades.
    public static class PokerCards
    {
        public static int Rank(int card) => card % 13 + 2;
        public static int Suit(int card) => card / 13;
        public static string Text(int card)
        {
            if (card < 0 || card >= 52) return "?";
            int r = Rank(card);
            return (r < 11 ? r.ToString() : new[] { "J", "Q", "K", "A" }[r - 11]) + new[] { "♣", "♦", "♥", "♠" }[Suit(card)];
        }

        // Base 15 digits: category, then all five relevant tie breakers, high first.
        public static long Evaluate(IList<int> cards)
        {
            if (cards == null || cards.Count < 5 || cards.Count > 7) throw new ArgumentException("Need 5–7 cards");
            ulong seen = 0;
            foreach (int c in cards)
            {
                if (c < 0 || c >= 52 || (seen & (1UL << c)) != 0) throw new ArgumentException("Invalid/duplicate card");
                seen |= 1UL << c;
            }
            long best = -1;
            var five = new int[5];
            for (int a = 0; a < cards.Count - 4; a++)
            for (int b = a + 1; b < cards.Count - 3; b++)
            for (int c = b + 1; c < cards.Count - 2; c++)
            for (int d = c + 1; d < cards.Count - 1; d++)
            for (int e = d + 1; e < cards.Count; e++)
            {
                five[0] = cards[a]; five[1] = cards[b]; five[2] = cards[c]; five[3] = cards[d]; five[4] = cards[e];
                best = Math.Max(best, Five(five));
            }
            return best;
        }

        // The five cards that make up the best hand (used to highlight the winning hand).
        public static int[] BestFive(IList<int> cards)
        {
            long best = -1; var result = new int[5]; var five = new int[5];
            for (int a = 0; a < cards.Count - 4; a++)
            for (int b = a + 1; b < cards.Count - 3; b++)
            for (int c = b + 1; c < cards.Count - 2; c++)
            for (int d = c + 1; d < cards.Count - 1; d++)
            for (int e = d + 1; e < cards.Count; e++)
            {
                five[0] = cards[a]; five[1] = cards[b]; five[2] = cards[c]; five[3] = cards[d]; five[4] = cards[e];
                long v = Five(five);
                if (v > best) { best = v; Array.Copy(five, result, 5); }
            }
            return result;
        }

        static long Pack(int category, params int[] ranks)
        {
            long value = category;
            for (int i = 0; i < 5; i++) value = value * 15 + (i < ranks.Length ? ranks[i] : 0);
            return value;
        }

        static long Five(int[] cards)
        {
            var count = new int[15]; var sorted = new int[5];
            bool flush = true;
            for (int i = 0; i < 5; i++) { sorted[i] = Rank(cards[i]); count[sorted[i]]++; flush &= Suit(cards[i]) == Suit(cards[0]); }
            Array.Sort(sorted); Array.Reverse(sorted);
            int straight = 0;
            for (int high = 14; high >= 5; high--)
            {
                bool yes = true;
                for (int j = 0; j < 5; j++) if (count[high - j == 1 ? 14 : high - j] == 0) yes = false;
                if (yes) { straight = high; break; }
            }
            if (flush && straight > 0) return Pack(8, straight);
            int four = 0, three = 0; var pairs = new List<int>(); var singles = new List<int>();
            for (int r = 14; r >= 2; r--)
            {
                if (count[r] == 4) four = r;
                if (count[r] == 3) three = r;
                if (count[r] == 2) pairs.Add(r);
                if (count[r] == 1) singles.Add(r);
            }
            if (four > 0) return Pack(7, four, singles[0]);
            if (three > 0 && pairs.Count > 0) return Pack(6, three, pairs[0]);
            if (flush) return Pack(5, sorted);
            if (straight > 0) return Pack(4, straight);
            if (three > 0) return Pack(3, three, singles[0], singles[1]);
            if (pairs.Count == 2) return Pack(2, pairs[0], pairs[1], singles[0]);
            if (pairs.Count == 1) return Pack(1, pairs[0], singles[0], singles[1], singles[2]);
            return Pack(0, sorted);
        }

        public static string HandName(long value)
        {
            for (int i = 0; i < 5; i++) value /= 15;
            return new[] { "Hai", "Pari", "Kaksi paria", "Kolmoset", "Suora", "Väri", "Täyskäsi", "Neloset", "Värisuora" }[(int)value];
        }
    }
}

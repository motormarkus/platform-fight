using System;
using System.Collections.Generic;
using System.Linq;

namespace TotalFist.Poker
{
    public enum PokerStyle { Cautious, Aggressive, Loose, Balanced }

    // This is the ENTIRE bot input. No table, deck, opposing hole cards or callbacks.
    public sealed class PokerObservation
    {
        public int[] Hole, Board;
        public PokerVariant Variant;
        public int Pot, Stack, Bet, Opponents, BigBlind;
        public LegalActions Legal;
        // Public betting context: street, 0 (acts first) .. 1 (acts last) among live players,
        // and how many live opponents have bet or raised during this hand.
        public PokerStreet Street;
        public double Position = .5;
        public int Aggressors;
    }
    public struct PokerDecision
    {
        public PokerAction Action;
        public int RaiseTo;
        public PokerDecision(PokerAction action, int raiseTo = 0) { Action = action; RaiseTo = raiseTo; }
    }

    public static class PokerBot
    {
        public static PokerObservation Observe(PokerTable table, int seat)
        {
            var s = table.Seats[seat];
            // Post-flop order starts left of the dealer; the dealer acts last.
            int n = table.Seats.Length, live = 0, before = 0;
            for (int i = 1; i <= n; i++)
            {
                int k = ((table.Dealer < 0 ? 0 : table.Dealer) + i) % n;
                if (!(table.Seats[k].InHand && !table.Seats[k].Folded)) continue;
                if (k == seat) before = live;
                live++;
            }
            int aggressors = 0;
            for (int i = 0; i < n; i++)
            {
                var o = table.Seats[i]; string a = o.LastAction ?? "";
                if (i != seat && o.InHand && !o.Folded && (a.StartsWith("Korotti") || a.StartsWith("All-in"))) aggressors++;
            }
            return new PokerObservation { Variant = table.Variant, Hole = (int[])s.Hole.Clone(), Board = table.Board.ToArray(), Pot = table.Pot,
                Stack = s.Stack, Bet = s.Bet, BigBlind = table.BigBlind,
                Opponents = table.Seats.Count(x => x.InHand && !x.Folded) - 1, Legal = table.Legal(seat),
                Street = table.Street, Position = live > 1 ? before / (double)(live - 1) : .5, Aggressors = aggressors };
        }

        // Omaha-aloituskäsi 0..1: kuuden kahden kortin parin paras puolikas + värit, suoramahdollisuus, ei kolmosia/neloseja.
        public static double OmahaPreflopStrength(int[] hole)
        {
            var pairs = new List<double>();
            for (int a = 0; a < 3; a++) for (int b = a + 1; b < 4; b++) pairs.Add(PreflopStrength(new[] { hole[a], hole[b] }));
            pairs.Sort(); pairs.Reverse();
            double score = .55 * pairs[0] + .3 * pairs[1] + .15 * pairs[2];
            var suitCounts = hole.GroupBy(PokerCards.Suit).Select(g => g.Count()).OrderByDescending(x => x).ToArray();
            if (suitCounts[0] == 2 && suitCounts.Length > 1 && suitCounts[1] == 2) score += .08;       // kaksi värimahdollisuutta
            else if (suitCounts[0] == 2) score += .03;
            else if (suitCounts[0] >= 3) score -= .06;                                                   // 3+ samaa maata: huono
            var ranks = hole.Select(PokerCards.Rank).OrderBy(x => x).ToArray();
            var rankGroups = ranks.GroupBy(x => x).Select(g => g.Count()).OrderByDescending(x => x).ToArray();
            if (rankGroups[0] >= 3) score -= .2;                                                          // kolmoset/neloset kädessä
            else if (rankGroups[0] == 2 && rankGroups.Length == 2) score += .02;                          // kaksi paria
            else if (rankGroups[0] == 1 && ranks[3] - ranks[0] <= 4) score += .08;                       // rundown / suoramahdollisuus
            else if (rankGroups[0] == 1 && ranks[3] - ranks[0] >= 9 && ranks[0] < 8) score -= .05;       // hajanainen
            if (ranks[3] == 14 && (rankGroups[0] == 2 && ranks.Count(x => x == 14) == 2)) score += .08;  // pari ässiä
            return Math.Max(0, Math.Min(1, score));
        }

        // Rough 0..1 preflop strength (Chen-style): high card, pairs, suitedness, connectedness.
        public static double PreflopStrength(int[] hole)
        {
            int hi = Math.Max(PokerCards.Rank(hole[0]), PokerCards.Rank(hole[1])), lo = Math.Min(PokerCards.Rank(hole[0]), PokerCards.Rank(hole[1]));
            bool suited = PokerCards.Suit(hole[0]) == PokerCards.Suit(hole[1]);
            double score = hi == 14 ? 10 : hi == 13 ? 8 : hi == 12 ? 7 : hi == 11 ? 6 : hi / 2.0;
            if (hi == lo) score = Math.Max(5, score * 2);
            else
            {
                if (suited) score += 2;
                int gap = hi - lo - 1;
                score -= gap == 0 ? 0 : gap == 1 ? 1 : gap == 2 ? 2 : gap == 3 ? 4 : 5;
                if (gap <= 1 && hi < 12) score += 1;
            }
            return Math.Max(0, Math.Min(1, (score - 1) / 19));
        }

        public static PokerDecision Decide(PokerObservation view, PokerStyle style, Random rng, int samples = 64)
        {
            var l = view.Legal;
            bool preflop = view.Street == PokerStreet.Preflop;
            // Tunable personality: how strong a hand must be to bet, how much price it needs to call,
            // how often it bluffs and how much it respects other players' aggression.
            double valueEdge, callMargin, bluffRate, respect, openStrength, sizing;
            switch (style)
            {
                case PokerStyle.Cautious: valueEdge = .30; callMargin = .08; bluffRate = .03; respect = 1.4; openStrength = .46; sizing = .5; break;
                case PokerStyle.Aggressive: valueEdge = .16; callMargin = .0; bluffRate = .22; respect = .6; openStrength = .26; sizing = .85; break;
                case PokerStyle.Loose: valueEdge = .30; callMargin = -.07; bluffRate = .08; respect = .5; openStrength = .22; sizing = .45; break;
                default: valueEdge = .22; callMargin = .02; bluffRate = .09; respect = 1; openStrength = .36; sizing = .65; break;
            }
            double equity = Equity(view, rng, samples);
            double potSize = Math.Max(1, view.Pot);
            double betWeight = Math.Min(1.5, l.CallAmount / potSize);
            // A bet or raise means the opponent's range is stronger than a random hand.
            if (l.CallAmount > 0 && view.Aggressors > 0)
                equity *= 1 - Math.Min(.4, .1 * respect * view.Aggressors * (.5 + betWeight));
            if (preflop)
            {
                // Random-hand equity underrates good starting hands in multiway pots: blend in the table.
                double strength = view.Variant == PokerVariant.Omaha ? OmahaPreflopStrength(view.Hole) : PreflopStrength(view.Hole);
                strength += (view.Position - .5) * .12;
                if (l.CallAmount > view.BigBlind) strength -= .08 * respect * Math.Max(1, view.Aggressors);
                equity = .5 * equity + .5 * Math.Min(.85, strength + .1);
                valueEdge = Math.Max(.1, valueEdge * .7);
            }
            double fair = 1.0 / (view.Opponents + 1);
            double odds = l.CallAmount / (double)Math.Max(1, view.Pot + l.CallAmount);
            double commit = l.CallAmount / (double)Math.Max(1, view.Stack);
            double margin = callMargin + (commit > .5 ? .06 : 0) + (view.Aggressors >= 2 ? .05 : 0) - (view.Position - .5) * .04;
            bool canRaise = l.Raise || l.AllIn;
            bool facingBet = l.CallAmount > 0;
            // Bluff: only when nobody has shown strength, few opponents, better in late position.
            bool bluff = !facingBet && view.Aggressors == 0 && view.Opponents <= (preflop ? 4 : 2) && equity < fair + valueEdge
                && rng.NextDouble() < bluffRate * (.5 + view.Position) * (preflop && equity < .25 ? .4 : 1);
            bool value = equity > fair + valueEdge && equity > (view.Aggressors >= 2 ? .75 : facingBet ? .55 : .0);
            bool openRaise = preflop && !facingBet && l.CallAmount == 0 && equity > openStrength + fair * .3 - .06 && view.Position > .25;
            if (canRaise && (value || bluff || openRaise))
            {
                double big = equity > .8 ? 1.0 : sizing;
                int target = view.Bet + l.CallAmount + Math.Max(view.BigBlind, (int)((view.Pot + l.CallAmount) * big * (.8 + .4 * rng.NextDouble())));
                if (preflop && !facingBet) target = Math.Max(target, view.BigBlind * (2 + (rng.NextDouble() < .5 ? 1 : 0)));
                target = Math.Max(l.MinimumRaiseTo, Math.Min(l.MaximumRaiseTo, target));
                bool shove = target >= view.Bet + view.Stack - view.BigBlind || !l.Raise;
                if (shove)
                {
                    // Shoving is for strong hands or short stacks, never a casual bluff.
                    if (l.AllIn && (equity > .72 || (bluff && view.Stack <= view.Pot && rng.NextDouble() < .3)))
                        return new PokerDecision(PokerAction.AllIn);
                    if (!l.AllIn && l.Raise) return new PokerDecision(PokerAction.RaiseTo, target);   // pot limit: potin kokoinen korotus, ei shovea
                }
                else return new PokerDecision(PokerAction.RaiseTo, target);
            }
            if (l.Check) return new PokerDecision(PokerAction.Check);
            if (l.Call && (equity + .06 - margin >= odds || (style == PokerStyle.Loose && commit < .15 && rng.NextDouble() < .2))) return new PokerDecision(PokerAction.Call);
            return new PokerDecision(PokerAction.Fold);
        }

        public static double Equity(PokerObservation view, Random rng, int samples)
        {
            if (view.Opponents <= 0) return 1;
            bool omaha = view.Variant == PokerVariant.Omaha; int holeN = omaha ? 4 : 2;
            var known = new HashSet<int>(view.Hole.Concat(view.Board));
            int[] remaining = Enumerable.Range(0, 52).Where(c => !known.Contains(c)).ToArray();
            double score = 0;
            samples = Math.Max(1, Math.Min(128, samples));
            var board = new int[5]; var hand = new int[7]; var oppHole = new int[4];
            for (int trial = 0; trial < samples; trial++)
            {
                int needed = 5 - view.Board.Length + view.Opponents * holeN;
                for (int i = 0; i < needed; i++) { int j = rng.Next(i, remaining.Length); int c = remaining[i]; remaining[i] = remaining[j]; remaining[j] = c; }
                for (int i = 0; i < view.Board.Length; i++) board[i] = view.Board[i];
                int cursor = 0;
                for (int i = view.Board.Length; i < 5; i++) board[i] = remaining[cursor++];
                Array.Copy(board, 0, hand, 2, 5); hand[0] = view.Hole[0]; hand[1] = view.Hole[1];
                long mine = omaha ? PokerCards.EvaluateOmaha(view.Hole, board) : PokerCards.Evaluate(hand); int ties = 1; bool lose = false;
                for (int n = 0; n < view.Opponents; n++)
                {
                    long theirs;
                    if (omaha) { for (int h = 0; h < 4; h++) oppHole[h] = remaining[cursor++]; theirs = PokerCards.EvaluateOmaha(oppHole, board); }
                    else { hand[0] = remaining[cursor++]; hand[1] = remaining[cursor++]; theirs = PokerCards.Evaluate(hand); }
                    if (theirs > mine) { lose = true; break; }
                    if (theirs == mine) ties++;
                }
                if (!lose) score += 1d / ties;
            }
            return score / samples;
        }
    }
}

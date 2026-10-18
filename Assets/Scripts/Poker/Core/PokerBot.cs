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
        public int Pot, Stack, Bet, Opponents, BigBlind;
        public LegalActions Legal;
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
            return new PokerObservation { Hole = (int[])s.Hole.Clone(), Board = table.Board.ToArray(), Pot = table.Pot,
                Stack = s.Stack, Bet = s.Bet, BigBlind = table.BigBlind,
                Opponents = table.Seats.Count(x => x.InHand && !x.Folded) - 1, Legal = table.Legal(seat) };
        }

        public static PokerDecision Decide(PokerObservation view, PokerStyle style, Random rng, int samples = 36)
        {
            var l = view.Legal;
            double equity = Equity(view, rng, samples);
            double tightness = style == PokerStyle.Cautious ? .13 : style == PokerStyle.Loose ? -.13 : 0;
            double aggression = style == PokerStyle.Aggressive ? .22 : style == PokerStyle.Cautious ? -.08 : .06;
            bool bluff = rng.NextDouble() < (style == PokerStyle.Aggressive ? .14 : style == PokerStyle.Cautious ? .025 : .075);
            double odds = l.CallAmount / (double)Math.Max(1, view.Pot + l.CallAmount);
            if ((equity > .56 - aggression || bluff) && (l.Raise || l.AllIn))
            {
                int target = view.Bet + l.CallAmount + Math.Max(view.BigBlind, (int)((view.Pot + l.CallAmount) * (style == PokerStyle.Aggressive ? .85 : .5)));
                target = Math.Max(l.MinimumRaiseTo, Math.Min(l.MaximumRaiseTo, target));
                if (l.Raise) return new PokerDecision(PokerAction.RaiseTo, target);
                if (l.AllIn && (equity > .65 || bluff)) return new PokerDecision(PokerAction.AllIn);
            }
            if (l.Check) return new PokerDecision(PokerAction.Check);
            if (l.Call && (equity + .08 - tightness >= odds || (style == PokerStyle.Loose && rng.NextDouble() < .16))) return new PokerDecision(PokerAction.Call);
            return new PokerDecision(PokerAction.Fold);
        }

        public static double Equity(PokerObservation view, Random rng, int samples)
        {
            if (view.Opponents <= 0) return 1;
            var known = new HashSet<int>(view.Hole.Concat(view.Board));
            int[] remaining = Enumerable.Range(0, 52).Where(c => !known.Contains(c)).ToArray();
            double score = 0;
            samples = Math.Max(1, Math.Min(128, samples));
            var board = new int[5]; var hand = new int[7];
            for (int trial = 0; trial < samples; trial++)
            {
                int needed = 5 - view.Board.Length + view.Opponents * 2;
                for (int i = 0; i < needed; i++) { int j = rng.Next(i, remaining.Length); int c = remaining[i]; remaining[i] = remaining[j]; remaining[j] = c; }
                for (int i = 0; i < view.Board.Length; i++) board[i] = view.Board[i];
                int cursor = 0;
                for (int i = view.Board.Length; i < 5; i++) board[i] = remaining[cursor++];
                Array.Copy(board, 0, hand, 2, 5); hand[0] = view.Hole[0]; hand[1] = view.Hole[1];
                long mine = PokerCards.Evaluate(hand); int ties = 1; bool lose = false;
                for (int n = 0; n < view.Opponents; n++)
                {
                    hand[0] = remaining[cursor++]; hand[1] = remaining[cursor++];
                    long theirs = PokerCards.Evaluate(hand);
                    if (theirs > mine) { lose = true; break; }
                    if (theirs == mine) ties++;
                }
                if (!lose) score += 1d / ties;
            }
            return score / samples;
        }
    }
}

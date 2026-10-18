using System;
using System.Collections.Generic;
using System.Linq;

namespace TotalFist.Poker
{
    public enum PokerStreet { BetweenHands, Preflop, Flop, Turn, River, Complete }
    public enum PokerAction { Fold, Check, Call, RaiseTo, AllIn }

    [Serializable]
    public sealed class PokerSeat
    {
        public string Name;
        public int Stack, Bet, Committed;
        public bool InHand, Folded, Acted;
        public int ActedAtBet;
        public int[] Hole = { -1, -1 };
        public string LastAction = "";
    }

    [Serializable]
    public sealed class PotAward
    {
        public int Amount;
        public int[] Winners;
        public string Hand;
    }

    public sealed class LegalActions
    {
        public bool Fold, Check, Call, Raise, AllIn;
        public int CallAmount, MinimumRaiseTo, MaximumRaiseTo;
    }

    [Serializable]
    public sealed class PokerTable
    {
        public PokerSeat[] Seats;
        public int SmallBlind = 25, BigBlind = 50;
        public int Dealer = -1, SmallBlindSeat = -1, BigBlindSeat = -1, Actor = -1;
        public PokerStreet Street;
        public int CurrentBet, LastFullRaise, HandNumber, Revision;
        public int LastHandPlayers;
        public long HandTotal;
        public int RemovedThisHand;
        public int[] Deck;
        public int DeckIndex;
        public List<int> Board = new List<int>();
        public List<PotAward> Awards = new List<PotAward>();
        public string Message = "Valmis jakoon";
        public bool Showdown;
        [field: NonSerialized] public event Action Changed;

        public PokerTable() { }
        public PokerTable(int[] stacks, int smallBlind = 25, int bigBlind = 50)
        {
            if (stacks == null || stacks.Length < 2 || stacks.Length > 5 || stacks.Any(x => x < 0)) throw new ArgumentException("2–5 nonnegative stacks required");
            if (smallBlind <= 0 || bigBlind < smallBlind || bigBlind > 100000000) throw new ArgumentException("Invalid blinds");
            if (stacks.Sum(x => (long)x) > int.MaxValue / 2) throw new ArgumentException("Table too large");
            SmallBlind = smallBlind; BigBlind = bigBlind;
            Seats = stacks.Select((x, i) => new PokerSeat { Name = i == 0 ? "Sinä" : "Botti " + i, Stack = x }).ToArray();
        }

        public bool Running => Street != PokerStreet.BetweenHands && Street != PokerStreet.Complete;
        public int Pot => Seats.Sum(x => x.Committed);
        public long Total => Seats.Sum(x => (long)x.Stack + x.Committed);
        int Next(int from, Func<PokerSeat, bool> predicate)
        {
            for (int step = 1; step <= Seats.Length; step++) { int n = (from + step + Seats.Length) % Seats.Length; if (predicate(Seats[n])) return n; }
            return -1;
        }
        static bool Live(PokerSeat s) => s.InHand && !s.Folded;
        static bool CanAct(PokerSeat s) => Live(s) && s.Stack > 0;
        bool NeedsAction(PokerSeat s) => CanAct(s) && (!s.Acted || s.Bet < CurrentBet);

        public void StartHand(Random random, int[] fixedDeck = null)
        {
            if (Running) throw new InvalidOperationException("Hand in progress");
            if (Seats.Count(x => x.Stack > 0) < 2) throw new InvalidOperationException("Need two funded players");
            var nextDeck = fixedDeck != null ? (int[])fixedDeck.Clone() : Enumerable.Range(0, 52).ToArray();
            if (nextDeck.Length != 52 || nextDeck.Distinct().Count() != 52 || nextDeck.Any(c => c < 0 || c >= 52)) throw new ArgumentException("Deck must be a 52-card permutation");
            if (fixedDeck == null && random == null) throw new ArgumentNullException(nameof(random));
            HandTotal = Total; RemovedThisHand = 0; HandNumber++; Board.Clear(); Awards.Clear(); Showdown = false;
            foreach (var s in Seats) { s.InHand = s.Stack > 0; s.Folded = !s.InHand; s.Bet = s.Committed = s.ActedAtBet = 0; s.Acted = false; s.Hole = new[] { -1, -1 }; s.LastAction = ""; }
            bool headsUp = Seats.Count(s => s.InHand) == 2;
            if (headsUp && LastHandPlayers > 2 && BigBlindSeat >= 0)
            {
                // On the transition to heads-up, advance the BB first so no survivor posts it twice.
                int nextBigBlind = Next(BigBlindSeat, s => s.InHand);
                Dealer = Next(nextBigBlind, s => s.InHand);
            }
            else Dealer = Next(Dealer, s => s.InHand);
            LastHandPlayers = Seats.Count(s => s.InHand);
            SmallBlindSeat = headsUp ? Dealer : Next(Dealer, s => s.InHand);
            BigBlindSeat = Next(SmallBlindSeat, s => s.InHand);
            Deck = nextDeck;
            if (fixedDeck == null) for (int i = 51; i > 0; i--) { int j = random.Next(i + 1); int t = Deck[i]; Deck[i] = Deck[j]; Deck[j] = t; }
            DeckIndex = 0;
            for (int round = 0; round < 2; round++)
            {
                int seat = Dealer;
                for (int n = 0; n < Seats.Count(s => s.InHand); n++) { seat = Next(seat, s => s.InHand); Seats[seat].Hole[round] = Draw(); }
            }
            Pay(SmallBlindSeat, Math.Min(SmallBlind, Seats[SmallBlindSeat].Stack));
            Pay(BigBlindSeat, Math.Min(BigBlind, Seats[BigBlindSeat].Stack));
            Seats[SmallBlindSeat].LastAction = "Pieni blindi"; Seats[BigBlindSeat].LastAction = "Iso blindi";
            CurrentBet = BigBlind; LastFullRaise = BigBlind; Street = PokerStreet.Preflop;
            Actor = Next(BigBlindSeat, NeedsAction); Message = "Preflop";
            Progress(BigBlindSeat); Notify();
        }

        int Draw() => Deck[DeckIndex++];
        void Pay(int index, int amount)
        {
            var s = Seats[index];
            if (amount < 0 || amount > s.Stack) throw new InvalidOperationException("Invalid payment");
            s.Stack -= amount; s.Bet += amount; s.Committed += amount;
        }

        public LegalActions Legal(int index)
        {
            var l = new LegalActions();
            if (!Running || Actor != index || index < 0 || !CanAct(Seats[index])) return l;
            var s = Seats[index]; int due = Math.Max(0, CurrentBet - s.Bet);
            bool reopen = !s.Acted || CurrentBet - s.ActedAtBet >= LastFullRaise;
            bool opponent = Seats.Where((x, i) => i != index).Any(CanAct);
            l.Fold = true; l.Check = due == 0; l.Call = due > 0; l.CallAmount = Math.Min(due, s.Stack);
            l.MinimumRaiseTo = checked(CurrentBet + LastFullRaise);
            l.MaximumRaiseTo = checked(s.Bet + s.Stack);
            l.Raise = reopen && opponent && l.MaximumRaiseTo >= l.MinimumRaiseTo;
            l.AllIn = l.MaximumRaiseTo <= CurrentBet || (reopen && opponent);
            return l;
        }

        // expectedRevision rejects stale UI clicks, including a double click crossing an automatic street change.
        public bool Act(int index, PokerAction action, int raiseTo = 0, int expectedRevision = -1)
        {
            if (expectedRevision >= 0 && expectedRevision != Revision) return false;
            var l = Legal(index); var s = index >= 0 && index < Seats.Length ? Seats[index] : null;
            if (s == null) return false;
            int target = s.Bet;
            switch (action)
            {
                case PokerAction.Fold: if (!l.Fold) return false; s.Folded = true; s.LastAction = "Luovutti"; break;
                case PokerAction.Check: if (!l.Check) return false; s.LastAction = "Sökö"; break;
                case PokerAction.Call: if (!l.Call) return false; target += l.CallAmount; s.LastAction = "Maksoi " + l.CallAmount; break;
                case PokerAction.RaiseTo:
                    if (!l.Raise || raiseTo < l.MinimumRaiseTo || raiseTo > l.MaximumRaiseTo) return false;
                    target = raiseTo; s.LastAction = "Korotti " + target; break;
                case PokerAction.AllIn: if (!l.AllIn) return false; target = l.MaximumRaiseTo; s.LastAction = "All-in " + target; break;
                default: return false;
            }
            int before = CurrentBet;
            Pay(index, target - s.Bet);
            if (target > before)
            {
                int increase = target - before;
                bool full = increase >= LastFullRaise;
                if (full) LastFullRaise = increase;
                CurrentBet = target;
            }
            s.Acted = true; s.ActedAtBet = CurrentBet;
            Message = s.Name + " · " + s.LastAction;
            Progress(index); Notify(); return true;
        }

        // Used only when leaving; an all-in hand remains live. No turn is consumed by an out-of-turn fold.
        public void FoldLeaving(int index)
        {
            if (!Running || !Live(Seats[index]) || Seats[index].Stack == 0) return;
            Seats[index].Folded = true; Seats[index].LastAction = "Poistui / luovutti";
            int previous = Actor;
            Progress(previous == index ? index : (previous - 1 + Seats.Length) % Seats.Length);
            Notify();
        }

        void Progress(int after)
        {
            if (Seats.Count(Live) == 1) { Finish(false); return; }
            // With no possible opponent bet, the remaining funded player only acts if facing a call.
            int funded = Seats.Count(CanAct);
            if (funded <= 1)
            {
                CurrentBet = Seats.Where(Live).Max(s => s.Bet);
                int lone = Next(-1, CanAct);
                if (lone >= 0 && Seats[lone].Bet < CurrentBet && Seats.Any(x => Live(x) && x != Seats[lone] && x.Committed > 0)) { Actor = lone; return; }
                Runout(); return;
            }
            Actor = Next(after, NeedsAction);
            if (Actor >= 0) return;
            ReturnUncalled();
            if (Street == PokerStreet.River) { Finish(true); return; }
            AdvanceStreet();
            Actor = Next(Dealer, NeedsAction);
        }

        void AdvanceStreet()
        {
            Draw(); // Burn before flop, turn and river.
            if (Street == PokerStreet.Preflop) { Board.Add(Draw()); Board.Add(Draw()); Board.Add(Draw()); Street = PokerStreet.Flop; }
            else { Board.Add(Draw()); Street = Street == PokerStreet.Flop ? PokerStreet.Turn : PokerStreet.River; }
            foreach (var s in Seats) { s.Bet = 0; s.Acted = false; s.ActedAtBet = 0; }
            CurrentBet = 0; LastFullRaise = BigBlind; Message = Street.ToString();
        }

        void Runout()
        {
            ReturnUncalled();
            while (Board.Count < 5) AdvanceStreet();
            Finish(true);
        }

        void ReturnUncalled()
        {
            int highest = Seats.Max(s => s.Committed);
            var leaders = Seats.Where(s => s.Committed == highest).ToArray();
            if (leaders.Length != 1) return;
            var leader = leaders[0];
            int second = Seats.Where(s => s != leader).Max(s => s.Committed);
            int refund = highest - second;
            leader.Stack += refund; leader.Committed -= refund; leader.Bet = Math.Max(0, leader.Bet - refund);
        }

        void Finish(bool showdown)
        {
            ReturnUncalled(); Showdown = showdown; Awards.Clear();
            int previous = 0;
            var levels = Seats.Where(s => s.Committed > 0).Select(s => s.Committed).Distinct().OrderBy(x => x).ToArray();
            foreach (int level in levels)
            {
                int amount = checked((level - previous) * Seats.Count(s => s.Committed >= level)); previous = level;
                var eligible = Enumerable.Range(0, Seats.Length).Where(i => Live(Seats[i]) && Seats[i].Committed >= level).ToArray();
                if (eligible.Length == 0) throw new InvalidOperationException("Pot without eligible player");
                long best = showdown ? eligible.Max(i => PokerCards.Evaluate(Seats[i].Hole.Concat(Board).ToArray())) : 0;
                int[] winners = eligible.Where(i => !showdown || PokerCards.Evaluate(Seats[i].Hole.Concat(Board).ToArray()) == best).ToArray();
                foreach (int i in winners) Seats[i].Stack += amount / winners.Length;
                int odd = amount % winners.Length;
                for (int step = 1; odd > 0 && step <= Seats.Length; step++)
                {
                    int i = (Dealer + step) % Seats.Length;
                    if (winners.Contains(i)) { Seats[i].Stack++; odd--; }
                }
                Awards.Add(new PotAward { Amount = amount, Winners = winners, Hand = showdown ? PokerCards.HandName(best) : "Muut luovuttivat" });
            }
            foreach (var s in Seats) s.Committed = s.Bet = 0;
            Street = PokerStreet.Complete; Actor = -1;
            Message = WinMessage();
            AssertConserved();
        }

        // Selkeä voittoteksti: sama voittaja ja käsi (pääpotti + sivupotit) yhdistetään yhdeksi riviksi.
        string WinMessage()
        {
            var parts = new List<string>();
            foreach (var g in Awards.GroupBy(a => string.Join(",", a.Winners.OrderBy(i => i)) + "|" + a.Hand))
            {
                var first = g.First(); int amount = g.Sum(a => a.Amount);
                var names = first.Winners.Select(i => Seats[i].Name).ToArray();
                bool you = first.Winners.Length == 1 && first.Winners[0] == 0;
                string who = string.Join(" ja ", names);
                string verb = first.Winners.Length > 1 ? "jakoivat" : you ? "voitit" : "voitti";
                string how = first.Hand == "Muut luovuttivat" ? "muut luovuttivat" : first.Hand;
                parts.Add(who + " " + verb + " " + amount + " mk – " + how);
            }
            return string.Join("  ·  ", parts);
        }

        public void AddChips(int index, int amount)
        {
            if (Running || amount < 0 || Total + amount > int.MaxValue / 2) throw new InvalidOperationException("Rebuy only between hands, within table limit");
            Seats[index].Stack = checked(Seats[index].Stack + amount);
            if (Street == PokerStreet.Complete) HandTotal += amount;
            Notify();
        }

        public int WithdrawStack(int index)
        {
            var s = Seats[index];
            if (Running && Live(s)) throw new InvalidOperationException("Fold before withdrawing a live stack");
            int amount = s.Stack; s.Stack = 0;
            if (Running || Street == PokerStreet.Complete) RemovedThisHand += amount;
            Notify(); return amount;
        }

        public void AssertConserved()
        {
            if (Seats.Any(s => s.Stack < 0 || s.Committed < 0 || s.Bet < 0 || s.Bet > s.Committed)) throw new InvalidOperationException("Negative/inconsistent chips");
            if ((Running || Street == PokerStreet.Complete) && Total + RemovedThisHand != HandTotal) throw new InvalidOperationException("Chips not conserved: " + Total + " + " + RemovedThisHand + " != " + HandTotal);
        }
        void Notify() { Revision++; if (Running) AssertConserved(); Changed?.Invoke(); }
    }
}

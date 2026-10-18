using System;

namespace TotalFist.Poker
{
    // A SINGLE durable snapshot contains wallet + escrow + hand. Committing them together
    // avoids the crash window between a separate wallet credit and its settlement receipt.
    [Serializable]
    public sealed class PokerCashSession
    {
        public int Version = 1;
        public int Wallet;
        public int BuyIn;
        public bool WalletInitialized, Joined, Leaving;
        public string SessionId;
        public PokerTable Table;
        public long Transactions;

        public bool Pending => Joined && Leaving && Table != null && Table.Running;

        public bool Join(int buyIn, int smallBlind, int bigBlind, int liveWallet)
        {
            if (Joined || buyIn <= 0 || liveWallet < buyIn || buyIn > 100000000 || smallBlind <= 0 || bigBlind < smallBlind || bigBlind > 100000000) return false;
            // Validate before touching money.
            var table = new PokerTable(new[] { buyIn, buyIn, buyIn, buyIn, buyIn }, smallBlind, bigBlind);
            string[] names = { "Sinä", "Puliukko", "Yakuza", "Prätkäjätkä", "Rokkimimmi" };
            for (int i = 0; i < names.Length; i++) table.Seats[i].Name = names[i];
            Wallet = liveWallet - buyIn; WalletInitialized = true; Table = table; BuyIn = buyIn;
            Joined = true; Leaving = false; SessionId = Guid.NewGuid().ToString("N"); Transactions++;
            return true;
        }

        public bool Rebuy(int amount)
        {
            if (!Joined || Leaving || Table.Running || amount <= 0 || Wallet < amount || Table.Total + amount > int.MaxValue / 2) return false;
            Table.AddChips(0, amount); Wallet -= amount; Transactions++; return true;
        }

        public void Leave()
        {
            if (!Joined || Leaving) return;
            Leaving = true;
            // An all-in has no unused stack to return; keep its hand alive until showdown.
            if (Table.Running && Table.Seats[0].Stack > 0) Table.FoldLeaving(0);
            if (!Table.Running || Table.Seats[0].Folded) CreditStack();
            CompleteExit(); Transactions++;
        }

        void CreditStack() { Wallet = checked(Wallet + Table.WithdrawStack(0)); }

        public void CompleteExit()
        {
            if (!Joined || !Leaving || Table.Running) return;
            CreditStack(); Joined = false; Transactions++;
        }

        public void StartHand(Random random)
        {
            if (!Joined || Leaving || Table.Running || Table.Seats[0].Stack <= 0) throw new InvalidOperationException("Cannot deal");
            // House-funded bots top up ONLY between hands. Their inflow is outside each hand's invariant.
            for (int i = 1; i < Table.Seats.Length; i++) if (Table.Seats[i].Stack == 0) Table.AddChips(i, Math.Max(BuyIn, Table.BigBlind));
            Table.StartHand(random);
        }

        public bool Act(int seat, PokerAction action, int raiseTo = 0, int revision = -1)
        {
            if (!Joined || Table == null || (Leaving && seat == 0)) return false;
            bool accepted = Table.Act(seat, action, raiseTo, revision);
            if (accepted) CompleteExit();
            return accepted;
        }
    }
}

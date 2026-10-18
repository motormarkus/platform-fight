using System;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;
using TotalFist.Poker;

public class PokerCoreTests
{
    static int C(int rank, int suit = 0) => suit * 13 + rank - 2;
    static int[] Rig(params int[] prefix) => prefix.Concat(Enumerable.Range(0, 52).Where(c => !prefix.Contains(c))).ToArray();
    static void Passive(PokerTable t)
    {
        int guard = 200;
        while (t.Running && guard-- > 0)
        {
            int seat = t.Actor; var l = t.Legal(seat);
            Assert.That(t.Act(seat, l.Check ? PokerAction.Check : PokerAction.Call), Is.True);
            t.AssertConserved();
        }
        Assert.That(guard, Is.GreaterThan(0));
    }

    [Test] public void PairKickersAndSevenCardBestHand()
    {
        long ace = PokerCards.Evaluate(new[] { C(8), C(8, 1), C(14), C(13), C(9), C(3, 2), C(2, 3) });
        long king = PokerCards.Evaluate(new[] { C(8), C(8, 1), C(12), C(13), C(9), C(3, 2), C(2, 3) });
        Assert.That(ace, Is.GreaterThan(king));
        Assert.That(PokerCards.HandName(ace), Is.EqualTo("Pari"));
        long full = PokerCards.Evaluate(new[] { C(14), C(14, 1), C(14, 2), C(13), C(13, 1), C(13, 2), C(2) });
        Assert.That(PokerCards.HandName(full), Is.EqualTo("Täyskäsi"));
    }

    [Test] public void EveryCategoryAndAllTieBreakers()
    {
        int[][] hands = {
            new[]{C(14),C(11,1),C(9,2),C(6,3),C(3)},
            new[]{C(14),C(14,1),C(9,2),C(6,3),C(3)},
            new[]{C(14),C(14,1),C(9,2),C(9,3),C(3)},
            new[]{C(14),C(14,1),C(14,2),C(6,3),C(3)},
            new[]{C(9),C(8,1),C(7,2),C(6,3),C(5)},
            new[]{C(14),C(11),C(9),C(6),C(3)},
            new[]{C(14),C(14,1),C(14,2),C(6,3),C(6)},
            new[]{C(14),C(14,1),C(14,2),C(14,3),C(3)},
            new[]{C(9),C(8),C(7),C(6),C(5)} };
        for(int i=1;i<hands.Length;i++) Assert.That(PokerCards.Evaluate(hands[i]),Is.GreaterThan(PokerCards.Evaluate(hands[i-1])));
        Assert.That(PokerCards.Evaluate(new[]{C(9),C(9,1),C(8),C(8,1),C(14,2)}),Is.GreaterThan(PokerCards.Evaluate(new[]{C(9),C(9,1),C(8),C(8,1),C(13,2)})));
        Assert.Throws<ArgumentException>(()=>PokerCards.Evaluate(new[]{0,0,1,2,3}));
    }

    [Test] public void AceLowStraightLosesToSixHigh()
    {
        long wheel = PokerCards.Evaluate(new[] { C(14), C(2, 1), C(3, 2), C(4, 3), C(5), C(12), C(11) });
        long six = PokerCards.Evaluate(new[] { C(6), C(2, 1), C(3, 2), C(4, 3), C(5) });
        Assert.That(PokerCards.HandName(wheel), Is.EqualTo("Suora")); Assert.That(six, Is.GreaterThan(wheel));
    }

    [Test] public void UnequalAllInsProduceMainSidePotAndUncalledRefund()
    {
        var t = new PokerTable(new[] { 300, 100, 200 });
        t.StartHand(new Random(1), Rig(C(14), C(13), C(12), C(14, 1), C(13, 1), C(12, 1), C(3), C(2,2), C(5,3), C(8,2), C(4), C(9,3), C(6), C(11,2)));
        Assert.That(t.Act(0, PokerAction.AllIn), Is.True);
        Assert.That(t.Act(1, PokerAction.Call), Is.True);
        Assert.That(t.Act(2, PokerAction.Call), Is.True);
        Assert.That(t.Seats.Select(s => s.Stack), Is.EqualTo(new[] { 100, 300, 200 }));
        Assert.That(t.Awards.Select(a => a.Amount), Is.EqualTo(new[] { 300, 200 })); t.AssertConserved();
    }

    [Test] public void SplitPotOddChipGoesLeftOfDealer()
    {
        var t = new PokerTable(new[] { 200, 200, 200 });
        t.StartHand(new Random(2), Rig(C(2), C(3), C(4), C(5), C(6), C(7), C(8), C(10,3), C(11,3), C(12,3), C(9), C(13,3), C(2,1), C(14,3)));
        t.Act(0, PokerAction.Call); t.Act(1, PokerAction.Fold); t.Act(2, PokerAction.Check); Passive(t);
        Assert.That(t.Seats.Select(s => s.Stack), Is.EqualTo(new[] { 212, 175, 213 })); t.AssertConserved();
    }

    [Test] public void MinimumRaisesAndStaleActions()
    {
        var t = new PokerTable(new[] { 2000, 2000, 2000, 2000, 2000 }); t.StartHand(new Random(3));
        int seat = t.Actor, rev = t.Revision;
        Assert.That(t.Legal(seat).MinimumRaiseTo, Is.EqualTo(100));
        Assert.That(t.Act(seat, PokerAction.RaiseTo, 99), Is.False);
        Assert.That(t.Act(seat, PokerAction.RaiseTo, 150, rev), Is.True);
        Assert.That(t.Legal(t.Actor).MinimumRaiseTo, Is.EqualTo(250));
        Assert.That(t.Act(t.Actor, PokerAction.Call, 0, rev), Is.False);
    }

    [Test] public void ShortAllInDoesNotReopenButUnactedPlayerMayRaise()
    {
        // Dealer 0; first actor 3 raises, 4 shoves a short raise, 0 has not acted yet.
        var t = new PokerTable(new[] { 1000, 1000, 1000, 1000, 150 }); t.StartHand(new Random(4));
        t.Act(3, PokerAction.RaiseTo, 100); t.Act(4, PokerAction.AllIn);
        Assert.That(t.Legal(0).Raise, Is.True); // 150-100 = 50: actually a full raise.
        // Real short raise: raise to 200 (increment 150), shove 250 (increment 50).
        t = new PokerTable(new[] { 1000, 1000, 1000, 1000, 250 }); t.StartHand(new Random(5));
        t.Act(3, PokerAction.RaiseTo, 200); t.Act(4, PokerAction.AllIn);
        Assert.That(t.Legal(0).Raise, Is.True);
        t.Act(0, PokerAction.Call); t.Act(1, PokerAction.Call); t.Act(2, PokerAction.Call);
        var l = t.Legal(3); Assert.That(l.CallAmount, Is.EqualTo(50)); Assert.That(l.Raise, Is.False); Assert.That(l.AllIn, Is.False);
    }

    [Test] public void CumulativeShortRaisesReopenWhenFullIncrementReached()
    {
        var t = new PokerTable(new[] { 300, 1000, 1000, 1000, 250 }); t.StartHand(new Random(6));
        t.Act(3, PokerAction.RaiseTo, 150); // Last full increment 100.
        // 250 is a full raise; instead use two short raises to 200 and 250.
        t = new PokerTable(new[] { 250, 1000, 1000, 1000, 200 }); t.StartHand(new Random(6));
        t.Act(3, PokerAction.RaiseTo, 150); t.Act(4, PokerAction.AllIn); t.Act(0, PokerAction.AllIn);
        t.Act(1, PokerAction.Call); t.Act(2, PokerAction.Call);
        Assert.That(t.Actor, Is.EqualTo(3)); Assert.That(t.Legal(3).Raise, Is.True); Assert.That(t.Legal(3).MinimumRaiseTo, Is.EqualTo(350));
    }

    [Test] public void HeadsUpDealerPostsSmallBlindAndActsFirstOnlyPreflop()
    {
        var t = new PokerTable(new[] { 2000, 2000 }); t.StartHand(new Random(7));
        Assert.That(t.Dealer, Is.EqualTo(t.SmallBlindSeat)); Assert.That(t.Actor, Is.EqualTo(t.Dealer));
        t.Act(t.Actor, PokerAction.Call); t.Act(t.Actor, PokerAction.Check);
        Assert.That(t.Street, Is.EqualTo(PokerStreet.Flop)); Assert.That(t.Actor, Is.EqualTo(t.BigBlindSeat));
        Passive(t); int last = t.Dealer; t.StartHand(new Random(8)); Assert.That(t.Dealer, Is.Not.EqualTo(last));
    }

    [Test] public void ShortBigBlindAndZeroStacksRunOutWithoutPhantomCall()
    {
        var t = new PokerTable(new[] { 200, 10, 0 }); t.StartHand(new Random(9));
        Assert.That(t.Street, Is.EqualTo(PokerStreet.Complete)); Assert.That(t.Total, Is.EqualTo(210));
        Assert.That(t.Seats[2].InHand, Is.False); t.AssertConserved();
    }

    [Test] public void TransitionToHeadsUpNeverRepeatsSurvivingBigBlind()
    {
        var t = new PokerTable(new[]{2000,2000,2000}); t.StartHand(new Random(25)); Passive(t);
        Assert.That(t.BigBlindSeat,Is.EqualTo(2)); t.WithdrawStack(0); t.StartHand(new Random(26));
        Assert.That(t.Dealer,Is.EqualTo(2)); Assert.That(t.SmallBlindSeat,Is.EqualTo(2)); Assert.That(t.BigBlindSeat,Is.EqualTo(1));
        t = new PokerTable(new[]{2000,2000,2000}); t.StartHand(new Random(25)); Passive(t);
        t.WithdrawStack(2); t.StartHand(new Random(26)); Assert.That(t.BigBlindSeat,Is.EqualTo(0));
    }

    [Test] public void WholeHandAndAllOthersFoldConserveChips()
    {
        var t = new PokerTable(new[] { 2000, 2000, 2000, 2000, 2000 }); t.StartHand(new Random(10)); Passive(t);
        Assert.That(t.Board.Count, Is.EqualTo(5)); Assert.That(t.Showdown, Is.True);
        t.StartHand(new Random(11));
        while (t.Running) { t.Act(t.Actor, PokerAction.Fold); t.AssertConserved(); }
        Assert.That(t.Showdown, Is.False); Assert.That(t.Total, Is.EqualTo(10000));
    }

    [Test] public void LeaveOutsideHandReturnsBuyInExactlyOnce()
    {
        var s = new PokerCashSession(); Assert.That(s.Join(2000, 25, 50, 5000), Is.True);
        Assert.That(s.Join(2000, 25, 50, s.Wallet), Is.False);
        s.Leave(); s.Leave(); s.CompleteExit(); Assert.That(s.Wallet, Is.EqualTo(5000)); Assert.That(s.Joined, Is.False);
    }

    [Test] public void MidHandExitFoldsAndReturnsOnlyUnusedChips()
    {
        var s = new PokerCashSession(); s.Join(2000, 25, 50, 5000); s.StartHand(new Random(12));
        s.Act(3, PokerAction.Call); s.Act(4, PokerAction.Call); s.Act(0, PokerAction.Call);
        s.Leave(); Assert.That(s.Table.Seats[0].Folded, Is.True); Assert.That(s.Wallet, Is.EqualTo(4950));
        Passive(s.Table); s.CompleteExit(); s.CompleteExit(); s.Leave(); Assert.That(s.Wallet, Is.EqualTo(4950)); s.Table.AssertConserved();
    }

    [Test] public void AllInExitPreservesHandAndSettlesExactlyOnce()
    {
        var s = new PokerCashSession(); s.Join(2000, 25, 50, 5000); s.StartHand(new Random(13));
        s.Act(3, PokerAction.Call); s.Act(4, PokerAction.Call); s.Act(0, PokerAction.AllIn);
        s.Leave(); Assert.That(s.Pending, Is.True); Assert.That(s.Table.Seats[0].Folded, Is.False);
        Passive(s.Table); int won = s.Table.Seats[0].Stack;
        s.CompleteExit(); s.CompleteExit(); s.Leave(); Assert.That(s.Wallet, Is.EqualTo(3000 + won)); Assert.That(s.Joined, Is.False);
    }

    [Test] public void RebuyAndBotRefillOnlyBetweenHands()
    {
        var s = new PokerCashSession(); s.Join(2000, 25, 50, 6000); Assert.That(s.Rebuy(1000), Is.True);
        s.StartHand(new Random(14)); Assert.That(s.Rebuy(1000), Is.False); Assert.Throws<InvalidOperationException>(() => s.Table.AddChips(1, 1));
        Passive(s.Table); Assert.That(s.Rebuy(100), Is.True);
    }

    [Test] public void BotObservationsContainOnlyPublicDataAndOwnCards()
    {
        var t = new PokerTable(new[] { 2000, 2000, 2000, 2000, 2000 }); t.StartHand(new Random(15));
        int actor = t.Actor; var v = PokerBot.Observe(t, actor);
        Assert.That(v.GetType().GetFields().Any(f => f.FieldType == typeof(PokerTable)), Is.False);
        var a = PokerBot.Decide(v, PokerStyle.Balanced, new Random(1));
        Array.Reverse(t.Deck); for (int i = 0; i < t.Seats.Length; i++) if (i != actor) t.Seats[i].Hole = new[] { 50, 51 };
        var b = PokerBot.Decide(PokerBot.Observe(t, actor), PokerStyle.Balanced, new Random(1));
        Assert.That(b.Action, Is.EqualTo(a.Action)); Assert.That(b.RaiseTo, Is.EqualTo(a.RaiseTo));
    }

    [Test] public void RandomLegalHandsNeverLoseOrCreateChips()
    {
        var rng = new Random(901);
        for (int hand = 0; hand < 200; hand++)
        {
            var t = new PokerTable(Enumerable.Range(0, 5).Select(_ => rng.Next(1, 3000)).ToArray()); t.StartHand(rng);
            int guard = 1000;
            while (t.Running && guard-- > 0)
            {
                var l = t.Legal(t.Actor); int roll = rng.Next(10);
                var action = roll == 0 ? PokerAction.Fold : roll < 3 && l.AllIn ? PokerAction.AllIn : roll < 5 && l.Raise ? PokerAction.RaiseTo : l.Check ? PokerAction.Check : PokerAction.Call;
                Assert.That(t.Act(t.Actor, action, l.MinimumRaiseTo), Is.True); t.AssertConserved();
            }
            Assert.That(guard, Is.GreaterThan(0)); t.AssertConserved();
        }
    }

    [Test] public void ShortOpeningAllInRequiresFullRaiseAboveActualBet()
    {
        var t = new PokerTable(new[] { 1000, 1000, 70 }); t.StartHand(new Random(20));
        t.Act(0, PokerAction.Call); t.Act(1, PokerAction.Call); t.Act(2, PokerAction.Check);
        Assert.That(t.Actor, Is.EqualTo(1)); t.Act(1, PokerAction.Check); t.Act(2, PokerAction.AllIn); // 20 chips left.
        Assert.That(t.Legal(0).MinimumRaiseTo, Is.EqualTo(70));
        Assert.That(t.Act(0, PokerAction.RaiseTo, 50), Is.False);
        t.Act(0, PokerAction.Call);
        Assert.That(t.Legal(1).Raise, Is.False); Assert.That(t.Legal(1).CallAmount, Is.EqualTo(20));
        Passive(t);
    }

    static readonly XmlSerializer Serializer = new XmlSerializer(typeof(PokerCashSession));
    static string Encode(PokerCashSession s) { using(var w = new StringWriter()) { Serializer.Serialize(w,s); return w.ToString(); } }
    static PokerCashSession Decode(string s) { using(var r = new StringReader(s)) return (PokerCashSession)Serializer.Deserialize(r); }

    [Test] public void DurableAllInRecoverySurvivesViewSceneAndRepeatedReload()
    {
        string folder = Path.Combine(Path.GetTempPath(), "TotalFistPokerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder,"checkpoint.xml");
        var store = new PokerCheckpointStore<PokerCashSession>(file, Encode, Decode);
        var s = new PokerCashSession(); s.Join(2000,25,50,5000); s.StartHand(new Random(21));
        s.Act(3,PokerAction.Call); s.Act(4,PokerAction.Call); s.Act(0,PokerAction.AllIn); store.Write(s);
        // Simulate loss of the entire Unity owner/view; restore from disk only.
        s = store.Read(); s.Leave(); store.Write(s);
        s = store.Read(); Assert.That(s.Table.Seats[0].Folded,Is.False); Passive(s.Table);
        int prize=s.Table.Seats[0].Stack; s.CompleteExit(); store.Write(s);
        s = store.Read(); s.CompleteExit(); s.Leave(); store.Write(s); s = store.Read();
        Assert.That(s.Wallet,Is.EqualTo(3000+prize)); Assert.That(s.Joined,Is.False);
        Assert.That(File.Exists(file+".bak"),Is.True);
    }

    [Test] public void FailedSerializationKeepsPreviousCommittedWallet()
    {
        string file=Path.Combine(Path.GetTempPath(),"TotalFistPokerTests",Guid.NewGuid().ToString("N"),"checkpoint.xml");
        var good=new PokerCheckpointStore<PokerCashSession>(file,Encode,Decode);
        var s=new PokerCashSession(); s.Join(2000,25,50,5000); good.Write(s);
        s.Leave(); var broken=new PokerCheckpointStore<PokerCashSession>(file,_=>throw new IOException("simulated write failure"),Decode);
        Assert.Throws<IOException>(()=>broken.Write(s)); var restored=good.Read();
        Assert.That(restored.Joined,Is.True); Assert.That(restored.Wallet,Is.EqualTo(3000));
        restored.Leave(); restored.Leave(); good.Write(restored); Assert.That(good.Read().Wallet,Is.EqualTo(5000));
    }

    [Test] public void AllFourBotStylesPlayCompleteLegalHands()
    {
        var rng=new Random(22);
        for(int n=0;n<12;n++)
        {
            var t=new PokerTable(new[]{2000,2000,2000,2000,2000}); t.StartHand(rng); int guard=400;
            while(t.Running && guard-->0)
            {
                int actor=t.Actor; var d=PokerBot.Decide(PokerBot.Observe(t,actor),(PokerStyle)(actor%4),rng,8);
                Assert.That(t.Act(actor,d.Action,d.RaiseTo),Is.True); t.AssertConserved();
            }
            Assert.That(guard,Is.GreaterThan(0));
        }
    }
}

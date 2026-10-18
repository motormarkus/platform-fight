using System;
using System.Reflection;

// Runs the SAME NUnit test methods as Unity Test Runner, without launching Unity.
static class PokerTestRunner
{
    static int Main()
    {
        int passed = 0, failed = 0;
        foreach (var method in typeof(PokerCoreTests).GetMethods())
        {
            if (!Attribute.IsDefined(method, typeof(NUnit.Framework.TestAttribute))) continue;
            try { method.Invoke(new PokerCoreTests(), null); Console.WriteLine("PASS " + method.Name); passed++; }
            catch (Exception e) { Console.WriteLine("FAIL " + method.Name + "\n" + (e.InnerException ?? e)); failed++; }
        }
        Console.WriteLine(passed + " passed, " + failed + " failed"); return failed == 0 ? 0 : 1;
    }
}

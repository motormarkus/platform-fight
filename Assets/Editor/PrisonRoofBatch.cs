public static partial class BeatEmUpSetup
{
    public static void RunPrisonRoofBatch() { batch = true; try { Step("372 vankilan katto", AddPrisonRoof); } finally { batch = false; } }
}

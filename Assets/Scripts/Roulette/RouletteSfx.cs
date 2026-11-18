using UnityEngine;

namespace TotalFist.Roulette
{
    /// <summary>Ruletin äänet: pelimerkki käyttää pokerin ChipClack-ääntä (Resources/Poker/Sfx), loput syntetisoidaan koodilla (ei äänitiedostoja):
    /// pyörän huminan ja pallon vierimisen silmukat, metalliset kilahdukset kolon reunoilla, kolahdus kolossa, voittofanfaari ja tappiosävel.</summary>
    public static class RouletteSfx
    {
        public static AudioClip Chip, Whirr, Roll, Tink, Clunk, Whoosh, WinSmall, WinBig, Lose;
        const int SR = 44100;
        static System.Random rnd = new System.Random(11);
        static float Noise() { return (float)(rnd.NextDouble() * 2.0 - 1.0); }
        static AudioClip Make(string name, float dur, System.Func<float, float, float> f)
        {
            int n = Mathf.RoundToInt(dur * SR); var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)SR; d[i] = Mathf.Clamp(f(t, t / dur), -1f, 1f); }
            var c = AudioClip.Create(name, n, 1, SR, false); c.SetData(d, 0); return c;
        }
        public static void Build()
        {
            if (Tink != null) return;
            Chip = Resources.Load<AudioClip>("Poker/Sfx/ChipClack");
            // pyörän hurina: harmoniset siniaallot kokonaisluku-Hz:llä (1 s jakso => saumaton silmukka) + hidas värinä (7 Hz)
            Whirr = Make("rl_whirr", 1f, (t, u) =>
            {
                float s = Mathf.Sin(2 * Mathf.PI * 55f * t) * .5f + Mathf.Sin(2 * Mathf.PI * 110f * t) * .35f + Mathf.Sin(2 * Mathf.PI * 165f * t) * .18f + Mathf.Sin(2 * Mathf.PI * 330f * t) * .06f;
                return s * (.75f + .25f * Mathf.Sin(2 * Mathf.PI * 7f * t)) * .5f;
            });
            // pallon vieriminen: alipäästetty kohina, jonka loppu sulautetaan alkuun (saumaton)
            int n = (int)(1.2f * SR), fade = (int)(.2f * SR), len = n - fade; var raw = new float[n]; float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++) { lp += (Noise() - lp) * .12f; lp2 += (lp - lp2) * .35f; float am = .7f + .3f * Mathf.Sin(i / (float)SR * 2 * Mathf.PI * 23f); raw[i] = lp2 * am * 2.4f; }
            var loop = new float[len];
            for (int i = 0; i < len; i++) { float v = raw[i]; if (i < fade) { float k = i / (float)fade; v = raw[i] * k + raw[len + i] * (1f - k); } loop[i] = Mathf.Clamp(v, -1f, 1f); }
            Roll = AudioClip.Create("rl_roll", len, 1, SR, false); Roll.SetData(loop, 0);
            // metallinen kilahdus: kaksi epäharmonista sävyä
            Tink = Make("rl_tink", .35f, (t, u) => (Mathf.Sin(2 * Mathf.PI * 2350f * t) * .5f + Mathf.Sin(2 * Mathf.PI * 3720f * t) * .3f + Mathf.Sin(2 * Mathf.PI * 1480f * t) * .25f) * Mathf.Exp(-t * 16f) * .6f);
            // kolahdus kolossa: matala nuotti + napsaus
            float pc = 0f;
            Clunk = Make("rl_clunk", .3f, (t, u) => { pc += 2 * Mathf.PI * Mathf.Lerp(210f, 95f, Mathf.Min(1f, t * 6f)) / SR; return (Mathf.Sin(pc) * .6f + Noise() * .25f * Mathf.Exp(-t * 90f)) * Mathf.Exp(-t * 14f); });
            // pyöräytyksen vihellys
            float lw = 0f;
            Whoosh = Make("rl_whoosh", .5f, (t, u) => { lw += (Noise() - lw) * Mathf.Lerp(.04f, .35f, u); return lw * 1.4f * Mathf.Sin(u * Mathf.PI) * .8f; });
            WinSmall = Fanfare("rl_win_small", new[] { 523f, 659f, 784f }, .12f);
            WinBig = Fanfare("rl_win_big", new[] { 523f, 659f, 784f, 1046f, 784f, 1046f, 1318f }, .11f);
            float pl = 0f;
            Lose = Make("rl_lose", .7f, (t, u) => { float f = t < .22f ? 330f : 247f; pl += 2 * Mathf.PI * f / SR; return (Mathf.Sin(pl) * .5f + Mathf.Sin(pl * .5f) * .25f) * Mathf.Exp(-(t < .22f ? t : t - .22f) * 5f) * .55f; });
        }
        // nopea nuottisarja: kolmioaalto + sini, pehmeä alku ja häntä
        static AudioClip Fanfare(string name, float[] notes, float step)
        {
            float dur = notes.Length * step + .5f; float ph = 0f;
            return Make(name, dur, (t, u) =>
            {
                int idx = Mathf.Min(notes.Length - 1, (int)(t / step)); float lt = t - idx * step;
                ph += 2 * Mathf.PI * notes[idx] / SR;
                float tri = Mathf.Asin(Mathf.Sin(ph)) * (2f / Mathf.PI);
                float env = Mathf.Min(1f, lt * 120f) * (idx == notes.Length - 1 ? Mathf.Exp(-lt * 4f) : Mathf.Exp(-lt * 6f) * .8f + .2f);
                return (tri * .35f + Mathf.Sin(ph * 2f) * .1f) * env;
            });
        }
    }
}

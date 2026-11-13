"""Vankilan sisäpihan kuvat käyttäjän kuvista (vain peilaus, rajaus ja siirto; uutta kuvaa ei piirretä).
Lähteet (Tools/PrisonAssets/alkuperaiset/): portti.png (vankilan portti ulkoa), terminaali.png (terminaalin suu-aukko), piha.png (vankilapihan taka-parallaksi).
Tuottaa Assets/Sprites/Taustat/:
  vankila_sisa_1.png   portti peilattuna (sisäpihan vasen pää); tien pinta siirretty samalle riville kuin terminaalikuvassa
  vankila_sisa_2.png, vankila_sisa_3.png   tietä: terminaalikuvan vasen tyhjä osa peilattuina paloina (saumat täsmäävät portin ja terminaalin reunoihin)
  vankila_sisa_4.png   terminaalin suu-aukko
  vankila_piha_taka.png   pihan taka-parallaksi, kaksinkertaisena (kuva + peilikuva), jotta parallaksi ehtii liukua koko pihan ajan
Aja: python Tools/PrisonAssets/make_prison_tiles.py
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "alkuperaiset")
OUT = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Taustat")
CROP = 800   # terminaalikuvan vasemmasta reunasta tyhjää tietä (seinä alkaa noin 812 px:ssä)


def load(name):
    return Image.open(os.path.join(SRC, name)).convert("RGBA")


def ground_row(im, x=100):
    a = im.split()[3]
    for y in range(im.size[1]):
        if a.getpixel((x, y)) > 128:
            return y
    return im.size[1]


def flip(im):
    return im.transpose(Image.FLIP_LEFT_RIGHT)


def main():
    gate, term, yard = load("portti.png"), load("terminaali.png"), load("piha.png")
    W, H = term.size
    assert gate.size == (W, H), (gate.size, term.size)
    gg, tg = ground_row(gate), ground_row(term)
    shift = tg - gg
    print("tien pinta: portti", gg, "terminaali", tg, "siirto", shift)

    # 1) peilattu portti, tie samalle riville kuin terminaalissa (siirretään alas; alareuna leikkautuu, yläreuna täyttyy läpinäkyväksi)
    g = flip(gate)
    t1 = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    t1.alpha_composite(g, (0, shift))
    t1.save(os.path.join(OUT, "vankila_sisa_1.png"))

    # 2) tietä: kaksi ruutua, neljä palaa (suora, peilattu, suora, peilattu); ensimmäisen vasen reuna = portin reuna, viimeisen oikea reuna = terminaalin reuna
    piece = term.crop((0, 0, CROP, H)).resize((W // 2, H), Image.LANCZOS)
    seq = [piece, flip(piece), piece, flip(piece)]
    for n in (0, 1):
        t = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        t.alpha_composite(seq[2 * n], (0, 0))
        t.alpha_composite(seq[2 * n + 1], (W // 2, 0))
        t.save(os.path.join(OUT, "vankila_sisa_%d.png" % (n + 2)))

    # 3) terminaalin suu
    term.save(os.path.join(OUT, "vankila_sisa_4.png"))

    # 4) pihan taka-parallaksi kahtena kappaleena (toinen peilattuna)
    yw, yh = yard.size
    wide = Image.new("RGBA", (yw * 2, yh), (0, 0, 0, 0))
    wide.alpha_composite(yard, (0, 0))
    wide.alpha_composite(flip(yard), (yw, 0))
    wide.save(os.path.join(OUT, "vankila_piha_taka.png"))
    # 5) vankilan käytävä (D-osasto): alkuperäinen pienennettynä 2560 px leveäksi + peilikuva (kaksi kuvaa = ensimmäinen osio)
    cor = load("kaytava_d1.png")
    cw = 2560
    cor = cor.resize((cw, round(cor.size[1] * cw / cor.size[0])), Image.LANCZOS)
    cor.save(os.path.join(OUT, "vankila_kaytava_d1.png"))
    flip(cor).save(os.path.join(OUT, "vankila_kaytava_d1b.png"))
    # 5b) E-osasto: sama käytävä punaisella viivalla (kaytava_e1.png = D-osaston kuva, viiva vihreäksi)
    ce = load("kaytava_e1.png")
    ce = ce.resize((cw, round(ce.size[1] * cw / ce.size[0])), Image.LANCZOS)
    ce.save(os.path.join(OUT, "vankila_kaytava_e1.png"))
    flip(ce).save(os.path.join(OUT, "vankila_kaytava_e1b.png"))
    # 6) sellin sisäkuva (ovi oikealla) 2560 px leveäksi + peilikuva (ovi vasemmalla)
    cell = load("selli.png")
    cell = cell.resize((cw, round(cell.size[1] * cw / cell.size[0])), Image.LANCZOS)
    cell.save(os.path.join(OUT, "vankila_selli.png"))
    flip(cell).save(os.path.join(OUT, "vankila_selli_b.png"))
    # 7) työpaja: kaksi kuvaa peräkkäin (vasen: kaksoisovi + koneet, oikea: jatko + kaksoisovi), kumpikin 2560 px leveäksi
    for i in (1, 2):
        tp = load("tyopaja_%d.png" % i)
        tp = tp.resize((cw, round(tp.size[1] * cw / tp.size[0])), Image.LANCZOS)
        tp.save(os.path.join(OUT, "vankila_tyopaja_%d.png" % i))
    # 8) ruokala: kaksi kuvaa peräkkäin (vasen: kaksoisovi + keittiö, oikea: keittiön jatko + kaksoisovi), kumpikin 2560 px leveäksi
    for i in (1, 2):
        rk = load("ruokala_%d.png" % i)
        rk = rk.resize((cw, round(rk.size[1] * cw / rk.size[0])), Image.LANCZOS)
        rk.save(os.path.join(OUT, "vankila_ruokala_%d.png" % i))
    # 9) kuntopiha: kolme kuvaa peräkkäin (vasen: kaksoisovi, kuntoilulaitteet, koripallokori; keski: leuanvetotanko; oikea: katsomo, kyykkyteline, kaksoisovi).
    #    Kuvat menevät noin 120 px (2000 px:n mittakaavassa) päällekkäin: keski- ja oikean kuvan vasen reuna leikataan.
    #    Taivas on kuvissa jo läpinäkyvä (taivasparallaksi näkyy läpi). Kuvien korkeudet poikkeavat: kiviaidan reuna tasataan, lattiaa venytetään.
    def ledge_top(im, x):
        a = im.split()[3].load()
        for y in range(im.size[1]):
            if a[x, y] > 200: return y
        return 0
    TH = 722   # kohdekorkeus 2000 px:n mittakaavassa (= kuva 1)
    raw = [Image.open(os.path.join(SRC, "piha_kunto_%d.png" % i)).convert("RGBA") for i in (1, 2, 3)]
    ledge = [ledge_top(raw[0], 1000), ledge_top(raw[1], 800), ledge_top(raw[2], 1060)]
    print("kiviaidan ylareuna:", ledge)
    for i, im in enumerate(raw):
        w, h = im.size
        if i > 0:
            cut = 152 if i == 1 else 120   # kuva 1 -> 2 päällekkäisyys mitattu 152 px (kuva 2 -> 3: 121 px); 120:lla sauma heitti 32 px
            im = im.crop((cut, 0, w, h)); w -= cut
        dy = ledge[0] - ledge[i]          # + = tyhjää ylös, - = leikataan ylhäältä
        body = Image.new("RGBA", (w, TH), (0, 0, 0, 0))
        if dy >= 0:
            body.paste(im, (0, dy)); used = min(TH, dy + h)
        else:
            body.paste(im.crop((0, -dy, w, h)), (0, 0)); used = min(TH, h + dy)
        if used < TH:   # lattian alaosa venytetään täyteen korkeuteen
            strip = im.crop((0, h - 100, w, h)).resize((w, 100 + (TH - used)), Image.LANCZOS)
            body.paste(strip, (0, used - 100))
        k = body.resize((round(w * 2560 / 2000), 922), Image.LANCZOS)
        k.save(os.path.join(OUT, "vankila_piha_%d.png" % (i + 1)))
        print("vankila_piha_%d" % (i + 1), k.size)
    # 10) henkilökunnan tilat: kaksi kuvaa peräkkäin (vasen: kaksoisovi pihalta, portaat, keittiö; oikea: pukuhuone, Wardenin puuovet, portaat, hissi).
    #     Oikean kuvan vasen reuna (130 px) leikataan päällekkäisyyden vuoksi; kulkusillan etureuna tasataan ja lattiaa venytetään.
    TH2 = 722
    hl = Image.open(os.path.join(SRC, "henkilokunta_1.png")).convert("RGBA")
    hr = Image.open(os.path.join(SRC, "henkilokunta_2.png")).convert("RGBA")
    hl.resize((2560, 922), Image.LANCZOS).save(os.path.join(OUT, "vankila_henkilokunta_1.png"))
    # hissin levennys: kehys + ovet (x 1709–1880) venytetään EXTRA px leveämmäksi, valaisin pidetään alkuperäisen kokoisena keskellä,
    # kehyksen oikealla puolella oleva (painike, seinä) siirtyy oikealle
    EXTRA = 100
    RX0, RX1, RTOP = 1709, 1880, 386
    ow, oh = hr.size
    rw = RX1 - RX0
    wide = Image.new("RGBA", (ow + EXTRA, oh), (0, 0, 0, 0))
    wide.paste(hr.crop((0, 0, RX0, oh)), (0, 0))
    wide.paste(hr.crop((RX1, 0, ow, oh)), (RX1 + EXTRA, 0))
    lower = hr.crop((RX0, RTOP, RX1, oh)).resize((rw + EXTRA, oh - RTOP), Image.LANCZOS)
    clean = hr.copy()
    lx0, lx1 = 1745, 1835                        # valaisimen ruutu
    from PIL import ImageFilter as _IF
    def soft(w_, h_, inset=7):
        m = Image.new("L", (w_, h_), 0); m.paste(255, (inset, inset, w_ - inset, h_ - inset)); return m.filter(_IF.GaussianBlur(inset / 2.0))
    pw, ph = lx1 - lx0 + 24, 44
    clean.paste(hr.crop((1712, 334, 1744, 372)).resize((pw, ph), Image.LANCZOS), (lx0 - 12, 330), soft(pw, ph))   # seinää valaisimen paikalle (valaisimen vasemmalta puolelta, pehmeä reuna)
    for k in range(3): clean.paste(hr.crop((1725, 372, 1755, 387)), (1760 + 30 * k, 372))   # putki jatkuu
    upper = clean.crop((RX0, 0, RX1, RTOP)).resize((rw + EXTRA, RTOP), Image.LANCZOS)
    wide.paste(upper, (RX0, 0)); wide.paste(lower, (RX0, RTOP))
    lamp = hr.crop((1760, 349, 1820, 387))
    newcx = RX0 + (rw + EXTRA) / 2
    wide.paste(lamp, (round(newcx - (1790 - 1760)), 349), soft(lamp.size[0], lamp.size[1], 5))
    hr = wide
    hr = hr.crop((130, 0, hr.size[0], hr.size[1])); w, h = hr.size
    # oikea kuva on pystysuunnassa hieman erimittainen: keltaiset viivat (162/494) ja lattian alareuna sovitetaan vasemman kuvan riveihin (175/516/722)
    body = Image.new("RGBA", (w, TH2), (0, 0, 0, 0))
    for (s0, s1, d0, d1) in ((0, 162, 9, 175), (162, 494, 175, 516), (494, h, 516, TH2)):
        part = hr.crop((0, s0, w, s1)).resize((w, d1 - d0), Image.LANCZOS)
        body.paste(part, (0, d0))
    body.resize((round(w * 2560 / 2000), 922), Image.LANCZOS).save(os.path.join(OUT, "vankila_henkilokunta_2.png"))
    # 11) kirjasto + kanttiini: yksi kuva, kaksoisovi vasemmalla ja oikealla
    kb = load("kirjasto.png")
    kb.resize((2560, 922), Image.LANCZOS).save(os.path.join(OUT, "vankila_kirjasto.png"))
    print("valmis")


if __name__ == "__main__":
    main()

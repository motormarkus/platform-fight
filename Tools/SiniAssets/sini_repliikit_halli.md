# Sini: hallin nykyiset repliikit (äänitettäväksi)

Kaikki Sinin sanomat rivit pelistä (ArcadeSini.cs). Tiedostonimi = ehdotus (Resources/Sfx/sini/, mp3 tai wav); kytken ne koodiin nimen perusteella.

## 1. Valitukset, kun pelaaja juttelee (E): yksi per kierros
| Tiedosto | Repliikki |
|---|---|
| sini_ask_1 | Kone söi viimeiset kolikkoni. Taas. |
| sini_ask_2 | Kaikki muut voittavat täällä, vaan en minä. |
| sini_ask_3 | Olisi kiva tanssia, mutta ei jaksa yhtään. |

## 2. Vastaukset pelaajan valintaan (kolme per kierros: hyvä / keskitaso / huono)
Kierros 1 (kone söi kolikot)
| Tiedosto | Pelaajan valinta | Sinin vastaus |
|---|---|---|
| sini_r1_hyva | Ostetaan sulle kanttiinista jotain juotavaa. | Tuo oli kiltisti sanottu. |
| sini_r1_kesk | Kone ei ole sinua vastaan. | No, ehkä. Ei se silti auta. |
| sini_r1_huono | Ei kannata pelata, jos ei osaa. | Kiitti vaan. Todella hauska. |

Kierros 2 (kaikki muut voittavat)
| sini_r2_hyva | Sinun takiasi tänne tullaan, ei koneiden. | Hah. Sanoit sen ihan vakavalla naamalla. |
| sini_r2_kesk | Tuuri kääntyy kyllä, usko pois. | Toivottavasti sinulla on oikeus. |
| sini_r2_huono | Voitto on aina vain tuuria. | Aina yhtä lohdullista. |

Kierros 3 (ei jaksa tanssia)
| sini_r3_hyva | Yksi tanssi. Minä vain katson. | Okei, okei. Se riittää. |
| sini_r3_kesk | Istu sitten vähän aikaa. | Tai niin. |
| sini_r3_huono | Ei kukaan jaksa, ihan sama. | Tuo ei auttanut yhtään. |

## 3. Lopputulos
| Tiedosto | Repliikki | Milloin |
|---|---|---|
| sini_verdict_hyva | Kiitos. Tarvitsin tuota todella paljon. | piristys onnistui |
| sini_verdict_huono | Hmm. En nyt oikein jaksa. Mene vaan. | ei onnistunut |

## 4. Kun hän on hyvällä tuulella (150 s piristyksen jälkeen)
| sini_lahja | Sinä taas! Tässä sulle. | lentosuukko |
| sini_hyva | Hei! Mulla menee tosi hyvin. | tervehdys |

## 5. Hyvästit
| sini_heippa | Nähdään! | juttelu loppuu normaalisti |
| sini_heippa2 | No heippa sitten. | pelaaja poistuu kesken |
| sini_minne | Hei, minne sinä menet? | pelaaja kävelee pois |

## 6. Tappelu
| sini_apua | Apua! | kun hallin tappelu alkaa |

## 7. Jutustelu toisen kanssa (Dinan tai muiden), satunnaisesti
sini_juttu_1 Ai niinkö? · sini_juttu_2 Hahaha! · sini_juttu_3 Ei kai! · sini_juttu_4 Kerro lisää. · sini_juttu_5 Totta kai. · sini_juttu_6 Oho.

---
Yhteensä 23 repliikkiä. Lisäksi tiedostossa `kahvikohtaus_repliikit.md` on uusi kahvikohtaus (jos se tehdään).
Dinan repliikit ovat pitkiä sivuja ArcadeSini.cs:n BuildDinaPages-funktiossa; ne voin listata erikseen, jos teet myös hänelle puheet.

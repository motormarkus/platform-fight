# Sini: kutsu ja kahvikohtaus (puhepuu, ArcadeSini.cs: InviteTree / CoffeeTree)

Tiedostot: Assets/Resources/Sfx/sini/ (wav). Valmiina (✔) ja puuttuvat (☐).

## Kutsu (hallissa, kun piristys onnistuu)
- ☐ sini_kutsu_0: "Hei kuule, haluaisitko tulla käymään kahvilla? Me asutaan Dinan kanssa tässä yläkerrassa."
  - valinta: "Mielelläni, kiitos." → ☐ sini_kutsu_joo "Kiva, seuraa mua!"
  - valinta: "Ehkä joku toinen kerta." → ☐ sini_kutsu_ei "Okei, joku toinen kerta sitten."
  - (kummassakin tapauksessa yläkerran ovi aukeaa)

## Kämpällä, ensimmäinen kerta
- ✔ sini_k_tervetuloa: "Tervetuloa meidän majaan! Mä laitan kahvit tulille. Istu vaikka sohvalle."
- ✔ sini_k_lokki: "On varmaan rankkaa mätkiä noita Lokin kätyreitä?"
  1. "No käyhän se työstä." / 2. "Menee treenistä." → ✔ sini_k_hauska: "Haha, sä oot kyl hauska! Oot muuten käynyt sparraamassa Marcusta vastaan salilla?"
     - valinnat: "Joo kävin. Annoin köniin." / "Joo kävin. Hiton kova jätkä..." / "En oo käynyt vielä." → ☐ sini_k_ketaan (kysymys "onko sulla ketään?", ks. alla)
  3. "No kyllä mieluummin istun tässä sun kanssa." → ✔ sini_k_rauhoittuisi: "Jospa kadut rauhoittuisi pian... Kuule Rocco, onko sulla ketään? Niinku sillee..."
- Kysymys "onko sulla ketään":
  1. "No onhan mulla. Ollut jo pidemmän aikaa." → ✔ sini_k_levolle: "No hyvä! Mun täytyy varmaan lähteä nyt levolle. Olipa kiva kun kävit, käy uudestaan!" (loppu)
  2. "Ei ole tällä hetkellä." → ✔ sini_k_lahemmas: "Nooo, tuu nyt jätkä vähän lähemmäs sitten!"
     - "Mun täytyy mennä nyt." → ☐ sini_k_nahdaan "Nähdään taas!" (loppu)
     - "Täältä mää tuun." → ruutu himmenee; makuuhuoneen kuva Assets/Resources/Kampa/suudelma.png (png, lisätään myöhemmin)
- Kahvi (täysi lepo) tulee puun lopussa.

## Myöhemmät käynnit (vain teksti)
"Taas sinä. Pannu on kuuma." / "Istu vaan. Ei tarvitse puhua, jos ei huvita." / "Tässä, kahvi. Hyvä, että tulit."

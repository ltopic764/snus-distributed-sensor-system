# Bezbednosne mere

Ovaj dokument opisuje kako je ostvarena bezbedna komunikacija
između senzora (klijenata) i servera.

## Pregled

Sva komunikacija ide preko HTTP/REST-a, ali se sadržaj poruka nikada ne šalje
otvoreno. Svaka poruka koju senzor pošalje je istovremeno **šifrovana**, **digitalno potpisana** i
**zaštićena od ponavljanja**. Server dodatno primenjuje
ograničavanje brzine radi otpornosti na DoS napade.

## 1. Poverljivost — hibridna enkripcija (AES + RSA)

Koristi se kombinacija simetrične i asimetrične kriptografije:

- Sadržaj poruke (izmereno merenje) šifruje se algoritmom **AES** (AES-256, CBC,
  sa nasumičnim IV po poruci).
- Sam AES ključ se, pošto obe strane moraju da ga znaju, šifruje **RSA** javnim
  ključem servera (RSA-2048, OAEP-SHA256). Time samo server, koji poseduje
  odgovarajući privatni ključ, može da odmota AES ključ i pročita poruku.

Ovaj pristup daje brzinu simetrične
kriptografije uz bezbednu razmenu ključa bez unapred deljene tajne.

## 2. Autentičnost i integritet — digitalni potpis

Svaki senzor poseduje sopstveni par RSA ključeva. Pre slanja, senzor svojim
**privatnim** ključem potpisuje kanonizovani sadržaj poruke (ID senzora, redni
broj poruke, vreme slanja i šifrovani sadržaj). Server potpis proverava
**javnim** ključem tog senzora.

Time se potvrđuje se identitet pošiljaoca (samo vlasnik privatnog
ključa je mogao da napravi potpis) i garantuje se integritet (svaka izmena
poruke u prenosu poništava potpis). Bitno je da klijent i server računaju potpis
nad identičnim bajtovima, pa je logika kanonizacije na jednom mestu, u zajedničkoj
biblioteci (`CryptoHelper.BuildSigningData`).

## 3. Zaštita od replay napada

Uz svaku poruku senzor šalje **vreme slanja** i **jedinstveni redni broj poruke
(MessageId)** koji se uvećava posle svake poslate poruke. Server za svaki senzor
pamti poslednji viđeni MessageId i primenjuje dve provere:

- MessageId nove poruke mora biti strogo veći od poslednjeg — čime se odbacuje
  presretnuta pa ponovo poslata (replay) poruka.
- Vreme slanja mora biti unutar dozvoljenog vremenskog prozora (podrazumevano
  30 sekundi) — čime se odbacuju zastarele poruke.


## 4. Razmena ključeva i poznati kompromisi

Server generiše svoj RSA par pri prvom pokretanju; javni ključ deli senzorima
(oni njime šifruju AES ključ). Javni ključ senzora server saznaje pri prvom
kontaktu i pamti ga (pristup „trust on first use").

Ovo je svesno pojednostavljenje pogodno za projekat. U produkcijskom sistemu
razmena ključeva bi se obavila unapred i van ovog kanala (npr. PKI/sertifikati
ili ručno dostavljeni ključevi), kako napadač ne bi mogao da se lažno predstavi
pri prvom kontaktu.

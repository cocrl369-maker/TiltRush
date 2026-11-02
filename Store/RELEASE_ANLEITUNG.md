# TILT RUSH – Release im Google Play Store

Schritt-für-Schritt-Anleitung von Anfang an. Hake ab, was erledigt ist: `[ ]` → `[x]`.
Die Reihenfolge ist wichtig – einige Schritte gehen erst, wenn ein früherer erledigt ist.

**Zeitbedarf:** Arbeit ca. 1–2 Tage, dazu Wartezeiten: Konto-Prüfung (einige Tage),
geschlossener Test (**14 Tage**), App-Prüfung durch Google (einige Tage).

---

## Phase 1 – Spiel in Unity fertig machen

- [ ] **1.1** Neuesten `Scripts`-Ordner von GitHub holen (**Code > Download ZIP**) und in Unity den alten ersetzen.
- [ ] **1.2** *Window > Package Manager > Unity Registry* – diese Pakete müssen installiert sein:
  - **Leaderboards**
  - **Multiplayer Services** (oder „Lobby“ – nur eins von beiden)
  - **Advertisement Legacy**
  - **In-App Purchasing** (Version 5)
- [ ] **1.3** *Edit > Project Settings > Player > Other Settings > Active Input Handling* = **Both**.
- [ ] **1.4** Console prüfen (*Window > General > Console* → **Clear**): **keine roten Fehler**. Falls doch → Screenshot an Claude.
- [ ] **1.5** *File > Build Profiles > Android* → **Clean Build** und einmal per **Build And Run** auf dem Handy testen.
- [ ] **1.6** Musik: liegt in `Assets/Resources/Music/<Modus>`, Lizenz-Screenshots gespeichert, keine „non-commercial“-Lizenz.
- [ ] **1.7** `Assets/Resources/credits.txt` mit allen Künstlern angelegt (Titel, Künstler, Link, Lizenz).

## Phase 2 – Unity Cloud (Online + Werbung)

- [ ] **2.1** **cloud.unity.com** → dein Projekt → **Leaderboards**: diese 5 anlegen (keine Resets, keine Tiers):

  | Name | ID | Sortierung | Update type |
  |---|---|---|---|
  | Classic | `classic` | absteigend | Keep best score |
  | Hard | `hard` | absteigend | Keep best score |
  | Dodge | `dodge` | absteigend | Keep best score |
  | Asteroids | `asteroids` | absteigend | Keep best score |
  | Online | `online` | absteigend | **Keep latest score** |

- [ ] **2.2** Links **Monetization** (Unity Ads) einrichten → „Unity Ads only“ / keine Mediation.
- [ ] **2.3** Dort die **Android Game ID** notieren (Zahl, z. B. `5712345`).
- [ ] **2.4** Anzeigen-Einheit **Interstitial_Android** öffnen → „Skip after 5 seconds“ aktiv.
- [ ] **2.5** In Unity `Assets/Scripts/Core/Config.cs` öffnen und eintragen:
  ```csharp
  public const string AdsGameIdAndroid = "800420854";   // schon eingetragen
  ```
- [ ] **2.6** Spiel testen: *Options > Profile* zeigt „Online: ONLINE“. Nach 2 Minuten im Menü erscheint eine **Test**-Werbung.

## Phase 3 – Webseite mit Datenschutz und Impressum

- [ ] **3.1** **sites.google.com** → neue Website „Tilt Rush“.
- [ ] **3.2** Seite **„Datenschutz“** anlegen: Text aus `Store/datenschutz_de.md` einfügen, alle `[PLATZHALTER]` ausfüllen.
      Optional zweite Seite „Privacy Policy“ mit `Store/privacy_policy_en.md`.
- [ ] **3.3** Seite **„Impressum“** mit `Store/impressum_de.md` (Name, Anschrift, E-Mail).
- [ ] **3.4** Rechtstexte prüfen lassen (Datenschutz-Generator oder Anwalt) – empfohlen, besonders mit Werbung und Käufen.
- [ ] **3.5** Website **veröffentlichen** und die Adresse der Datenschutz-Seite notieren.
- [ ] **3.6** In `Config.cs` eintragen:
  ```csharp
  public const string PrivacyPolicyUrl = "https://sites.google.com/view/.../datenschutz";
  ```

## Phase 4 – Google Play Entwicklerkonto

- [ ] **4.1** **play.google.com/console** → Konto erstellen (**25 $ einmalig**), Kontotyp **Privat** (für Firmen: Organisation, braucht D-U-N-S-Nummer).
- [ ] **4.2** Identität bestätigen (Ausweis) und Telefonnummer/E-Mail verifizieren. Die Prüfung kann **einige Tage** dauern – solange mit Phase 5 weitermachen.
- [ ] **4.3** *Einstellungen > Zahlungsprofil*: **Zahlungsprofil anlegen** (Bankverbindung) – nötig für den Kauf „Werbung entfernen“ und für Auszahlungen.

## Phase 5 – Unity für den Store einstellen

*Edit > Project Settings > Player*, Reiter **Android** (Roboter-Symbol):

- [ ] **5.1** **Company Name**: dein Name oder Studioname · **Product Name**: `Tilt Rush` · **Version**: `1.0.0`
- [ ] **5.2** **Icon**: `Store/icon_1024.png` in den Unity-`Assets`-Ordner ziehen → bei *Icon > Default Icon* einsetzen.
- [ ] **5.3** *Other Settings*:
  - **Package Name** = `com.deinname.tiltrush` (nur Kleinbuchstaben) – ⚠️ **nie mehr änderbar** nach dem ersten Upload
  - **Bundle Version Code** = `1` (bei jedem Update +1)
  - **Target API Level** = *Automatic (highest installed)*
  - **Scripting Backend** = **IL2CPP**
  - **Target Architectures**: **ARM64** angehakt (ARMv7 darf zusätzlich an bleiben)
  - **Internet Access** = **Require**
- [ ] **5.4** *Publishing Settings > Keystore Manager > Keystore… > Create New > Anywhere*:
  Datei z. B. `tiltrush.keystore`, Passwort, Alias `tiltrush`, Alias-Passwort.
  ⚠️ **Keystore-Datei + beide Passwörter doppelt sichern** (USB-Stick + Cloud). Ohne sie keine Updates!
- [ ] **5.5** Bei *Publishing Settings* „Custom Keystore“ angehakt und Passwörter eingetragen.
- [ ] **5.6** *File > Build Profiles > Android*: Haken bei **Build App Bundle (Google Play)** → **Build** → Datei `TiltRush.aab` speichern.

## Phase 6 – App in der Play Console anlegen

- [ ] **6.1** **App erstellen**: Name „Tilt Rush“, Sprache Deutsch, **Spiel**, **Kostenlos**, Erklärungen bestätigen.
- [ ] **6.2** Unter **Dashboard > App einrichten** alle Punkte erledigen:
  - **Datenschutzerklärung**: URL aus 3.5
  - **App-Zugriff**: „Alle Funktionen ohne besonderen Zugriff verfügbar“
  - **Werbung**: **Ja**, die App enthält Werbung
  - **Einstufung des Inhalts**: Fragebogen (Kategorie Spiel; keine Gewalt gegen Menschen, kein Glücksspiel; Nutzer können interagieren = Spielernamen in Bestenlisten → ja; In-App-Käufe → ja)
  - **Zielgruppe**: **13 Jahre und älter** (nicht unter 13 – sonst strenge Familien-Regeln für Werbung)
  - **Datensicherheit**: Daten werden erhoben und geteilt durch Unity Ads/Unity Services: *Geräte- oder andere IDs*, *App-Aktivitäten (Interaktionen)*, *App-Infos und Leistung (Absturzprotokolle/Diagnose)*, *ungefährer Standort*; Daten werden verschlüsselt übertragen; Löschung per E-Mail möglich. Hilfe: Suche „Unity Ads Google Play data safety“.
  - **Behörden-Apps / Finanzen / Gesundheit / Nachrichten**: jeweils Nein
- [ ] **6.3** **Store-Eintrag** (*Präsenz im Play Store > Haupteintrag*): Texte aus `Store/store_listing.md` (Deutsch), dann unter „Übersetzungen“ Englisch hinzufügen.
  - **App-Symbol**: `Store/icon_512.png`
  - **Vorstellungsgrafik**: `Store/feature_1024x500.png`
  - **Screenshots**: 4–8 Stück vom Handy (Spiel spielen, **Power + Leiser** gleichzeitig drücken) – verschiedene Welten, Modi, Menü
  - **Kategorie**: Spiele > Arcade · **Kontakt-E-Mail** eintragen
- [ ] **6.4** *Einstellungen > Lizenztests*: deine eigene Google-Mail eintragen (Käufe testen ohne echtes Geld).

## Phase 7 – Interner Test (schnell, nur für dich)

- [ ] **7.1** *Testen > Interner Test > Neuen Release erstellen* → `TiltRush.aab` hochladen → Release-Name `1.0.0` → **Veröffentlichen**.
      (Beim ersten Mal fragt Google nach **Play App-Signatur** → **akzeptieren**.)
- [ ] **7.2** Reiter **Tester**: E-Mail-Liste mit deiner Adresse anlegen → Test-Link öffnen → App über Play Store installieren.
- [ ] **7.3** *Monetarisieren > Produkte > In-App-Produkte* → **Produkt erstellen** (geht erst, wenn ein Build hochgeladen ist):
  - Produkt-ID: **`remove_ads`** · Name: „Werbung entfernen“ · Beschreibung: „Entfernt alle Werbung für immer.“
  - Preis: z. B. **1,99 €** → Speichern → **Aktivieren**
- [ ] **7.4** Auf dem Handy testen: Werbung (Test-Werbung), Kauf **NO ADS** (Testkauf, kostenlos), Bestenlisten, Online-Status.

## Phase 8 – Store-Version bauen und geschlossener Test (Pflicht, 14 Tage)

- [ ] **8.1** In `Config.cs`: **`AdsTestMode = false`** setzen. Ab jetzt **nie auf die eigene Werbung klicken**.
- [ ] **8.2** **Bundle Version Code** auf `2` erhöhen → neues `.aab` bauen.
- [ ] **8.3** *Testen > Geschlossener Test > Track erstellen* → `.aab` hochladen → Länder auswählen (z. B. Deutschland, Österreich, Schweiz oder alle).
- [ ] **8.4** **Mindestens 12 Tester** (Familie, Freunde – jeder mit Google-Konto) als E-Mail-Liste eintragen und ihnen den Test-Link schicken.
- [ ] **8.5** Alle Tester installieren die App und **behalten sie 14 Tage am Stück** installiert (zwischendurch spielen ist gut).
- [ ] **8.6** Feedback der Tester sammeln – Fehler an Claude schicken, korrigierte Version mit Version Code +1 hochladen.

## Phase 9 – Veröffentlichen

- [ ] **9.1** Nach 14 Tagen erscheint im Dashboard **„Zugriff auf Produktion beantragen“** → kurze Fragen zum Test beantworten (wie viele Tester, was verbessert wurde). Google antwortet meist innerhalb weniger Tage.
- [ ] **9.2** *Produktion > Neuen Release erstellen* → letztes `.aab` übernehmen/hochladen → Versionshinweise (z. B. „Erste Version von TILT RUSH!“).
- [ ] **9.3** **Länder/Regionen** auswählen → **Zur Überprüfung senden**.
- [ ] **9.4** Prüfung durch Google (meist einige Tage) → danach ist **Tilt Rush im Play Store**. 🎉

## Phase 10 – Nach dem Release

- [ ] **Updates**: Bundle Version Code +1 (und Version z. B. `1.0.1`) → `.aab` bauen → *Produktion > Neuen Release*.
- [ ] **Bewertungen** in der Play Console beantworten.
- [ ] **app-ads.txt** (optional, mehr Werbeeinnahmen): braucht eine eigene Domain (z. B. tiltrush.de). Inhalt steht im Unity-Monetization-Dashboard; Datei kommt nach `https://deine-domain/app-ads.txt`, die Domain wird im Play-Store-Eintrag als Website eingetragen.
- [ ] **Einnahmen**: Werbung zahlt Unity aus (Zahlungsdaten im Unity-Dashboard hinterlegen), Käufe zahlt Google aus (Zahlungsprofil). Einnahmen ggf. beim Finanzamt angeben.

---

### Wichtig zu merken
- **Keystore + Passwörter** niemals verlieren.
- **Package Name** nie ändern.
- Für **jeden Upload** den **Bundle Version Code** erhöhen.
- **AdsTestMode** = `true` nur für eigene Tests, `false` für alles, was Tester/Spieler bekommen.
- Googles Regeln (Testerzahl, Ziel-API) ändern sich gelegentlich – die Play Console zeigt immer genau an, was fehlt.

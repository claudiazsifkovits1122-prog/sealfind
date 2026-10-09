# Robben-Suche – Game Design

## Vision

Eine gemütliche Such-App im Stil eines digitalen Wimmelbuchs. Kurze Runden (30 s – 3 min),
sofortiges Erfolgserlebnis, charmante Robben mit Persönlichkeit.

**Der Moment, um den sich alles dreht:** *„OH, DA IST SIE! 😂"*
Jede Mechanik muss diesen Moment entweder häufiger, überraschender oder belohnender machen.

## Core Loop

```
Szene erscheint ─► Ziel-Banner ─► suchen (verschieben/zoomen) ─► tippen
      ▲                                                          │
      │                                    ┌─── Treffer: Pop, Funken, +Punkte, Icon füllt sich
      │                                    └─── Fehlklick: rotes X, +Strafsekunden
      │                                                          │
 „Weiter" ◄── Ergebnis: Sterne, Zeit, neue Robben ◄── alle gefunden (Konfetti, Robben hüpfen)
```

## Aufgaben-Typen (pro Welt in dieser Reihenfolge)

| # | Name | Aufgabe | Besonderheit |
|---|---|---|---|
| 1 | Aufwärmen | Finde 5+ Robben | nur frei & hinter Deko |
| 2 | Gut versteckt | Finde 8+ Robben | + im Wasser, winzig, schlafend |
| 3 | Kindergarten | Finde die Babyrobben | Erwachsene als Ablenkung |
| 4 | Modenschau | Finde Robben mit *Welt-Accessoire* | Sonnenbrille, Schal, Partyhut, Piratenhut, Schnorchel |
| 5 | Gegen die Uhr | Finde 10+ Robben in N Sekunden | + abtauchende Robben, Countdown-Ticken |
| 6 | Finale | Finde 12+ Robben | alle Versteck-Arten inkl. Trickser & Schwimmer |

Pro Welt steigen: Anzahl Robben, Deko-Dichte, Köder, Tarnung (Fellfarbe nähert sich der Umgebung an);
die Robben werden kleiner.

## Versteck-Stile

| Stil | Wirkung |
|---|---|
| Frei | liegt zwischen der Deko – leicht, aber in dichten Bildern trotzdem knifflig |
| Hinter Deko | Steine, Kisten, Fässer, Eisblöcke, Boote … Kopf schaut seitlich heraus |
| Im Wasser | nur Kopf und Rücken über der Wasserlinie, wippt leicht |
| Abtauchen | taucht alle paar Sekunden ab – nur sichtbar antippbar („Siehst du mich?") |
| Schwimmend | zieht Bahnen; unter Wasser frei, sonst mit Kopf über Wasser |
| Winzig | 60 % Größe |
| Schlafend | Augen zu, steigende „Z"-Partikel |
| Trickser | hüpft beim ersten Treffer weg: „Hihi! Fast!" – keine Strafe |

## Wertung

- Punkte je Robbe nach Seltenheit: 100 / 200 / 500 / 800 / 1000 × Combo (bis x5, Fenster 3,5 s)
- Robben, die nicht zur Aufgabe passen: 50 Punkte, zählen nicht
- Fehlklick: +2 s (bei schnellem Spammen +3, +4, max. +5 s), −25 Punkte, Combo weg
- Köder-Stein antippen: wie Fehlklick, mit „Nur ein Stein!"
- Lupe: +5 s
- Zeitbonus am Ende für schnelles Finden
- Sterne: ≤ Zielzeit → 3 · ≤ 1,75 × Zielzeit → 2 · sonst 1 (Zeit-Level: nach verbleibender Zeit)

## Progression

- Level einer Welt werden nacheinander freigeschaltet.
- Welten kosten Sterne: 0 · 10 · 22 · 36 · 50 · 66 · 84 (von 126 möglichen).
- Das Sammelalbum zeigt unentdeckte Robben als Silhouette mit Seltenheit – Neugier als Motor.
- Legendäre Robben erscheinen vor allem als seltene **Bonus-Robbe** (8–35 % Chance je Level).

## Retention-Ideen, die schon drin sind

- Tages-Challenge mit Serien-Zähler und „!"-Badge im Menü
- Zufallsrunden nach Ende der Kampagne
- Bestzeiten pro Level

## Ideen für später (bewusst noch nicht gebaut)

- Echte Illustrationen statt prozeduraler Grafik (Schnittstelle: `ArtLibrary`)
- Musik-Loop pro Welt
- „Finde den Unterschied"-Modus (eine Robbe ist anders als die anderen)
- Robben-Profilseiten mit kleinen Animationen
- Taschenlampen-Mechanik in der Nachtwelt
- Weltraum-Welt mit Space-Robben 🚀
- Lokalisierung (Texte liegen bereits gesammelt in den Screens / `ContentDatabase`)
- Cloud-Save, Leaderboard für die Tages-Challenge

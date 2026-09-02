using A1Testsuite.Treiber;
using Xunit;

namespace A1Testsuite.Basisklassen;

/// <summary>
/// Schicht 1 (Testfaelle) fuer Projekt B, gegen die Treiberschnittstelle aus
/// claude/Akzeptanztests_Architektur.md (v2.0, Abschnitt 5). Ein [Fact] je
/// Testfall aus claude/Akzeptanztestfaelle_ProjektB.md. Konkrete Klassen
/// erzeugen im Konstruktor den jeweiligen Adapter und reichen ihn hier durch,
/// siehe a1-testsuite/xunit-project/README.md.
///
/// Produkte werden nicht ueber die Bezeichnungen "P1"-"P14" aus
/// claude/Anfangsdatenbestand_Spezifikation.md adressiert -- das sind reine
/// Spezifikationslabel, keine echten IDs. Stattdessen loest
/// <see cref="FindeProduktId"/> die tatsaechliche ID zur Laufzeit ueber
/// SucheProdukte(name) auf (siehe README.md, Abschnitt "Design-Entscheidung").
///
/// TF-B-F14-1/2 verwenden je eine eigens generierte E-Mail-Adresse statt der
/// im Dokument beispielhaft genannten "max.mustermann@example.com", damit
/// HoleBestellungen(kontakt) nicht durch die Bestellung aus
/// TF_B_F8_1_bis_TF_B_F11_1 verfaelscht wird -- eine bewusste, ueber das
/// Dokument hinausgehende Testisolations-Entscheidung.
/// </summary>
public abstract class ProjektBTestsBase
{
    private readonly IProjektBTreiber _treiber;

    protected ProjektBTestsBase(IProjektBTreiber treiber)
    {
        _treiber = treiber;
    }

    private async Task<string> FindeProduktId(string name)
    {
        var treffer = await _treiber.SucheProdukte(name);
        var produkt = treffer.SingleOrDefault(p => p.Name == name);
        Assert.True(produkt is not null, $"Produkt '{name}' nicht ueber SucheProdukte gefunden.");
        return produkt!.Id;
    }

    private static void AssertPreisNahe(decimal erwartet, decimal tatsaechlich)
        => Assert.True(
            Math.Abs(erwartet - tatsaechlich) <= 0.01m,
            $"Erwartet {erwartet:F2}, war {tatsaechlich:F2} (Toleranz 1 Cent, Abschnitt 7 der Akzeptanztest-Architektur).");

    private async Task<HashSet<string>> AlleProduktnamenInKategorie(string kategorie)
    {
        var namen = new HashSet<string>();
        var seite = 1;
        while (true)
        {
            var ergebnis = await _treiber.ListeProdukte(seite: seite, kategorieId: kategorie);
            foreach (var p in ergebnis.Produkte) namen.Add(p.Name);
            if (seite >= ergebnis.SeitenAnzahl) break;
            seite++;
        }
        return namen;
    }

    // B-F1: paginierte Produktliste, feste Gesamtzahl.
    [Fact]
    public async Task TF_B_F1_1_Erste_Seite_zeigt_Teilmenge_und_korrekte_Gesamtzahl()
    {
        var ergebnis = await _treiber.ListeProdukte(seite: 1);
        Assert.Equal(14, ergebnis.GesamtAnzahl);
        Assert.True(ergebnis.SeitenAnzahl >= 2, $"Erwartet mindestens 2 Seiten, war {ergebnis.SeitenAnzahl}.");
        Assert.NotEmpty(ergebnis.Produkte);
    }

    // B-F1: Vereinigung aller Seiten enthaelt alle 14 Produkte genau einmal.
    [Fact]
    public async Task TF_B_F1_2_Alle_Seiten_zusammen_ergeben_alle_vierzehn_Produkte()
    {
        var erste = await _treiber.ListeProdukte(seite: 1);
        var alleIds = new List<string>(erste.Produkte.Select(p => p.Id));

        for (var seite = 2; seite <= erste.SeitenAnzahl; seite++)
        {
            var naechste = await _treiber.ListeProdukte(seite: seite);
            alleIds.AddRange(naechste.Produkte.Select(p => p.Id));
        }

        Assert.Equal(14, alleIds.Count);
        Assert.Equal(14, alleIds.Distinct().Count());
    }

    // B-F2: zweistufiger Kategoriebaum.
    [Fact]
    public async Task TF_B_F2_1_Kategoriebaum()
    {
        var baum = await _treiber.HoleKategoriebaum();

        var elektronik = baum.SingleOrDefault(o => o.Name == "Elektronik");
        var haushalt = baum.SingleOrDefault(o => o.Name == "Haushalt");
        Assert.True(elektronik is not null, "Oberkategorie 'Elektronik' fehlt.");
        Assert.True(haushalt is not null, "Oberkategorie 'Haushalt' fehlt.");

        Assert.Equal(
            new HashSet<string> { "Kopfhörer", "Smartphones & Zubehör" },
            elektronik!.Unterkategorien.Select(u => u.Name).ToHashSet());
        Assert.Equal(
            new HashSet<string> { "Küchengeräte", "Reinigung" },
            haushalt!.Unterkategorien.Select(u => u.Name).ToHashSet());
    }

    // B-F3: Einschraenkung auf eine Unterkategorie.
    [Fact]
    public async Task TF_B_F3_1_Einschraenkung_auf_Kategorie()
    {
        var namen = await AlleProduktnamenInKategorie("Kopfhörer");
        Assert.Equal(
            new HashSet<string> { "Ohrhörer Modell Compact", "Bügelkopfhörer Studio", "Noise-Cancelling Over-Ear XR" },
            namen);
    }

    // B-F4: Sortierung nach Preis aufsteigend -> guenstigstes Produkt zuerst.
    [Fact]
    public async Task TF_B_F4_1_Sortierung_Preis_aufsteigend()
    {
        var ergebnis = await _treiber.ListeProdukte(seite: 1, sortierung: Sortierungen.PreisAufsteigend);
        Assert.NotEmpty(ergebnis.Produkte);
        Assert.Equal("Fensterreiniger Klar", ergebnis.Produkte[0].Name);
    }

    // B-F4: Sortierung nach Name absteigend -> alphabetisch letztes Produkt zuerst.
    [Fact]
    public async Task TF_B_F4_2_Sortierung_Name_absteigend()
    {
        var ergebnis = await _treiber.ListeProdukte(seite: 1, sortierung: Sortierungen.NameAbsteigend);
        Assert.NotEmpty(ergebnis.Produkte);
        Assert.Equal("Wasserkocher Kompakt", ergebnis.Produkte[0].Name);
    }

    // B-F5: Volltextsuche ueber Name und Beschreibung.
    [Fact]
    public async Task TF_B_F5_1_Volltextsuche()
    {
        var treffer = await _treiber.SucheProdukte("Reiniger");
        var namen = treffer.Select(p => p.Name).ToHashSet();
        Assert.Equal(new HashSet<string> { "Allzweckreiniger Zitrus", "Fensterreiniger Klar" }, namen);
    }

    // B-F6: Filterung nach kategoriespezifischer Eigenschaft (Kabellos = Ja).
    //
    // Nachtrag 31.08.2026 (Rollout b_bmad_1): die Anfangsdatenbestand-Spezifikation
    // legt fuer boolesche Merkmale wie "Kabellos" nur die fachliche Bedeutung
    // fest ("Ja/Nein" als Beschreibung), nicht die konkrete Zeichenkette --
    // das Dateiformat/die Werteform ist laut Spezifikation Abschnitt 1 bewusst
    // offengelassen. b_bmad_1 speichert den Wert serverseitig als "true"/"false"
    // statt "Ja"/"Nein" (bestaetigt gegen ProductQueryService.cs). Deshalb hier
    // mehrere plausible Kodierungen ausprobieren, statt "Ja" hart zu verdrahten --
    // das serverseitige Filtern ist exakter String-Abgleich, tolerant sein kann
    // also nur der Client, der die passende Kodierung selbst herausfindet.
    [Fact]
    public async Task TF_B_F6_1_Filterung_nach_Eigenschaft()
    {
        IReadOnlyList<ProduktUebersicht> produkte = [];
        foreach (var wert in new[] { "Ja", "true", "Wahr" })
        {
            var versuch = await _treiber.ListeProdukte(
                seite: 1,
                kategorieId: "Kopfhörer",
                eigenschaftsfilter: new Dictionary<string, string> { ["Kabellos"] = wert });
            if (versuch.Produkte.Count > 0)
            {
                produkte = versuch.Produkte;
                break;
            }
        }

        var namen = produkte.Select(p => p.Name).ToHashSet();
        Assert.Contains("Ohrhörer Modell Compact", namen);
        Assert.Contains("Noise-Cancelling Over-Ear XR", namen);
        Assert.DoesNotContain("Bügelkopfhörer Studio", namen);
    }

    // B-F7: Detailansicht mit Lieferanten und Preisen (P1, zwei Lieferanten).
    [Fact]
    public async Task TF_B_F7_1_Detailansicht_mit_Lieferanten()
    {
        var id = await FindeProduktId("Ohrhörer Modell Compact");
        var detail = await _treiber.HoleProdukt(id);

        Assert.Equal("Ohrhörer Modell Compact", detail.Name);
        Assert.Equal("Kopfhörer", detail.KategorieName);
        Assert.Equal("In-Ear", detail.Eigenschaften["Bauform"]);
        // Siehe Kommentar bei TF_B_F6_1: konkrete Zeichenkette fuer boolesche
        // Merkmale ist von der Spezifikation nicht festgelegt.
        Assert.Contains(detail.Eigenschaften["Kabellos"], new[] { "Ja", "true", "Wahr" });

        var nordtech = detail.Lieferanten.SingleOrDefault(l => l.LieferantName == "NordTech Distribution");
        var blitzversand = detail.Lieferanten.SingleOrDefault(l => l.LieferantName == "Blitzversand Elektronik");
        Assert.True(nordtech is not null, "Lieferant 'NordTech Distribution' fehlt.");
        Assert.True(blitzversand is not null, "Lieferant 'Blitzversand Elektronik' fehlt.");
        AssertPreisNahe(29.99m, nordtech!.Preis);
        AssertPreisNahe(27.50m, blitzversand!.Preis);

        Assert.True(detail.DurchschnittsBewertung is null or 0, "Durchschnittsbewertung im Ausgangszustand nicht 0/leer.");
        Assert.Equal(0, detail.AnzahlBewertungen);
    }

    // B-F8 bis B-F11: Warenkorb mit Mehr-Lieferanten-Produkt, Mengenaenderung,
    // Entfernen, Bestellung, Bestelluebersicht. Als eine zusammenhaengende
    // Testmethode implementiert, da jeder Schritt auf dem Ergebnis des
    // vorherigen aufbaut (Abschnitt 7 der Akzeptanztest-Architektur).
    [Fact]
    public async Task TF_B_F8_1_bis_TF_B_F11_1_Warenkorb_und_Bestellablauf()
    {
        var produktId = await FindeProduktId("Ohrhörer Modell Compact");
        var sitzung = await _treiber.NeueWarenkorbSitzung();

        // TF-B-F8-1: dasselbe Produkt bei zwei Lieferanten -> getrennte Positionen.
        await _treiber.LegeInWarenkorb(sitzung, produktId, "NordTech Distribution", 2);
        await _treiber.LegeInWarenkorb(sitzung, produktId, "Blitzversand Elektronik", 1);

        var warenkorb = await _treiber.HoleWarenkorb(sitzung);
        Assert.Equal(2, warenkorb.Positionen.Count);
        var positionNordtech = warenkorb.Positionen.Single(p => p.LieferantName == "NordTech Distribution");
        var positionBlitzversand = warenkorb.Positionen.Single(p => p.LieferantName == "Blitzversand Elektronik");
        Assert.Equal(2, positionNordtech.Menge);
        Assert.Equal(1, positionBlitzversand.Menge);

        // TF-B-F9-1: Preise, Zwischensummen, Gesamtsumme.
        AssertPreisNahe(29.99m, positionNordtech.Einzelpreis);
        AssertPreisNahe(27.50m, positionBlitzversand.Einzelpreis);
        AssertPreisNahe(59.98m, positionNordtech.Zwischensumme);
        AssertPreisNahe(27.50m, positionBlitzversand.Zwischensumme);
        AssertPreisNahe(87.48m, warenkorb.Gesamtsumme);

        // TF-B-F9-2: Menge der ersten Position aendern.
        await _treiber.AendereMenge(sitzung, positionNordtech.PositionId, 3);
        warenkorb = await _treiber.HoleWarenkorb(sitzung);
        positionNordtech = warenkorb.Positionen.Single(p => p.PositionId == positionNordtech.PositionId);
        Assert.Equal(3, positionNordtech.Menge);
        AssertPreisNahe(89.97m, positionNordtech.Zwischensumme);

        // TF-B-F9-3: zweite Position entfernen.
        await _treiber.EntferneAusWarenkorb(sitzung, positionBlitzversand.PositionId);
        warenkorb = await _treiber.HoleWarenkorb(sitzung);
        var uebrig = Assert.Single(warenkorb.Positionen);
        Assert.Equal(positionNordtech.PositionId, uebrig.PositionId);

        // TF-B-F10-1: Bestellung anlegen, Warenkorb wird geleert, Preis/Lieferant festgeschrieben.
        var lieferdaten = new Lieferdaten("Max Mustermann", "Beispielweg 1", "12345", "Musterstadt");
        var kontaktdaten = new Kontaktdaten("Max Mustermann", $"warenkorb-ablauf-{Guid.NewGuid():N}@example.com");
        var bestellkennung = await _treiber.LegeBestellungAn(sitzung, lieferdaten, kontaktdaten);
        Assert.False(string.IsNullOrWhiteSpace(bestellkennung));

        var warenkorbNachBestellung = await _treiber.HoleWarenkorb(sitzung);
        Assert.Empty(warenkorbNachBestellung.Positionen);

        // TF-B-F11-1: Bestelluebersicht.
        var bestellungen = await _treiber.HoleBestellungen(kontaktdaten);
        var bestellung = bestellungen.SingleOrDefault(b => b.Kennung == bestellkennung);
        Assert.True(bestellung is not null, $"Bestellung '{bestellkennung}' nicht in HoleBestellungen() gefunden.");
        Assert.False(string.IsNullOrWhiteSpace(bestellung!.Status));

        var bestellposition = Assert.Single(bestellung.Positionen);
        Assert.Equal("NordTech Distribution", bestellposition.LieferantName);
        AssertPreisNahe(29.99m, bestellposition.FestgeschriebenerPreis);
        Assert.Equal(3, bestellposition.Menge);
        AssertPreisNahe(89.97m, bestellung.Gesamtsumme);
    }

    // B-F12: Bewertung abgeben, erneute Bewertung desselben Autors ersetzt die vorherige.
    [Fact]
    public async Task TF_B_F12_1_und_2_Bewertung_abgeben_und_ersetzen()
    {
        var id = await FindeProduktId("Bügelkopfhörer Studio");

        // TF-B-F12-1
        await _treiber.BewerteProdukt(id, 4, "Julia Test");
        var nachErster = await _treiber.HoleProdukt(id);
        Assert.Equal(4.0, nachErster.DurchschnittsBewertung);
        Assert.Equal(1, nachErster.AnzahlBewertungen);

        // TF-B-F12-2
        await _treiber.BewerteProdukt(id, 2, "Julia Test");
        var nachZweiter = await _treiber.HoleProdukt(id);
        Assert.Equal(2.0, nachZweiter.DurchschnittsBewertung);
        Assert.Equal(1, nachZweiter.AnzahlBewertungen);
    }

    // B-F13: Aufrufzaehler steigt bei jeder Detailansicht, sortiert danach nach oben.
    //
    // 31.08.2026: sechs statt drei Aufrufe -- bei b_solo_1 erhoehen andere
    // Testmethoden (TF_B_F7_1 per explizitem HoleProdukt, sowie
    // TF_B_F8_1_bis_TF_B_F11_1 indirekt ueber den adapterinternen
    // Detail-Abruf in LegeInWarenkorb zur Aufloesung der Lieferanten-ID)
    // beilaeufig den Aufrufzaehler von "Ohrhoerer Modell Compact" auf
    // denselben Wert (3), den dieser Testfall fuer sein eigenes Zielprodukt
    // erzeugt. xUnit garantiert innerhalb einer Testklasse keine
    // Ausfuehrungsreihenfolge (derselbe Mechanismus wie beim A2-Fix in
    // Abschnitt 2b) -- ein Unentschieden bei der Sortierung ist dadurch
    // reihenfolgeabhaengig und kein SUT- oder Adapterfehler. Sechs Aufrufe
    // statt drei schaffen einen Sicherheitsabstand ueber die bekannte
    // maximale Fremdkontamination (3) hinaus, siehe Uebergabedokument.
    [Fact]
    public async Task TF_B_F13_1_und_2_Aufrufzaehler_und_Sortierung()
    {
        var id = await FindeProduktId("Smart-Displayhülle");

        var zaehlerVerlauf = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            zaehlerVerlauf.Add((await _treiber.HoleProdukt(id)).Aufrufzaehler);
        }

        for (var i = 1; i < zaehlerVerlauf.Count; i++)
        {
            Assert.True(
                zaehlerVerlauf[i] > zaehlerVerlauf[i - 1],
                $"Aufrufzähler stieg beim {i + 1}. Aufruf nicht (Verlauf: {string.Join(", ", zaehlerVerlauf)}).");
        }

        var sortiert = await _treiber.ListeProdukte(seite: 1, sortierung: Sortierungen.AufrufzaehlerAbsteigend);
        Assert.Equal(id, sortiert.Produkte[0].Id);
    }

    // B-F14, Fall 1: leerer Warenkorb wird abgelehnt.
    [Fact]
    public async Task TF_B_F14_1_Leerer_Warenkorb_wird_abgelehnt()
    {
        var sitzung = await _treiber.NeueWarenkorbSitzung();
        var lieferdaten = new Lieferdaten("Max Mustermann", "Beispielweg 1", "12345", "Musterstadt");
        var kontaktdaten = new Kontaktdaten("Max Mustermann", $"leerer-warenkorb-{Guid.NewGuid():N}@example.com");

        await Assert.ThrowsAsync<BestellungAbgelehntException>(
            () => _treiber.LegeBestellungAn(sitzung, lieferdaten, kontaktdaten));

        var bestellungen = await _treiber.HoleBestellungen(kontaktdaten);
        Assert.Empty(bestellungen);
    }

    // B-F14, Fall 2: unzulaessige Menge (0) wird abgelehnt, entweder schon
    // beim Einlegen oder spaetestens bei der Bestellung -- beides zulaessig
    // (Abschnitt 7 der Akzeptanztest-Architektur, TF-B-F14-2 als
    // zweispuriges Ergebnis).
    [Fact]
    public async Task TF_B_F14_2_Unzulaessige_Menge_wird_abgelehnt()
    {
        var produktId = await FindeProduktId("Standmixer Pro 900");
        var sitzung = await _treiber.NeueWarenkorbSitzung();
        var lieferdaten = new Lieferdaten("Max Mustermann", "Beispielweg 1", "12345", "Musterstadt");
        var kontaktdaten = new Kontaktdaten("Max Mustermann", $"unzulaessige-menge-{Guid.NewGuid():N}@example.com");

        var beimEinlegenAbgelehnt = false;
        try
        {
            await _treiber.LegeInWarenkorb(sitzung, produktId, "Haushaltswaren Söder", 0);
        }
        catch (WarenkorbAbgelehntException)
        {
            beimEinlegenAbgelehnt = true;
        }

        if (!beimEinlegenAbgelehnt)
        {
            await Assert.ThrowsAsync<BestellungAbgelehntException>(
                () => _treiber.LegeBestellungAn(sitzung, lieferdaten, kontaktdaten));
        }

        var bestellungen = await _treiber.HoleBestellungen(kontaktdaten);
        Assert.DoesNotContain(bestellungen, b => b.Positionen.Any(p => p.ProduktId == produktId));
    }
}

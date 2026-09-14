# Feature-zu-Datei-Zuordnung der zwei OSS-Originale

*Stand 07.09.2026 (Gegenprüfung Abschnitt 2.3/2.4 abgeschlossen) · Voraussetzung für die modulscharfe Baseline-Messung (FF3, siehe `Bachelorarbeit_Gesamtstand.md` Abschnitt 10 und `Metrikkatalog.md` Abschnitt 4). Baut auf den beim A2-Auszählen bereits gelesenen Belegstellen aus `A2_Feature_Abdeckung.md` auf und erweitert sie auf eine vollständige Zuordnung jeder Quelldatei. Abgelegt auch im Harness-Repository unter `docs/Feature_Datei_Zuordnung_Baseline.md` (Replikationspaket).*

## 1. Kategorien

Für die modulscharfe Messung (Hauptauswertung) zählen **nur** Dateien der Kategorien P und Q. Für die vollständige Messung (Sensitivitätswert) zählen zusätzlich Dateien der Kategorie X. Testcode (T) und generierte/Vendor-Dateien (G) sind nach der allgemeinen Ausschlussliste (`Referenzprojekte_Auswahl.md` Abschnitt 1.1, `Metrikkatalog.md` Abschnitt 4) in **beiden** Varianten ausgeschlossen.

| Kürzel | Bedeutung | modulscharf | vollständig |
|---|---|---|---|
| **P** | Produktivcode, einem oder mehreren Katalog-Features zugeordnet | ja | ja |
| **Q** | Querschnittscode ohne Feature-Bezug (Startup/DI, Routing, Basisklassen, DB-Kontext, Angular-Grundgerüst) — entsteht auch in den KI-Implementierungen | ja | ja |
| **X** | Produktivcode für Funktionalität, die im eingefrorenen Katalog bewusst ausgeschlossen wurde (Admin-Verwaltung, Auth, E-Mail, Artikelmodul, Lieferantenverwaltung, SignalR, Mehrsprachigkeit, Bildergalerie, Produktvergleich, Kommentarbäume, Statistikansicht) | nein | ja |
| **T** | Testcode (separat ausgewiesen, nicht Teil des Produktivwerts) | nein | nein |
| **G** | Generiert/Vendor/Build (Migrationen, Lock-Dateien, kompilierte JS-Artefakte, Vendor-Assets, Projekt-/Solution-Dateien, Binärdateien) | nein | nein |

## 2. Methodische Befunde bei der Zuordnung

**2.1 Angularbooking trennt Lese- und Admin-Schreibzugriff nicht auf Dateiebene.** Fast jeder Site-Controller (`EventsController`, `ShowingsController`, `RoomsController`, `VenuesController`) hat ungeschützte GET-Endpunkte (öffentlich, vom Frontend unter `ClientApp/src/app/site` und `booking` konsumiert, per Grep gegen `site.service.ts`/`booking.service.ts` verifiziert) und in derselben Datei `[Authorize(Roles="admin")]`-geschützte PUT/POST/DELETE-Endpunkte für den ausgeschlossenen Verwaltungsbereich. Diese Dateien werden vollständig dem jeweiligen Feature zugerechnet, weil sie unteilbar sind — das ist eine Konsequenz des geringeren Trennungsgrads des Originals gegenüber den KI-Implementierungen (die den Admin-Bereich nie bauen) und im Diskussionsteil als Differenzquelle zu benennen: die modulscharfe Baseline enthält dadurch etwas mehr admin-CRUD-Boilerplate, als eine saubere Trennung ergäbe.

**2.2 Godsend nutzt generische Basisklassen über mehrere Entitätstypen hinweg.** `EntityController<TEntity>.cs` liefert generisches CRUD, die Bewertungsfunktion (→ B-F12) UND die ausgeschlossene Kommentarfunktion in einer Datei; sie wird von `ProductController` (katalogrelevant) und `ArticleController`/weiteren (ausgeschlossen) geerbt. `SharedProject/Models/Shared/Repository.cs` trägt ebenso den Aufrufzähler (→ B-F13) und die Bewertungslogik (→ B-F12) neben generischem Basisrepository-Code. Beide Dateien werden den katalogrelevanten Features zugerechnet (B-F12/B-F13), mit der Einschränkung, dass sie zusätzlich Funktionalität für ausgeschlossene Entitätstypen tragen, die sich nicht codeseitig heraustrennen lässt.

**2.3 Unsichere Einzelfälle — Gegenprüfung abgeschlossen (07.09.2026).** Bei vier Godsend-Frontend-Komponenten ließ sich die Zuordnung ursprünglich nicht aus dem Dateinamen und den A2-Belegen ableiten und wurde zunächst ohne Volltextprüfung als X eingestuft: `store/home`, `store/consult`, `store/pages`, `store/input-output`. Die inzwischen durchgeführte Volltext- und Grep-Prüfung (Selektor-Suche über den gesamten `Client/src/app`-Baum) ergab:

- `store/home` und `store/consult`: **bestätigt als X.** Beide Komponenten sind trivial (leere/Platzhalter-Templates — `home.component.html` 0 Byte, `consult.component.html` nur `<h1>Consult</h1>`) und ausschließlich über `app.module.ts`-Routing referenziert, keine sonstige Verwendung im Baum.
- `store/pages` (`PagesComponent`, generisches Paginierungs-Widget): **korrigiert auf P.** Grep über die Templates zeigt Verwendung als `<godsend-pages>` u. a. in `store/products/products.component.html` — direkt tragend für B-F1 (paginierte Liste). Die Komponente wird zusätzlich in mehreren ausgeschlossenen Listenansichten (`articles`, `orders`, `suppliers`) verwendet, ist also nach der in 2.1/2.2 etablierten Logik unteilbar und wird dem Feature zugerechnet, für das sie tragend ist.
- `store/input-output` (`InputOutputComponent extends CustomControlValueAccessor<string>`, generisches Formular-Steuerelement): **korrigiert auf Q.** Grep zeigt Verwendung als `<godsend-input-output>` u. a. in `store/products/product-detail.component.html` — direkt tragend für B-F7. Die Komponente selbst trägt keine feature-spezifische Logik (generisches Eingabe-Widget, zusätzlich in mehreren ausgeschlossenen Bereichen verwendet), daher Q statt P.

Beide Korrekturen sind in Abschnitt 4.1–4.3 nachgezogen. **Konsequenz:** `sonar/scope/B-frontend-modulscharf.properties` wurde auf Basis der ursprünglichen (fehlerhaften) Einstufung erstellt und führt `store/pages/**` bzw. `store/input-output/**` bislang nicht in den Inclusions — die bereits durchgeführte SonarQube-Messung für `baseline-b-modulscharf` misst diese beiden Komponenten also noch nicht mit. Siehe Abschnitt 5 zur weiteren Behandlung.

**2.4 Doppelzuordnung `supplier.model.ts` — aufgelöst (07.09.2026).** Abschnitt 4.1 (B-F7) führte `supplier.model.ts` bereits unter den featurerelevanten Dateien, während Abschnitt 4.3 sie zugleich als X auflistete, mit dem Hinweis, dies vor Verwendung gegenzuprüfen. Die Prüfung von `product-detail.component.ts` (der Komponente hinter B-F7) zeigt: Die Datei importiert `SupplierInfo` direkt aus `supplier.model.ts` (zusätzlich zu `SupplierAndPrice` aus `product.model.ts`) und verwendet sie durchgängig für Kernfunktionalität der Detailansicht (`selectedSupplier`, `foundSuppliers`, `supplierToAdd`, `addSupplier()`, `removeSupplier()`). Die reine DTO-Form in `product.model.ts` deckt den Bedarf also nicht vollständig ab — `supplier.model.ts` ist echt featurerelevant für B-F7. **Auflösung: P**, der X-Eintrag in Abschnitt 4.3 entfällt. Gleiche Konsequenz wie in 2.3: `sonar/scope/B-frontend-modulscharf.properties` schließt `src/app/models/supplier.model.ts` aktuell noch explizit über `sonar.exclusions` aus — die bereits durchgeführte Messung für `baseline-b-modulscharf` ist davon mitbetroffen.

## 3. Projekt A — angularbooking

### 3.1 Zuordnungstabelle Feature → Dateien

| Feature | Backend | Frontend |
|---|---|---|
| A-F1 (Liste) | `Controllers/Site/EventsController.cs`, `Controllers/Site/ShowingsController.cs`, `Controllers/Site/RoomsController.cs`, `Models/Event.cs`, `Models/Showing.cs`, `Models/Room.cs` | `site/showing-list/*`, `site/_services/site.service.ts`, `site/_models/showing-list-entry*.ts`, `site/_models/showing-option*.ts`, `core/_models/entity/{event,showing,room}.ts` |
| A-F2 (Datumsfilter) | `Controllers/Site/ShowingsController.cs`, `Models/Showing.cs` | `site/showing-list/*`, `site/_services/site.service.ts` |
| A-F3 (Spielstättenfilter) | `Controllers/Site/VenuesController.cs`, `Models/Venue.cs` | `site/showing-list/*`, `site/venue-list/*`, `site/_services/site.service.ts`, `core/_models/entity/venue.ts` |
| A-F4 (Detailansicht inkl. Dauer/Altersfreigabe) | `Controllers/Site/EventsController.cs`, `Controllers/Site/VenuesController.cs`, `Models/Event.cs` (inkl. `AgeRatingType.cs`), `Models/Venue.cs` | `site/event-detail/*`, `site/venue-detail/*`, `site/site-detail/*`, `core/_models/entity/{event,age-rating-type,venue}.ts` |
| A-F5 (Räume/Sitzplan-Struktur) | `Controllers/Site/RoomsController.cs`, `Controllers/Site/VenuesController.cs`, `Models/Room.cs`, `Models/Venue.cs`, `Data/SeedDataProvider.cs` (anteilig) | `booking/allocation/*`, `core/_models/entity/room.ts` |
| A-F6 (Sitzplan grafisch) | `Controllers/Site/ShowingsController.cs` (Allocations) | `booking/allocation/allocation.component.*`, `booking/allocation/allocation-unit.component.*` |
| A-F7 (Sitzplatzauswahl) | `Controllers/Site/ShowingsController.cs` (`GetAllocatedSeating`) | `booking/allocation/allocation.component.ts` |
| A-F8 (Preiskategorien je Sitzplatz) | `Models/PricingStrategy.cs`, `Models/PricingStrategyItem.cs`, `Controllers/Site/ShowingsController.cs` ($expand PricingStrategy) | `booking/allocation/allocation.component.*`, `core/_models/entity/pricing-strategy*.ts` |
| A-F9 (Gesamtpreis) | wie A-F8 | `booking/allocation/allocation.component.ts`, `booking/booking.component.*`, `booking/_services/booking.service.ts` |
| A-F10 (Buchung anlegen) | `Controllers/Site/BookingsController.cs` (`CreateBooking`), `Controllers/Site/BookingItemsController.cs`, `Models/Booking.cs`, `Models/BookingItem.cs`, `Models/Customer.cs`, `Models/View/Booking/*` | `booking/booking.component.*`, `booking/_services/booking.service.ts` |
| A-F11 (Buchung abrufen) | `Controllers/Site/BookingsController.cs` (`GetBooking`), `Models/Booking.cs`, `Models/BookingItem.cs` | `shared/profile/profile.component.*` (Anzeige), `site/_services/site.service.ts` (`getBookings`), `booking/booking-status.component.*` |
| A-F12 (Stornierung) | `Controllers/Site/BookingsController.cs` (`CancelBooking`), `Models/BookingStatus.cs` | `shared/profile/profile.component.*` (Stornieren), `site/_services/site.service.ts` (`cancelBooking`) |
| A-F13 (Konflikt/Atomizität) | `Controllers/Site/BookingsController.cs` (`CreateBooking`, ein `SaveChanges`), `Migrations/…custom_db-integrity_checks*` (SQLite-Trigger — als Migration nach G ausgeschlossen, Logik textlich dennoch A-F13 zuzuordnen), `Data/DbRepository.cs` | — |

*Hinweis A-F11/A-F12:* `shared/profile/profile.component.*` zeigt zugleich Kontoprofil-Bearbeitung (ausgeschlossen, Auth-gebunden). Die Datei wird wegen der Buchungsanzeige/-stornierung A-F11/A-F12 zugerechnet (siehe 2.1-Logik).

*Hinweis A-F10:* `Models/Customer.cs` wird bei anonymer Buchung direkt in `BookingsController.CreateBooking` neu angelegt (Name/E-Mail/Adresse aus dem Formular) — nicht über den separaten, ausgeschlossenen `CustomersController`.

### 3.2 Querschnittscode (Q) — vollständig in modulscharf enthalten

Backend: `Program.cs`, `Startup.cs`, `Data/ApplicationDbContext.cs`, `Data/DbRepository.cs`, `Data/DbUnitOfWork.cs`, `Data/IRepository.cs`, `Data/IUnitOfWork.cs`, `Data/SeedDataProvider.cs`, `Models/IModel.cs`, `appsettings*.json`.

Frontend: `app.module.ts`, `app.component.*`, `core/core.module.ts`, `core/core-routing.module.ts` (+ `.spec.ts`-Pendants), `shared/shared.module.ts` (+ `.spec.ts`), `core/_animations/fade-animation.ts`, sowie Angular-CLI-Gerüst: `environments/*`, `main.ts`, `polyfills.ts`, `styles.css`, `karma.conf.js`, `tsconfig*.json`, `tslint.json`, `browserslist`, `index.html`, `favicon.ico`, `angular.json`.

### 3.3 Ausgeschlossen (X) — nur in „vollständig"

Backend: `Controllers/Account/AccountController.cs`, `Controllers/Site/CustomersController.cs`, `Controllers/Site/FeaturesController.cs`, `Controllers/Site/PricingStrategiesController.cs`, `Controllers/Site/PricingStrategyItemsController.cs`, `Models/User.cs`, `Models/FacilityFlags.cs`, `Models/Feature.cs`, `Models/View/Account/*`, `Services/Email/*`, `Services/JwtManager/*`.

Frontend: gesamtes `admin/*` (13 Unterordner/-module), `core/_services/{auth.service,auth-guard.service,refresh-interceptor.service}.ts` (+ specs), `core/_models/{login-user*,register-user*,csrf-response}.ts`, `core/registration/*`, `shared/login/*`.

### 3.4 Nicht bewertet (T/G) — Referenz auf bestehende Ausschlussliste

`AngularBooking.Tests/*`, alle `*.spec.ts`, `ClientApp/e2e/*` (T); `Migrations/*`, `ClientApp/package-lock.json`, `ClientApp/src/assets/bootstrap/*`, `*.csproj`, `*.csproj.user`, `*.sln`, `Properties/*`, `Resources/images/*`, `Dockerfile`, `publish.{bat,sh}`, `LICENSE`, `README.md` (G).

## 4. Projekt B — Godsend

### 4.1 Zuordnungstabelle Feature → Dateien

| Feature | Backend | Frontend |
|---|---|---|
| B-F1 (paginierte Liste) | `Controllers/ProductController.cs` (`ByFilter`), `SharedProject/Models/Product/{EFProductRepository,ProductRepository,FilterInfo,Product}.cs` | `store/products/{products,product-card}.component.*`, `store/pages/*`, `models/product.model.ts` |
| B-F2 (zweistufige Kategorien) | `Controllers/ProductController.cs` (`GetBaseCategories`, `GetSubCategories`, `GetAllCategories`), `SharedProject/Models/Product/Category.cs`, `SharedProject/Models/Seed/SeedHelper.cs` (anteilig) | `store/products/category-tree.component.*`, `services/category.service.ts` |
| B-F3 (Kategoriefilter) | wie B-F1/B-F2 | `store/products/{products,filter}.component.*` |
| B-F4 (Sortierung) | `SharedProject/Models/Product/OrderBy.cs`, `ProductController.cs` (`ByFilter`) | `store/products/{products,filter}.component.*` |
| B-F5 (Volltextsuche) | `Controllers/SearchController.cs`, `SharedProject/Models/Search/AllSearchResult.cs`, `EFProductRepository.cs` (`FilterBySearch`) | `store/search/*` |
| B-F6 (kategoriespezifische Eigenschaften) | `ProductController.cs` (`GetPropertiesByCategory`), `SharedProject/Models/Product/{Property,EAV}.cs` | `store/products/filter.component.*` |
| B-F7 (Detailansicht inkl. Lieferanten/Preise) | `ProductController.cs` (`Detail`), `SharedProject/Models/Product/{Product,ProductInformation}.cs`, `SharedProject/Models/Shared/LinkProductsSuppliers.cs`, `SharedProject/Models/Supplier/{Supplier,SupplierInformation}.cs` | `store/products/product-detail.component.*`, `store/input-output/*`, `store/rating/*`, `store/stars/*`, `models/{product,supplier,rating}.model.ts` |
| B-F8 (Warenkorb, Lieferant je Position) | `SharedProject/Models/Shared/LinkProductsSuppliers.cs` | `store/cart/*`, `services/cart.service.ts`, `models/cart.model.ts` |
| B-F9 (Warenkorbanzeige) | wie B-F8 | `store/cart/*`, `services/cart.service.ts` |
| B-F10 (Bestellung anlegen) | `Controllers/OrderController.cs` (`CreateOrUpdate`), `SharedProject/Models/Order/{Order,EFOrderRepository,IOrderRepository}.cs`, `SharedProject/Models/ViewModels/{OrderFromNg,OrderPartNg}.cs` | `models/order.model.ts` (Checkout selbst ist im Original nicht als eigene Komponente auffindbar, Bestellung wird direkt aus dem Warenkorb ausgelöst — siehe `store/cart/*`) |
| B-F11 (Bestellübersicht) | `Controllers/OrderController.cs` (`Detail`) | `store/orders/*` |
| B-F12 (Bewertung) | `Controllers/EntityController.cs` (`SetRating`/`Rating`/`Ratings`, anteilig — siehe 2.2), `SharedProject/Models/Shared/{Repository,LinkRatingEntity}.cs` (anteilig) | `store/rating/*`, `store/stars/*` |
| B-F13 (Aufrufzähler) | `SharedProject/Models/Shared/Repository.cs` (`Watch()`, anteilig), `SharedProject/Models/Product/OrderBy.cs` (`Watches`), `Product.cs` | `store/products/product-detail.component.*` |
| B-F14 (Ablehnung bei Fehlerfall) | `Controllers/OrderController.cs` (`CreateOrUpdate`) | — |

*Hinweis OrderController:* dieselbe Datei enthält zusätzlich `All`/`Count`/`ChangeStatus`/`Delete` für die ausgeschlossene Bestellverwaltung durch Fachpersonal — unteilbar, siehe 2.1-Logik, hier auf Godsend übertragen.

*Hinweis `store/pages` (B-F1):* die Komponente wird zusätzlich in den ausgeschlossenen Listenansichten `articles`, `orders`, `suppliers` verwendet — unteilbar, siehe 2.1/2.2-Logik und 2.3.

*Hinweis `store/input-output` (B-F7):* als Q (nicht P) geführt, siehe 4.2 — die Zeile hier dokumentiert nur die feature-tragende Verwendungsstelle, nicht die Kategorie.

### 4.2 Querschnittscode (Q)

Backend: `Godsend/Program.cs`, `Godsend/Startup.cs`, `Godsend/GlobalSuppressions.cs`, `Godsend/Services/NotFoundException.cs`, `Godsend/appsettings*.json`, `Godsend/nlog.config`, `SharedProject/Models/Shared/{DataContext,IEntity,Information}.cs`.

Frontend: `store/app/*`, `store/navmenu/*`, `store/input-output/*` (generisches Formular-Steuerelement, siehe 2.3 — verwendet u. a. in `product-detail` für B-F7 sowie in mehreren ausgeschlossenen Bereichen, ohne eigene feature-spezifische Logik), `services/{data.service,repository.service}.ts`, `shared/custom-control-value-accessor.ts`, `models/entity.model.ts`, Angular-CLI-Gerüst analog Projekt A (`environments/*`, `main.ts`, `polyfills.ts`, `styles.css`, `karma.conf.js`, `tsconfig*.json`, `tslint.json`, `browserslist`, `index.html`, `favicon.ico`, `angular.json`).

### 4.3 Ausgeschlossen (X)

Backend: `Controllers/{AccountController,ArticleController,ImageController,NotificationHub,SupplierController}.cs`, `Godsend/Services/{IImageService,IStorageService,ImageService,StorageService}.cs`, `SharedProject/Models/Article/*`, `SharedProject/Models/Identity/*`, `SharedProject/Models/ImageProcessing/*`, `SharedProject/Models/Seed/IdentitySeedData.cs`, `SharedProject/Models/Shared/{CommentHelper,LinkCommentEntity}.cs`, `SharedProject/Models/Supplier/{EFSupplierRepository,SupplierRepository,Location}.cs`, `SharedProject/Models/TestModel/*` (toter Code, siehe `Feature-Kataloge_final.md` Abschnitt 1), `SharedProject/Models/ViewModels/{LoginViewModel,RegisterViewModel}.cs`.

Frontend: `store/{admin,articles,comments,gallery,login,notification,pages*,registration,richtext,statistics,suppliers,user,home,consult}/*`, `store/products/products-comparison.component.*`, `store/shared/edit-delete/*`, `services/{authentication.guard,authentication.service,image.service,location.service,notification.service,storage.service}.ts`, `models/{article,comment,image}.model.ts`, `assets/i18n/*`.

*(`store/pages` — nur die Verwendungsstellen außerhalb von B-F1 sind X, die Komponente selbst ist als Ganzes P, siehe 2.3/4.1 — sie wird nicht doppelt gezählt, sondern vollständig P zugerechnet, analog 2.1/2.2. `store/input-output` und `models/supplier.model.ts` sind nach der Gegenprüfung vom 07.09.2026 nicht mehr Teil dieser Liste, siehe 2.3/2.4.)*

### 4.4 Nicht bewertet (T/G)

`API.Tests/*`, alle `*.spec.ts`/`*.e2e-spec.ts` (T); `Migrations/*`, `package-lock.json`, `npm-shrinkwrap.json`, alle kompilierten `*.js`/`*.js.map` neben `*.ts`-Quellen (Altlast aus dem Original-Build, kein generierter KI-Code, aber nach allgemeiner Regel „keine Build-Artefakte" auszuschließen), `*.csproj`, `*.sln`, `Client/ssl/*`, `Godsend/Images/*`, `_config.yml`, `CODE_OF_CONDUCT.md`, `.github/*`, `LICENSE`, `README.md` (G).

## 5. Offene Punkte

- ~~Abschnitt 2.3 (vier unsichere Godsend-Frontend-Komponenten) vor der SonarQube-Messung durch kurzen Blick in den Code gegenprüfen.~~ **Erledigt (07.09.2026)** — siehe Abschnitt 2.3. Ergebnis: `home`/`consult` bestätigt X, `pages` korrigiert auf P, `input-output` korrigiert auf Q.
- ~~Die Doppelzuordnung von `supplier.model.ts` (Abschnitt 4.3) auflösen, sobald `product-detail.component.ts` im Detail gelesen wird.~~ **Erledigt (07.09.2026)** — siehe Abschnitt 2.4. Ergebnis: P.
- **Neu (07.09.2026) — Konsequenz für die bereits abgeschlossene Messung `baseline-b-modulscharf`:** `sonar/scope/B-frontend-modulscharf.properties` wurde vor der Gegenprüfung erstellt und spiegelt die drei jetzt korrigierten Einstufungen noch nicht wider (fehlende Inclusions für `store/pages/**` und `store/input-output/**`, sowie `models/supplier.model.ts`, das zusätzlich noch explizit in den Exclusions steht). Betroffen ist ausschließlich die deskriptiv berichtete Baseline-Zeile `baseline-b-modulscharf` (Backend unverändert, nur Frontend betroffen) — nicht betroffen sind: die `vollständig`-Variante (keine Inclusions-Beschränkung), alle 24 AI-Lauf-Analysen (deren Scope wird pro Lauf aus dem tatsächlich implementierten Featureumfang abgeleitet, nicht aus dieser Tabelle) und die BMAD-vs.-Solo-Statistik in `Statistische_Auswertung.md` (vergleicht AI-Läufe untereinander, nicht gegen die Baseline). Empfehlung: `B-frontend-modulscharf.properties` korrigieren (Inclusions um `src/app/store/pages/**`, `src/app/store/input-output/**` ergänzen; `src/app/models/supplier.model.ts` aus den Exclusions entfernen und stattdessen implizit über die fehlende Exclusion in die Inclusions-Messung einschließen) und die zwei betroffenen Analysen (`baseline-b-modulscharf-backend` bleibt unverändert lauffähig, `baseline-b-modulscharf-frontend` neu laufen lassen), bevor diese Werte im Ergebniskapitel zitiert werden. **Umgesetzt (07.09.2026):** die Datei ist bereits korrigiert (`sonar/scope/B-frontend-modulscharf.properties`, Original als `.bak-vor-gegenpruefung-07092026` gesichert), nur die eigentliche Neuerhebung auf der Erhebungsmaschine steht noch aus — Befehl siehe `claude/SonarQube_Setup.md` Abschnitt 5. Unkritisch vor Schreibbeginn nachholbar, keine Blockade für Ebene B oder die Gesamtzusammenfassung.
- Diese Tabelle ist Grundlage für die SonarQube-Projektabgrenzung (`sonar.inclusions`/`sonar.exclusions` je Baseline-Variante) — siehe `SonarQube_Setup.md`.

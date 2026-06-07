# Entwicklungsroadmap: AI Knowledge Transfer Platform

## 1. Produktvision

Die Software wird als AI-gestuetzte Knowledge-Transfer-, Onboarding- und Compliance-Plattform fuer technische Anlagen, Maschinenbau-Umgebungen und komplexe Software-Systeme entwickelt.

Das System nimmt Dokumente, Source Code, Konfigurationen, Betriebswissen und technische Workflows auf und erzeugt daraus strukturierte, pruefbare und nachvollziehbare Wissensartefakte.

Ziel ist nicht nur ein Chatbot, sondern eine Plattform, die aus Quellen belastbare Einarbeitungsplaene, Glossare, Checklisten, Trainingspfade, Review-Dokumente und Compliance-Nachweise erstellt.

## 2. Zielgruppen

| Rolle | Hauptnutzen |
| --- | --- |
| Neuer Mitarbeiter | Gefuehrte Einarbeitung in System, Architektur und Prozesse |
| Senior Engineer | Wissen bereitstellen, pruefen und freigeben |
| Projektleiter | Fortschritt, Luecken und Risiken sehen |
| Compliance / QS | Nachweise, Reviews und Freigaben nachvollziehen |
| Support Engineer | Troubleshooting, Runbooks und Workflows nutzen |
| Administrator | Benutzer, Rollen, Datenquellen und Betrieb verwalten |

## 3. Leitprinzipien

- Menschliche Freigabe vor finaler Veroeffentlichung von AI-generierten Inhalten.
- Jede wichtige Aussage braucht eine Quelle oder wird als unsicher/offen markiert.
- Compliance, Auditierbarkeit und Security werden von Beginn an mitentwickelt.
- Start als modularer Monolith, spaetere Aufteilung nur bei echtem Bedarf.
- Businesslogik bleibt im Application- und Domain-Layer, nicht in der UI.
- MVP klein halten: zuerst Quellen, Review und Roadmap-Generierung stabil machen.

## 4. Zielarchitektur

```text
Blazor Web UI
  -> API / Application Services
  -> Domain Layer
  -> Infrastructure Layer
  -> Database / File Storage / AI Provider / Search Index
```

Empfohlene Projektstruktur:

```text
src/
  AiKnowledgeTransfer.Web
  AiKnowledgeTransfer.Api
  AiKnowledgeTransfer.Application
  AiKnowledgeTransfer.Domain
  AiKnowledgeTransfer.Infrastructure
  AiKnowledgeTransfer.Contracts

tests/
  AiKnowledgeTransfer.UnitTests
  AiKnowledgeTransfer.IntegrationTests
  AiKnowledgeTransfer.UiTests
  AiKnowledgeTransfer.ArchitectureTests
```

## 5. Technologieentscheidung

| Bereich | Empfehlung |
| --- | --- |
| Plattform | .NET LTS |
| Sprache | C# |
| UI | Blazor Web App |
| API | ASP.NET Core |
| Datenbank | PostgreSQL oder SQL Server |
| Datenzugriff | Entity Framework Core |
| Authentifizierung | OpenID Connect, Entra ID oder Active Directory |
| Dokumentenspeicher | Lokaler Storage fuer MVP, spaeter Blob Storage |
| AI/RAG | Provider-abstrahiert, z. B. Azure OpenAI, OpenAI oder lokale Modelle |
| Suche | Volltextsuche plus Vector Search |
| Logging | Microsoft Logging oder Serilog |
| Background Jobs | BackgroundService, spaeter Hangfire oder Quartz |

## 6. MVP-Scope

Version 1 konzentriert sich auf den wichtigsten Wert: technische Quellen aufnehmen, daraus Wissen extrahieren, eine Einarbeitungsroadmap erzeugen und diese durch Experten pruefen lassen.

MVP-Funktionen:

- Login und rollenbasierter Zugriff.
- Projekt anlegen.
- Dokumente hochladen und versionieren.
- Metadaten und Quellen verwalten.
- Dokumente analysieren.
- Glossar erzeugen.
- Komponentenliste erzeugen.
- Offene Fragen und Wissensluecken erkennen.
- Einarbeitungsroadmap erzeugen.
- Review und Freigabe durch Experten.
- Export als Markdown, Word oder PDF.

Nicht im MVP:

- Mandantenfaehigkeit.
- Vollautomatische Diagrammgenerierung.
- Tiefe Source-Code-Analyse.
- Confluence- oder SharePoint-Integration.
- Vollstaendige Quiz Engine.
- Komplexer Workflow Designer.
- Vollstaendige Compliance Matrix.

## 7. Phasenplan

Aktueller Implementierungsstand:

- Phase 1 ist gestartet: Solution-Struktur, Schichten, Dependency Injection, API und Tests sind vorhanden.
- Phase 2 ist gestartet: Blazor-Dashboard als erster MVP-Prototyp ist vorhanden.
- Phase 2 ist erweitert: Blazor bietet einen interaktiven MVP-Workflow fuer Projekt, Upload, Analyse, Extraction, Review, Roadmap, Traceability und Export.
- Phase 3 ist gestartet: Projektanlage, Dokumentregistrierung, echter Datei-Upload und lokale Dateispeicherung sind vorhanden.
- Phase 4 ist gestartet: Text-/Markdown-Parsing, Dokument-Chunks, Analyse-Endpunkt, heuristische Knowledge Extraction und OpenAI als erster provider-unabhaengiger AI-Provider sind vorhanden; Azure OpenAI, Kunden-AI und lokale Modelle koennen spaeter als weitere Provider folgen.
- Phase 5 ist gestartet: Roadmaps verwenden freigegebenes Wissen bevorzugt und markieren nicht freigegebene Wissenselemente als Review-Hinweise.
- Phase 6 ist gestartet: Wissenselemente koennen in Review gesetzt, freigegeben oder abgelehnt werden; Review-Metadaten werden gespeichert.
- Phase 7 ist gestartet: Traceability Matrix verknuepft Quellen, Chunks, Wissenselemente, Review-Status und Roadmap-/Export-Nutzung.
- Phase 8 ist gestartet: Projektuebergaben koennen als Markdown exportiert werden.
- Phase 9 ist gestartet: Projekte werden lokal als JSON-Datei persistiert; Upload-Dateien bleiben im lokalen Storage.
- Security-Grundlage ist gestartet: MVP-Rollenmatrix fuer Admin, Senior Engineer, Contributor und Viewer ist vorhanden und in der UI sichtbar.
- Audit-Grundlage ist gestartet: zentrale Aktionen werden als JSON-basiertes Audit Log gespeichert und per API/UI abrufbar gemacht.
- UX-/Fehlerbehandlungs-Grundlage ist gestartet: kritische Blazor-Workflow-Aktionen zeigen kontrollierte Status- und Fehlermeldungen statt ungefangener UI-Fehler.
- API-Haertung ist gestartet: REST-Endpunkte validieren Kern-Requests und liefern konsistente JSON-Fehlerantworten fuer Validierungs-, NotFound- und BadRequest-Faelle.
- Betriebs-Grundlage ist gestartet: Health-Endpoint meldet lokalen Storage, Persistenzdateien und AI-Provider-Konfiguration inklusive Fallback-Betrieb.
- Backup-/Restore-Grundlage ist gestartet: lokale JSON-Persistenz, Audit Log und Upload-Dateien koennen als ZIP gesichert, aufgelistet und kontrolliert wiederhergestellt werden.

### Phase 0: Produktklaerung

Ziel: Den fachlichen Rahmen und den MVP eindeutig festlegen.

Aufgaben:

- Zielgruppen und Kernnutzen bestaetigen.
- Wichtigste Use Cases priorisieren.
- Compliance-Scope definieren.
- Risiken und Annahmen dokumentieren.
- MVP-Abgrenzung festlegen.

Deliverables:

- Product Vision.
- Stakeholder Map.
- MVP-Scope.
- Initial Risk List.
- Erste Requirements.

Akzeptanzkriterium:

- Das Team kann in einem Satz erklaeren, welchen konkreten Nutzen Version 1 liefert und was bewusst nicht enthalten ist.

### Phase 1: Architektur und Projektfundament

Ziel: Ein stabiles technisches Fundament fuer Entwicklung, Tests und spaetere Audits schaffen.

Aufgaben:

- Solution- und Projektstruktur erstellen.
- Architekturregeln definieren.
- Datenbankmodell fuer Projekte, Dokumente, Versionen, Benutzer und Reviews entwerfen.
- Authentifizierungsmodell festlegen.
- AI-Provider als Interface abstrahieren.
- CI/CD-Grundlage vorbereiten.
- Logging, Error Handling und Konfiguration standardisieren.

Deliverables:

- Solution-Struktur.
- Architecture Decision Records.
- Software Architecture Document.
- Coding Guidelines.
- Definition of Done.
- Erste Unit- und Architekturtests.

Akzeptanzkriterium:

- Die Anwendung startet lokal, hat eine klare Schichtenstruktur und kann automatisiert getestet werden.

### Phase 2: UX-Prototyp

Ziel: Die wichtigsten Arbeitsablaeufe frueh validieren.

Aufgaben:

- Dashboard entwerfen.
- Projektansicht entwerfen.
- Dokumenten-Upload-Flow bauen.
- Roadmap-Preview bauen.
- Review Center entwerfen.
- Export Wizard skizzieren.

Deliverables:

- Klickbarer Blazor-Prototyp.
- UX-Flows fuer Upload, Analyse, Review und Export.
- Feedbackliste mit priorisierten Verbesserungen.

Akzeptanzkriterium:

- Ein Testnutzer kann ohne Erklaerung ein Projekt anlegen, Dokumente hochladen und eine Roadmap-Vorschau finden.

### Phase 3: Dokumentenverwaltung

Ziel: Quellen sauber, versioniert und nachvollziehbar speichern.

Aufgaben:

- Projekte anlegen und bearbeiten.
- Dokumente hochladen.
- Dokumentversionen speichern.
- Dateityp, Quelle, Autor, Upload-Zeitpunkt und Status speichern.
- Dokumentstatus einfuehren: Entwurf, analysiert, in Review, freigegeben, archiviert.
- Zugriff nach Rolle absichern.

Deliverables:

- Dokumentenmodul.
- Datenmodell fuer Quellen und Versionen.
- Upload-Validierung.
- Audit Log fuer Uploads.

Akzeptanzkriterium:

- Ein Benutzer kann mehrere Dokumente einem Projekt zuordnen, Versionen nachvollziehen und alte Versionen einsehen.

### Phase 4: Parsing und Knowledge Extraction

Ziel: Aus Dokumenten erste strukturierte Wissenselemente erzeugen.

Aufgaben:

- Parser-Pipeline fuer Markdown, TXT, PDF und Word vorbereiten.
- Dokumente in Chunks zerlegen.
- Metadaten je Chunk speichern.
- AI-basierte Extraktion fuer Begriffe, Komponenten, Workflows und offene Fragen implementieren.
- Quellenverweise pro extrahiertem Element speichern.
- Unsichere Aussagen markieren.

Deliverables:

- Parsing-Pipeline.
- Knowledge Extraction Service.
- Glossar-Entwurf.
- Komponentenliste.
- Liste offener Fragen.

Akzeptanzkriterium:

- Nach Analyse eines Dokuments erzeugt das System ein Glossar, Komponenten und offene Fragen mit Quellenverweisen.

### Phase 5: Roadmap Generator

Ziel: Aus freigegebenem oder pruefbarem Wissen strukturierte Einarbeitungsplaene erzeugen.

Aufgaben:

- Zielrolle auswaehlen, z. B. Entwickler, Support, QS oder Operator.
- Roadmap-Dauer festlegen, z. B. 2, 4 oder 8 Wochen.
- Lernziele generieren.
- Kapitel, Uebungen, Checklisten und Pruefungsfragen erzeugen.
- Abschlusskriterien je Woche definieren.
- Quellen und offene Unsicherheiten anzeigen.

Deliverables:

- Roadmap Generator.
- Roadmap Preview.
- Bearbeitbare Roadmap-Struktur.
- Quellen- und Unsicherheitsanzeige.

Akzeptanzkriterium:

- Ein Benutzer kann aus analysierten Dokumenten eine 2- bis 4-Wochen-Roadmap fuer eine Zielrolle erzeugen und bearbeiten.

### Phase 6: Review und Freigabe

Ziel: AI-generierte Inhalte kontrolliert pruefen und freigeben.

Aufgaben:

- Review-Status fuer Glossar, Komponenten, Workflows und Roadmaps einfuehren.
- Kommentare und Aenderungsvorschlaege erfassen.
- Freigabeprozess mit Rollen umsetzen.
- Versionen vergleichen.
- Final freigegebene Wissensartefakte kennzeichnen.

Deliverables:

- Review Center.
- Kommentar- und Freigabeprozess.
- Versionshistorie.
- Audit Log.

Akzeptanzkriterium:

- Kein generierter Inhalt kann als final gelten, bevor ein berechtigter Experte ihn freigegeben hat.

### Phase 7: Export

Ziel: Freigegebenes Wissen als nutzbare Dokumente ausgeben.

Aufgaben:

- Markdown Export implementieren.
- Word oder PDF Export implementieren.
- Roadmap, Glossar, Komponentenliste und offene Fragen exportieren.
- Exportversion und Quelle dokumentieren.

Deliverables:

- Export Center.
- Export Templates.
- Exporthistorie.

Akzeptanzkriterium:

- Ein freigegebener Einarbeitungsplan kann reproduzierbar exportiert werden.

### Phase 8: Compliance und Traceability

Ziel: Nachvollziehbarkeit fuer QS, Audits und technische Freigaben schaffen.

Aufgaben:

- Requirement Traceability einfuehren.
- Quelle zu Aussage zu Review zu Export verknuepfen.
- Compliance Matrix als erste Version bauen.
- Audit Log auswertbar machen.
- Security- und Datenschutzkonzept dokumentieren.

Deliverables:

- Traceability Matrix.
- Compliance Matrix.
- Audit-Auswertung.
- Security Concept.
- Data Protection Concept.

Akzeptanzkriterium:

- Ein Auditor kann nachvollziehen, welche Quelle zu welcher Aussage gefuehrt hat und wer diese Aussage freigegeben hat.

### Phase 9: Betrieb, Sicherheit und Skalierung

Ziel: Die Plattform stabil im Unternehmensbetrieb nutzbar machen.

Aufgaben:

- Monitoring einbauen.
- Backup und Restore definieren.
- Dependency Scan, Secret Scan und SAST in CI/CD aufnehmen.
- Performance fuer grosse Dokumente verbessern.
- Rollen- und Rechteverwaltung erweitern.
- Optional Mandantenfaehigkeit planen.

Deliverables:

- Betriebshandbuch.
- Monitoring Dashboard.
- Security Gates.
- Backup- und Restore-Konzept.
- Release-Prozess.

Akzeptanzkriterium:

- Die Plattform kann kontrolliert deployed, ueberwacht, gesichert und bei Fehlern wiederhergestellt werden.

## 8. Erste Epics fuer das Backlog

| Epic | Beschreibung | Prioritaet |
| --- | --- | --- |
| Projektverwaltung | Projekte, Status und Verantwortliche verwalten | Hoch |
| Dokumentenverwaltung | Upload, Versionierung, Metadaten und Quellen | Hoch |
| Rollen und Rechte | Zugriff nach Benutzerrolle absichern | Hoch |
| Parsing Pipeline | Dokumente lesbar und analysierbar machen | Hoch |
| Knowledge Extraction | Begriffe, Komponenten, Workflows und Luecken erkennen | Hoch |
| Roadmap Generator | Rollenbasierte Einarbeitungsplaene erstellen | Hoch |
| Review Center | Expertenpruefung und Freigabe | Hoch |
| Export Center | Roadmaps und Wissensartefakte exportieren | Mittel |
| Compliance Matrix | Nachweise und Traceability strukturieren | Mittel |
| Integrationen | Confluence, SharePoint, Git Repositories | Niedrig |

## 9. Definition of Done

Eine Funktion gilt erst als fertig, wenn:

- Requirement dokumentiert ist.
- Akzeptanzkriterien definiert sind.
- Implementierung abgeschlossen ist.
- Unit Tests vorhanden sind.
- Kritische Fehlerfaelle behandelt sind.
- Logging fuer relevante Ereignisse existiert.
- Security-Auswirkungen betrachtet wurden.
- UI verstaendlich und rollenbasiert nutzbar ist.
- Dokumentation aktualisiert wurde.
- Review abgeschlossen ist.

## 10. Empfohlene Umsetzungsreihenfolge

1. Produktvision und MVP finalisieren.
2. Solution-Struktur und Architekturregeln erstellen.
3. Projekt- und Dokumentenverwaltung bauen.
4. Upload, Versionierung und Quellenmodell stabilisieren.
5. Parsing-Pipeline implementieren.
6. Erste Knowledge Extraction bauen.
7. Roadmap Generator entwickeln.
8. Review- und Freigabeprozess integrieren.
9. Export implementieren.
10. Traceability, Compliance und Betrieb erweitern.

## 11. Wichtigste Produktentscheidung

AI erzeugt Vorschlaege. Menschen geben Wissen frei.

Diese Entscheidung schuetzt Qualitaet, Compliance und Vertrauen in der Plattform.

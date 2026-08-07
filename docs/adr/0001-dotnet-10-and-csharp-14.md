# ADR-0001 — Usare .NET 10 e C# 14

- Stato: **Accepted**
- Data: 2026-08-07

## Contesto

Il progetto nasce come nuovo software senza vincoli di compatibilità con runtime precedenti. La TUI scelta è Terminal.Gui 2.x, la cui linea stabile corrente targetta .NET 10.

## Decisione

Tutti i progetti della solution targettano `net10.0` e usano C# 14. Il target è centralizzato in `Directory.Build.props`; `global.json` fissa la linea SDK 10.0 con roll-forward alla feature band disponibile.

## Conseguenze

- accesso alle API e al tooling moderni di .NET 10;
- nessun costo di multi-targeting nelle prime milestone;
- è richiesto .NET SDK 10.x per build e sviluppo;
- un eventuale target precedente richiederà un ADR dedicato.

## Alternative considerate

### .NET 8

Scartato dopo la decisione di usare la linea Terminal.Gui 2.x corrente.

### Multi-target net8.0/net10.0

Scartato perché aumenta superficie di test e vincoli senza un requisito concreto.

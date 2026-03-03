# AI Rules (OLTRE)

Regole operative quando usiamo AI per modificare il progetto.

## Regole
- **Non cambiare comportamento** se non esplicitamente richiesto.
- **Patch piccole:** massimo **1-2 file** per volta.
- **Niente nuove dipendenze / package** senza richiesta esplicita.
- **Niente refactor/rename massivi** (se serve, prima si propone un piano).
- Ogni patch deve includere:
  - **Motivo** (perché si fa)
  - **Cosa cambia** (in termini di comportamento)
  - **Come testare in Unity** (passi ripetibili)
- **Mai segreti/API key** nel codice o nei prompt.

## Workflow consigliato
1) Prima: **piano senza codice**
2) Poi: **patch piccola**
3) Poi: **test in editor**
4) Infine: **review-mode** (caccia a regressioni)

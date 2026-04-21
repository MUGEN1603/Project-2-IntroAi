# Belief Revision Engine
**Course:** 02180 Introduction to AI — Assignment 2
**Language:** C# (.NET)

A belief revision engine for propositional logic. The agent holds a set of beliefs with priorities, and can revise, contract, or expand those beliefs when new information arrives.

---

## How to Run

Make sure you have the [.NET SDK](https://dotnet.microsoft.com/download) installed. Then navigate to the project folder and run:

```bash
dotnet run
```

That's it. No extra setup needed.

---

## What Happens When You Run It

The program runs in two phases:

### Phase 1 — Automatic Demo
The program first runs a fixed demonstration using **Bob's belief base** from the lecture:

| Formula | Priority | Meaning |
|---|---|---|
| `p` | 2 (highest) | Bob strongly believes p |
| `q` | 1 | Bob believes q |
| `p → ¬q` | 0 (lowest) | Bob weakly believes p implies not-q |

This base is **inconsistent** — `p` and `q` together contradict `p → ¬q`.

The demo walks through:
1. **Contraction** — remove `q` from the base using partial meet contraction
2. **Expansion** — add `¬q` to the base (no consistency check)
3. **Revision** — revise by `¬q` using the Levi Identity: `B * φ = (B ÷ ¬φ) + φ`
4. **AGM Postulate Tests** — verifies all 5 postulates hold:
   - ✅ Success
   - ✅ Inclusion
   - ✅ Vacuity
   - ✅ Consistency
   - ✅ Extensionality

### Phase 2 — Interactive Mode
After the demo, you land in an interactive prompt where you can run your own operations.

---

## Interactive Mode

```text
=== Interactive Mode ===
Starting from Bob's belief base (pre-loaded above).
Type 'reset' to start from an empty base.
Commands: revise <formula>, contract <formula>, expand <formula>, print, reset, exit

Current Base: { [p=2] p, [p=1] q, [p=0] (p → ¬q) }
> revise not q
Parsed formula: ¬q
Revision successful.

Current Base: { [p=2] p, [p=0] (p → ¬q), [p=3] ¬q }
> contract p
Parsed formula: p
Contraction successful.

Current Base: { [p=0] (p → ¬q), [p=3] ¬q }
> reset
Base reset to empty.

Current Base: { }
> expand p implies q
Parsed formula: (p → q)
Expansion successful.

Current Base: { [p=1] (p → q) }
> exit
```

### Available Commands

| Command | What it does |
|---|---|
| `revise <formula>` | Revise the base by the formula (removes conflicts first, then adds) |
| `contract <formula>` | Remove the formula from the base while keeping as much as possible |
| `expand <formula>` | Add the formula directly (no conflict check) |
| `print` | Print the current base |
| `reset` | Wipe the base to empty — useful for testing your own scenarios |
| `exit` | Quit the program |

---

## Formula Syntax

You can type formulas using plain English words or symbols:

| Connective | Accepted inputs |
|---|---|
| Not | `not`, `!`, `~`, `¬` |
| And | `and`, `&`, `&&`, `∧` |
| Or | `or`, `\|`, `\|\|`, `∨` |
| Implies | `implies`, `->`, `→` |
| Iff | `iff`, `<->`, `↔` |
| Grouping | `(`, `)` |

Atoms can be any plain word: `p`, `q`, `r`, `rain`, `sunny`, `myAtom`.

### Examples

```text
revise not q
revise p and not q
revise p implies q
contract p or q
expand rain implies wet
revise (p and q) implies r
```

### Two things to know about parsing

1. **`implies` is right-associative** — `p implies q implies r` is parsed as `p implies (q implies r)`. This is the standard mathematical convention for propositional logic.

2. **Priority of new beliefs** — when you add a formula (via revise or expand), it gets priority = highest existing priority + 1. This means the newest information is always the most entrenched, which is the correct AGM behaviour.

---

## How It Works — Stage by Stage

### Stage 1: Belief Base
Each belief is stored as a formula + a priority number. Higher priority = more entrenched = harder to remove during contraction.

### Stage 2: Logical Entailment (Resolution)
To check if the base entails a formula φ (`B ⊨ φ`), we use **refutation by resolution**:
- Convert `B ∪ {¬φ}` to CNF (Conjunctive Normal Form)
- Repeatedly resolve clause pairs
- If the empty clause (⊥) is derived → φ is entailed
- If no new clauses can be derived → φ is not entailed

No external libraries are used — the CNF converter and resolution engine are implemented from scratch.

### Stage 3: Contraction (`B ÷ φ`)
Uses **Partial Meet Contraction**:
1. Find all *remainder sets* — maximal subsets of B that do NOT entail φ
2. Select the best ones using a priority-based selection function (keep whichever remainders preserve the highest-priority formulas)
3. Return the intersection of the selected remainders

### Stage 4: Expansion and Revision
- **Expansion** (`B + φ`): just add φ to B with the highest priority. May make the base inconsistent.
- **Revision** (`B * φ`): uses the **Levi Identity**:
  ```
  B * φ = (B ÷ ¬φ) + φ
  ```
  First contract by ¬φ (remove anything that conflicts), then expand by φ. Guarantees a consistent result when φ itself is consistent.

---

## Project Structure

```
BeliefRevision/
├── Program.cs          # Entry point: demo + interactive mode
├── Formula.cs          # AST nodes: Atom, Not, And, Or, Implies, Iff
├── BeliefBase.cs       # Belief base: stores formulas with priorities
├── Cnf.cs              # CNF conversion pipeline + Clause/Literal types
├── Resolution.cs       # Resolution-based entailment checker
├── Contraction.cs      # Partial meet contraction
├── Revision.cs         # Expansion and revision (Levi Identity)
├── AgmPostulates.cs    # AGM postulate verification (all 5)
└── Parser.cs           # Formula parser for interactive input
```

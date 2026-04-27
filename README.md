# Belief Revision Engine

A propositional logic belief revision engine built in C# (.NET 9). The agent holds a set of beliefs, each with a priority, and can revise, contract, or expand them when new information arrives.

---

## 1. Install .NET 9

You need the .NET 9 SDK. You only have to do this once.

**macOS**
```bash
brew install dotnet
```
Or download the installer directly: https://dotnet.microsoft.com/download/dotnet/9.0

After installing, verify:
```bash
dotnet --version   # should print 9.x.x
```

**Windows**
1. Go to https://dotnet.microsoft.com/download/dotnet/9.0
2. Download the **SDK** installer (not the Runtime)
3. Run the installer — it adds `dotnet` to your PATH automatically
4. Open a new terminal (PowerShell or Command Prompt) and verify:
```powershell
dotnet --version   # should print 9.x.x
```

---

## 2. Run the Project

```bash
# navigate to the project folder
cd path/to/BeliefRevision

# build and run
dotnet run
```

That's it. No packages to install, no extra setup.

---

## 3. What Happens When You Run It

The program runs in two phases back to back.

### Phase 1 — Automatic Demo

It runs a fixed demo using **Bob's belief base** (from the course lectures):

| Formula | Priority | Notes |
|---|---|---|
| `p` | 2 | Strongest belief |
| `q` | 1 | Medium belief |
| `p → ¬q` | 0 | Weakest belief |

This base is **intentionally inconsistent** — `p` and `q` together contradict `p → ¬q`.

The demo walks through each operation and prints the result:

1. **Contraction** `B ÷ q` — removes `q` using partial-meet contraction
2. **Expansion** `B + ¬q` — adds `¬q` directly (no conflict check)
3. **Revision** `B * ¬q` — uses the Levi Identity to revise safely
4. **AGM Postulate checks** — verifies all 5 postulates hold:
   - ✅ Success
   - ✅ Inclusion
   - ✅ Vacuity
   - ✅ Consistency
   - ✅ Extensionality

### Phase 2 — Interactive Mode

After the demo, you get a live prompt starting from Bob's base:

```
=== Interactive Mode ===
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

---

## 4. Interactive Commands

| Command | What it does |
|---|---|
| `revise <formula>` | Removes conflicts with φ, then adds φ (Levi Identity) |
| `contract <formula>` | Removes φ from the base, keeping as much as possible |
| `expand <formula>` | Adds φ directly — no conflict check |
| `print` | Prints the current base |
| `reset` | Wipes the base to empty |
| `exit` | Quits the program |

---

## 5. Formula Syntax

Use plain English words or standard symbols — both work:

| Connective | Accepted inputs |
|---|---|
| Not | `not` `!` `~` `¬` |
| And | `and` `&` `&&` `∧` |
| Or | `or` `|` `||` `∨` |
| Implies | `implies` `->` `→` |
| Iff | `iff` `<->` `↔` |
| Grouping | `(` `)` |

Atoms can be any plain word: `p`, `q`, `r`, `rain`, `sunny`, `alarm`.

**Examples:**
```
revise not q
revise p and not q
revise p implies q
revise (p or q) implies r
contract p or q
expand rain implies wet
```

> **Note:** `implies` is right-associative — `p implies q implies r` parses as `p implies (q implies r)`.  
> **Note:** When you add a formula via `revise` or `expand`, it gets priority = highest existing priority + 1.

---

## 6. Testing to the Limit

These scenarios push every part of the engine. Paste them into the interactive prompt in order.

### Test 1 — Build and destroy a consistent base
```
reset
expand p
expand q
expand p implies q
revise not p
print
```
Expected: base no longer contains `p` (revised away).

---

### Test 2 — Contraction vacuity (formula not in base)
```
reset
expand p
contract q
print
```
Expected: base unchanged — you can't contract something that isn't entailed.

---

### Test 3 — Contraction of a tautology
```
reset
expand p
contract p or not p
print
```
Expected: base unchanged — tautologies can't be contracted.

---

### Test 4 — Revise by contradiction (inconsistent input)
```
reset
expand p
revise p and not p
print
```
Expected: `p ∧ ¬p` is added. The AGM Consistency postulate vacuously holds here (it only promises consistency when the *input* φ is itself consistent).

---

### Test 5 — Chain of revisions, conflicting beliefs
```
reset
expand p
expand q
revise not p
revise not q
revise p and q
print
```
Expected: final base contains `p ∧ q` as the newest (highest-priority) belief.

---

### Test 6 — Deeply nested formula
```
reset
expand (p implies q) and (q implies r)
expand not r
revise p
print
```
Expected: after expanding `¬r` and `(p→q)∧(q→r)`, revising by `p` forces a consistent outcome.

---

### Test 7 — Biconditional and double negation
```
reset
expand p iff q
revise not not p
print
```
Expected: `¬¬p` is parsed correctly (equivalent to `p`) and revision works normally.

---

### Test 8 — Priority ordering matters
```
reset
expand p
expand q
expand p implies not q
contract q
print
```
The newest formula has the highest priority. Contraction should prefer removing the lowest-priority formula to satisfy the removal requirement.

---

### Test 9 — Longer atom names (real-world modelling)
```
reset
expand rain
expand rain implies wet
revise not wet
print
```
Expected: `rain` is contracted since believing `¬wet` conflicts with `rain → wet`.

---

### Test 10 — Stress: max base size
Contraction uses brute-force enumeration over all subsets, which is fast up to 15 entries and noticeable at 18–20. This pushes it near the limit:
```
reset
expand a
expand b
expand c
expand d
expand e
expand f
expand g
expand h
expand i
expand j
expand k
expand l
expand m
expand n
expand o
contract a
print
```
Expected: base with 14 remaining formulas. If you push past 20 entries and then try to `contract`, the engine will throw a clear error message — that is intentional and expected.

---

## 7. Project Structure

```
BeliefRevision/
├── Program.cs          # Entry point: demo + interactive mode
├── Formula.cs          # AST: Atom, Not, And, Or, Implies, Iff
├── BeliefBase.cs       # Stores formulas with priorities
├── Cnf.cs              # CNF conversion + Clause/Literal types
├── Resolution.cs       # Resolution-based entailment (no external packages)
├── Contraction.cs      # Partial-meet contraction
├── Revision.cs         # Expansion and revision (Levi Identity)
├── AgmPostulates.cs    # AGM postulate checks (all 5)
└── Parser.cs           # Formula parser for interactive input
```

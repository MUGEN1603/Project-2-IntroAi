using System;
using System.Collections.Generic;
using System.Linq;

namespace BeliefRevision
{
    // ========================================================================
    //  STAGE 2 — Clauses + CNF conversion
    //
    //  A Literal  = an atom or its negation.
    //  A Clause   = a DISJUNCTION of literals (represented as a set).
    //  A CNF set  = a CONJUNCTION of clauses    (represented as a set).
    //
    //  Conversion pipeline (applied in this order):
    //    1.  Eliminate ↔:  (a ↔ b)  ⇒  (a → b) ∧ (b → a)
    //    2.  Eliminate →:  (a → b)  ⇒  (¬a ∨ b)
    //    3.  Push ¬ inward (NNF, De Morgan):  ¬(a ∧ b) ⇒ (¬a ∨ ¬b), etc.
    //    4.  Distribute ∨ over ∧:  a ∨ (b ∧ c) ⇒ (a ∨ b) ∧ (a ∨ c)
    //    5.  Flatten into a set of clauses.
    // ========================================================================

    /// <summary>An atom or its negation.</summary>
    public readonly struct Literal : IEquatable<Literal>
    {
        public string Atom    { get; }
        public bool   Negated { get; }

        public Literal(string atom, bool negated)
        {
            Atom    = atom;
            Negated = negated;
        }

        public Literal Complement() => new(Atom, !Negated);

        public bool Equals(Literal other) => Atom == other.Atom && Negated == other.Negated;
        public override bool Equals(object obj) => obj is Literal l && Equals(l);
        public override int GetHashCode() => HashCode.Combine(Atom, Negated);
        public override string ToString() => Negated ? $"¬{Atom}" : Atom;
    }

    /// <summary>A disjunction of literals. Empty clause = ⊥ = contradiction.</summary>
    public sealed class Clause : IEquatable<Clause>
    {
        public HashSet<Literal> Literals { get; }

        public Clause(IEnumerable<Literal> literals)
        {
            Literals = new HashSet<Literal>(literals);
        }

        public bool IsEmpty     => Literals.Count == 0;                                    // ⊥
        public bool IsTautology => Literals.Any(l => Literals.Contains(l.Complement()));   // e.g. p ∨ ¬p

        public bool Equals(Clause other) => other is not null && Literals.SetEquals(other.Literals);
        public override bool Equals(object obj) => obj is Clause c && Equals(c);

        // Order-independent hash: XOR of literal hashes.
        public override int GetHashCode()
        {
            int h = 0;
            foreach (var l in Literals) h ^= l.GetHashCode();
            return h;
        }

        public override string ToString() =>
            IsEmpty ? "⊥" : "{" + string.Join(" ∨ ", Literals.Select(l => l.ToString())) + "}";
    }

    /// <summary>Static CNF conversion pipeline.</summary>
    public static class Cnf
    {
        /// <summary>Full pipeline: Formula → set of clauses (CNF).</summary>
        public static HashSet<Clause> ToClauses(Formula f)
        {
            // ORDER IS CRITICAL: EliminateIff → EliminateImplies → Nnf → Distribute
            // Changing this order will cause CollectDisjuncts to throw.
            var cnf = Distribute(Nnf(EliminateImplies(EliminateIff(f))));
            var clauses = new HashSet<Clause>();
            CollectConjuncts(cnf, clauses);
            return clauses;
        }

        // ---- Step 1: eliminate ↔ ----
        public static Formula EliminateIff(Formula f) => f switch
        {
            Iff x     => new And(
                            new Implies(EliminateIff(x.Left),  EliminateIff(x.Right)),
                            new Implies(EliminateIff(x.Right), EliminateIff(x.Left))),
            Not x     => new Not(EliminateIff(x.Sub)),
            And x     => new And(EliminateIff(x.Left), EliminateIff(x.Right)),
            Or  x     => new Or (EliminateIff(x.Left), EliminateIff(x.Right)),
            Implies x => new Implies(EliminateIff(x.Left), EliminateIff(x.Right)),
            _         => f
        };

        // ---- Step 2: eliminate → ----
        public static Formula EliminateImplies(Formula f) => f switch
        {
            Implies x => new Or(new Not(EliminateImplies(x.Left)), EliminateImplies(x.Right)),
            Not x     => new Not(EliminateImplies(x.Sub)),
            And x     => new And(EliminateImplies(x.Left), EliminateImplies(x.Right)),
            Or  x     => new Or (EliminateImplies(x.Left), EliminateImplies(x.Right)),
            _         => f
        };

        // ---- Step 3: push negations inward (Negation Normal Form) ----
        // Precondition: no Iff or Implies remain.
        public static Formula Nnf(Formula f) => f switch
        {
            Not n => n.Sub switch
            {
                Not nn => Nnf(nn.Sub),                                         // ¬¬a ≡ a
                And a  => new Or (Nnf(new Not(a.Left)), Nnf(new Not(a.Right))), // ¬(a∧b) ≡ ¬a∨¬b
                Or  o  => new And(Nnf(new Not(o.Left)), Nnf(new Not(o.Right))), // ¬(a∨b) ≡ ¬a∧¬b
                _      => f                                                    // ¬atom stays
            },
            And x => new And(Nnf(x.Left), Nnf(x.Right)),
            Or  x => new Or (Nnf(x.Left), Nnf(x.Right)),
            _     => f
        };

        // ---- Step 4: distribute ∨ over ∧ ----
        // Precondition: formula is in NNF.
        public static Formula Distribute(Formula f)
        {
            f = f switch
            {
                And a => new And(Distribute(a.Left), Distribute(a.Right)),
                Or  o => new Or (Distribute(o.Left), Distribute(o.Right)),
                _     => f
            };

            if (f is Or or)
            {
                if (or.Left is And la)
                    return Distribute(new And(new Or(la.Left,  or.Right),
                                              new Or(la.Right, or.Right)));
                if (or.Right is And ra)
                    return Distribute(new And(new Or(or.Left, ra.Left),
                                              new Or(or.Left, ra.Right)));
            }
            return f;
        }

        // ---- Step 5: flatten a CNF formula into a set of clauses ----
        static void CollectConjuncts(Formula f, HashSet<Clause> acc)
        {
            if (f is And a)
            {
                CollectConjuncts(a.Left,  acc);
                CollectConjuncts(a.Right, acc);
            }
            else
            {
                var lits = new HashSet<Literal>();
                CollectDisjuncts(f, lits);
                acc.Add(new Clause(lits));
            }
        }

        static void CollectDisjuncts(Formula f, HashSet<Literal> acc)
        {
            switch (f)
            {
                case Or o:
                    CollectDisjuncts(o.Left,  acc);
                    CollectDisjuncts(o.Right, acc);
                    break;
                case Atom a:
                    acc.Add(new Literal(a.Name, negated: false));
                    break;
                case Not n when n.Sub is Atom na:
                    acc.Add(new Literal(na.Name, negated: true));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Formula not in CNF at disjunct position: {f}");
            }
        }
    }
}
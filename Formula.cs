using System;
using System.Collections.Generic;

namespace BeliefRevision
{
    // ========================================================================
    //  STAGE 1 — Propositional-logic formula AST
    //
    //  A Formula is built inductively from atoms and the connectives
    //  ¬, ∧, ∨, →, ↔. All formulas are IMMUTABLE, with value equality, so
    //  they can be used safely as dictionary keys, put in HashSets, etc.
    // ========================================================================

    public abstract class Formula
    {
        /// <summary>Set of atomic proposition names occurring in this formula.</summary>
        public abstract HashSet<string> Atoms();

        /// <summary>Evaluate under a truth assignment (atom name → bool).</summary>
        public abstract bool Evaluate(IDictionary<string, bool> assignment);

        // Value equality — two formulas with the same shape are equal.
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
        public abstract override string ToString();
    }

    /// <summary>Atomic proposition, e.g. p, q, r.</summary>
    public sealed class Atom : Formula
    {
        public string Name { get; }
        public Atom(string name) => Name = name;

        public override HashSet<string> Atoms() => new() { Name };
        public override bool Evaluate(IDictionary<string, bool> a) => a[Name];
        public override string ToString() => Name;
        public override bool Equals(object o) => o is Atom x && x.Name == Name;
        public override int GetHashCode() => Name.GetHashCode();
    }

    /// <summary>Negation: ¬φ</summary>
    public sealed class Not : Formula
    {
        public Formula Sub { get; }
        public Not(Formula sub) => Sub = sub;

        public override HashSet<string> Atoms() => Sub.Atoms();
        public override bool Evaluate(IDictionary<string, bool> a) => !Sub.Evaluate(a);
        public override string ToString() => $"¬{Sub}";
        public override bool Equals(object o) => o is Not x && x.Sub.Equals(Sub);
        public override int GetHashCode() => HashCode.Combine("Not", Sub);
    }

    /// <summary>Shared base for the four binary connectives.</summary>
    public abstract class BinOp : Formula
    {
        public Formula Left  { get; }
        public Formula Right { get; }
        protected BinOp(Formula l, Formula r) { Left = l; Right = r; }

        public override HashSet<string> Atoms()
        {
            var s = Left.Atoms();
            s.UnionWith(Right.Atoms());
            return s;
        }
    }

    /// <summary>Conjunction: (φ ∧ ψ)</summary>
    public sealed class And : BinOp
    {
        public And(Formula l, Formula r) : base(l, r) {}
        public override bool Evaluate(IDictionary<string, bool> a) => Left.Evaluate(a) && Right.Evaluate(a);
        
        public override string ToString() => $"({Left} ∧ {Right})";
        public override bool Equals(object o) => o is And x && x.Left.Equals(Left) && x.Right.Equals(Right);
        public override int GetHashCode() => HashCode.Combine("And", Left, Right);
    }

    /// <summary>Disjunction: (φ ∨ ψ)</summary>
    public sealed class Or : BinOp
    {
        public Or(Formula l, Formula r) : base(l, r) {}
        public override bool Evaluate(IDictionary<string, bool> a) => Left.Evaluate(a) || Right.Evaluate(a);
        public override string ToString() => $"({Left} ∨ {Right})";
        public override bool Equals(object o) => o is Or x && x.Left.Equals(Left) && x.Right.Equals(Right);
        public override int GetHashCode() => HashCode.Combine("Or", Left, Right);
    }

    /// <summary>Implication: (φ → ψ)</summary>
    public sealed class Implies : BinOp
    {
        public Implies(Formula l, Formula r) : base(l, r) {}
        public override bool Evaluate(IDictionary<string, bool> a) => !Left.Evaluate(a) || Right.Evaluate(a);
        public override string ToString() => $"({Left} → {Right})";
        public override bool Equals(object o) => o is Implies x && x.Left.Equals(Left) && x.Right.Equals(Right);
        public override int GetHashCode() => HashCode.Combine("Implies", Left, Right);
    }

    /// <summary>Biconditional: (φ ↔ ψ)</summary>
    public sealed class Iff : BinOp
    {
        public Iff(Formula l, Formula r) : base(l, r) {}
        public override bool Evaluate(IDictionary<string, bool> a) => Left.Evaluate(a) == Right.Evaluate(a);
        public override string ToString() => $"({Left} ↔ {Right})";
        public override bool Equals(object o) => o is Iff x && x.Left.Equals(Left) && x.Right.Equals(Right);
        public override int GetHashCode() => HashCode.Combine("Iff", Left, Right);
    }

    /// <summary>Convenience factory — use F.Implies(p, F.Not(q)) instead of constructors.</summary>
    public static class F
    {
        public static Atom    P(string name)                  => new(name);
        public static Not     Not(Formula f)                  => new(f);
        public static And     And(Formula l, Formula r)       => new(l, r);
        public static Or      Or(Formula l, Formula r)        => new(l, r);
        public static Implies Implies(Formula l, Formula r)   => new(l, r);
        public static Iff     Iff(Formula l, Formula r)       => new(l, r);
    }
}
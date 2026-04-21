using System.Collections.Generic;
using System.Linq;

namespace BeliefRevision
{
    // ========================================================================
    //  STAGE 1 — Belief Base   (extended in Stage 2 with Entails/IsConsistent)
    // bigger number = stronger belief
    //smaller number = weaker belief
    // ========================================================================

    /// <summary>Belief-base entry: a formula together with its priority.</summary>
    public sealed class BeliefEntry
    {
        public Formula Formula  { get; }
        public int     Priority { get; }   // higher = more entrenched

        public BeliefEntry(Formula formula, int priority)
        {
            Formula  = formula;
            Priority = priority;
        }

        public override string ToString() => $"[p={Priority}] {Formula}";
    }

    public sealed class BeliefBase
    {
        private readonly List<BeliefEntry> entries = new();

        public IReadOnlyList<BeliefEntry> Entries => entries;
        public int Count => entries.Count;

        /// <summary>Add a formula with a priority. Syntactic duplicates are skipped.</summary>
        /// <remarks>Note: This only checks syntactic equality. Adding a logically equivalent formula
        /// (e.g. p -> q vs ¬p ∨ q) will result in a duplicate entry, potentially duplicating priority weight.</remarks>
        public void Add(Formula formula, int priority = 0)
        {
            if (entries.Any(e => e.Formula.Equals(formula))) return;
            entries.Add(new BeliefEntry(formula, priority));
        }

        public bool Remove(Formula formula)
        {
            int i = entries.FindIndex(e => e.Formula.Equals(formula));
            if (i < 0) return false;
            entries.RemoveAt(i);
            return true;
        }

        public bool Contains(Formula formula) =>
            entries.Any(e => e.Formula.Equals(formula));

        
        /*
         Why?
        Because belief revision should usually produce a new base, not destroy the original one.

        That matches the assignment idea that the output should be the resulting/new belief base.
    */
        public BeliefBase Copy()
        {
            var b = new BeliefBase();
            foreach (var e in entries) b.Add(e.Formula, e.Priority);
            return b;
        }

        // --------  Stage 2 convenience  --------

        /// <summary>Plain formulas (without priorities) for the reasoner.</summary>
        public IEnumerable<Formula> Formulas() => entries.Select(e => e.Formula);

        /// <summary>Does the base entail phi?   B ⊨ φ ?</summary>
        public bool Entails(Formula phi) => Resolution.Entails(Formulas(), phi);

        /// <summary>Is the base consistent (does NOT derive ⊥)?</summary>
        public bool IsConsistent() => Resolution.IsConsistent(Formulas());

        public override string ToString() =>
            entries.Count == 0 ? "{ }" : "{ " + string.Join(", ", entries) + " }";
    }
}
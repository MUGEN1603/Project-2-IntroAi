using System.Collections.Generic;
using System.Linq;

namespace BeliefRevision
{
    // ========================================================================
    //  STAGE 2 — Resolution-based entailment
    //
    //  Refutation principle:  B ⊨ φ   iff   B ∪ {¬φ}  is UNSAT.
    //
    //  Algorithm (Robinson, 1965):
    //    1. Convert B ∪ {¬φ} to a set of clauses S.
    //    2. Repeatedly compute resolvents of every pair of clauses in S.
    //    3. If the EMPTY clause ⊥ is derived  →  UNSAT  →  B ⊨ φ.
    //    4. If saturation is reached (no new clauses) →  SAT  →  B ⊭ φ.
    //
    //  Sound and complete for propositional logic; termination is guaranteed
    //  because there are only finitely many clauses over a finite atom set.
    // ========================================================================

    public static class Resolution
    {
        /// <summary>Does the belief base entail the query?  B ⊨ φ ?</summary>
        public static bool Entails(IEnumerable<Formula> beliefBase, Formula query)
        {
            // Build S = CNF(B) ∪ CNF(¬query).
            var clauses = new HashSet<Clause>();

            foreach (var f in beliefBase)
                AddClauses(Cnf.ToClauses(f), clauses);

            AddClauses(Cnf.ToClauses(new Not(query)), clauses);

            return IsUnsat(clauses);
        }

        /// <summary>Is the belief base consistent?  True iff B ⊭ ⊥.</summary>
        public static bool IsConsistent(IEnumerable<Formula> beliefBase)
        {
            var clauses = new HashSet<Clause>();
            foreach (var f in beliefBase)
                AddClauses(Cnf.ToClauses(f), clauses);

            return !IsUnsat(clauses);
        }

        /// <summary>Core resolution-refutation procedure.
        /// Returns true iff the empty clause can be derived from S.</summary>
        public static bool IsUnsat(HashSet<Clause> initialClauses)
        {
            // Early exit: ⊥ already present?
            if (initialClauses.Any(c => c.IsEmpty)) return true;

            var clauses = new HashSet<Clause>(initialClauses);

            while (true)
            {
                var newClauses = new HashSet<Clause>();
                var list = clauses.ToList();

                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        foreach (var resolvent in Resolvents(list[i], list[j]))
                        {
                            if (resolvent.IsEmpty) return true;     // ⊥ derived
                            if (resolvent.IsTautology) continue;     // useless, skip
                            newClauses.Add(resolvent);
                        }
                    }
                }

                // Saturation: no genuinely new clause was produced.
                if (newClauses.IsSubsetOf(clauses)) return false;

                clauses.UnionWith(newClauses);
            }
        }

        /// <summary>All resolvents of two clauses (one per complementary pair).</summary>
        static IEnumerable<Clause> Resolvents(Clause c1, Clause c2)
        {
            foreach (var lit in c1.Literals)
            {
                if (!c2.Literals.Contains(lit.Complement())) continue;

                var merged = new HashSet<Literal>(c1.Literals);
                merged.UnionWith(c2.Literals);
                merged.Remove(lit);
                merged.Remove(lit.Complement());

                yield return new Clause(merged);
            }
        }

        /// <summary>Add a batch of clauses, dropping tautologies.</summary>
        static void AddClauses(HashSet<Clause> source, HashSet<Clause> target)
        {
            foreach (var c in source)
                if (!c.IsTautology) target.Add(c);
        }
    }
}
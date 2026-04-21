using System;
using System.Collections.Generic;
using System.Linq;

namespace BeliefRevision
{
    // ========================================================================
    //  STAGE 3 — Partial-meet contraction with priority-based selection
    //
    //  B ÷ φ   "remove φ from B"
    //
    //   1. Compute the remainder set  B ⊥ φ :
    //      all maximal subsets X ⊆ B with X ⊭ φ.
    //   2. Selection γ picks the remainders that keep the highest-priority
    //      formulas (lexicographic on priority-level counts).
    //   3. The contraction is the INTERSECTION of the selected remainders:
    //              B ÷ φ  =  ⋂ γ(B ⊥ φ)
    //
    //  Special cases:
    //     • B ⊭ φ          ⇒   B ÷ φ = B   (vacuity)
    //     • φ is a tautology ⇒ B ÷ φ = B   (nothing to remove)
    // ========================================================================

    public static class Contraction
    {
        /// <summary>Partial-meet contraction of B by phi, selected by priority.</summary>
        public static BeliefBase Contract(BeliefBase B, Formula phi)
        {
            // Shortcut 1 — vacuity: nothing to remove if phi is not entailed.
            if (!B.Entails(phi)) return B.Copy();

            var remainders = ComputeRemainders(B, phi);

            // Shortcut 2 — phi is a tautology: no remainder exists.
            if (remainders.Count == 0) return B.Copy();

            var selected = SelectByPriority(B, remainders);

            return IntersectRemainders(B, selected);
        }

        // ------------------------------------------------------------------
        //  Step 1: enumerate remainders via bitmask over the base entries.
        //  A subset is a remainder iff it does not entail phi AND adding any
        //  missing formula from B WOULD entail phi (maximality).
        // ------------------------------------------------------------------
        internal static List<List<BeliefEntry>> ComputeRemainders(BeliefBase B, Formula phi)
        {
            var entries = B.Entries.ToList();
            int n = entries.Count;

            if (n > 20)
                throw new InvalidOperationException(
                    "Base too large for brute-force remainder enumeration.");

            var remainders = new List<List<BeliefEntry>>();

            for (int mask = 0; mask < (1 << n); mask++)
            {
                var subset = Subset(entries, mask);
                var formulas = subset.Select(e => e.Formula).ToList();

                // 1a. Must NOT entail phi.
                if (Resolution.Entails(formulas, phi)) continue;

                // 1b. Must be MAXIMAL: adding any absent entry creates entailment.
                bool maximal = true;
                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) != 0) continue;      // i already in subset

                    var expanded = new List<Formula>(formulas) { entries[i].Formula };
                    if (!Resolution.Entails(expanded, phi))
                    {
                        maximal = false;                        // i could be added → not maximal
                        break;
                    }
                }

                if (maximal) remainders.Add(subset);
            }

            return remainders;
        }

        // ------------------------------------------------------------------
        //  Step 2: keep only remainders with the best priority profile.
        //  Profile = (count of highest-priority formulas,
        //            count of next-highest, ..., count of lowest).
        //  Compared lexicographically, larger is better.
        // ------------------------------------------------------------------
        internal static List<List<BeliefEntry>> SelectByPriority(
            BeliefBase B,
            List<List<BeliefEntry>> remainders)
        {
            if (remainders.Count == 0) return remainders; // defensive guard

            var prioritiesDesc = B.Entries
                                   .Select(e => e.Priority)
                                   .Distinct()
                                   .OrderByDescending(p => p)
                                   .ToArray();

            int[] Profile(List<BeliefEntry> r) =>
                prioritiesDesc.Select(p => r.Count(e => e.Priority == p)).ToArray();

            int[] best = remainders
                .Select(Profile)
                .Aggregate((a, b) => LexCompare(a, b) >= 0 ? a : b);

            return remainders.Where(r => LexCompare(Profile(r), best) == 0).ToList();
        }

        // ------------------------------------------------------------------
        //  Step 3: intersect selected remainders.
        //  Keep an entry of B only if it appears in every selected remainder.
        //  Priority is preserved from the original base.
        // ------------------------------------------------------------------
        static BeliefBase IntersectRemainders(
            BeliefBase B,
            List<List<BeliefEntry>> selected)
        {
            var result = new BeliefBase();

            foreach (var entry in B.Entries)
            {
                bool inAll = selected.All(r => r.Any(e => e.Formula.Equals(entry.Formula)));
                if (inAll) result.Add(entry.Formula, entry.Priority);
            }

            return result;
        }

        // ------------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------------
        static List<BeliefEntry> Subset(List<BeliefEntry> entries, int mask)
        {
            var s = new List<BeliefEntry>();
            for (int i = 0; i < entries.Count; i++)
                if ((mask & (1 << i)) != 0) s.Add(entries[i]);
            return s;
        }

        static int LexCompare(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return a[i].CompareTo(b[i]);
            return 0;
        }
    }
}
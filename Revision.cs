using System.Linq;

namespace BeliefRevision
{
    // ========================================================================
    //  STAGE 4 — Expansion and Revision
    //
    //   Expansion:  B + φ  just adds φ to B (may become inconsistent).
    //   Revision:   B * φ  uses the Levi Identity:
    //
    //             B * φ  :=  (B ÷ ¬φ)  +  φ
    //
    //   Contract by ¬φ first (so the base is no longer hostile to φ),
    //   then expand by φ.  Result is consistent whenever φ is consistent.
    // ========================================================================

    public static class Revision
    {
        /// <summary>Priority used for a newly-added formula — higher than everything in B.</summary>
        static int DefaultNewPriority(BeliefBase B) =>
            B.Count == 0 ? 1 : B.Entries.Max(e => e.Priority) + 1;

        /// <summary>Expansion:  B + φ  (no consistency check).</summary>
        public static BeliefBase Expand(BeliefBase B, Formula phi, int? priority = null)
        {
            var result = B.Copy();
            result.Add(phi, priority ?? DefaultNewPriority(B));
            return result;
        }

        /// <summary>Revision via Levi Identity:  B * φ  :=  (B ÷ ¬φ) + φ.</summary>
        public static BeliefBase Revise(BeliefBase B, Formula phi, int? priority = null)
        {
            var contracted = Contraction.Contract(B, new Not(phi));
            return Expand(contracted, phi, priority);
        }
    }
}
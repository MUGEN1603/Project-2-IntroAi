namespace BeliefRevision
{
    // ========================================================================
    //  STAGE 4 — AGM postulate tests
    //
    //  Each check returns TRUE when the postulate holds for the given (B, φ).
    //  Where a postulate has the form  "if X then Y",  the method returns
    //  true vacuously when X is false (that's correct logical behaviour).
    //
    //  The postulates are stated on the DEDUCTIVE CLOSURE, so we compare
    //  bases by mutual entailment, not by their syntactic entries.
    // ========================================================================

    public static class AgmPostulates
    {
        // ---------- helpers ----------

        /// <summary>φ and ψ entail each other (⊨ φ ↔ ψ).</summary>
        public static bool LogicallyEquivalent(Formula phi, Formula psi)
        {
            var fromPhi = new[] { phi };
            var fromPsi = new[] { psi };
            return Resolution.Entails(fromPhi, psi) &&
                   Resolution.Entails(fromPsi, phi);
        }

        /// <summary>Two bases have the same deductive closure:
        /// every entry of each is entailed by the other.</summary>
        public static bool SameClosure(BeliefBase A, BeliefBase B)
        {
            foreach (var e in A.Entries)
                if (!B.Entails(e.Formula)) return false;
            foreach (var e in B.Entries)
                if (!A.Entails(e.Formula)) return false;
            return true;
        }

        // ---------- the five postulates ----------

        /// <summary>K*2 Success:  φ ∈ Cn(B * φ).</summary>
        public static bool Success(BeliefBase B, Formula phi) =>
            Revision.Revise(B, phi).Entails(phi);

        /// <summary>K*3 Inclusion:  Cn(B * φ) ⊆ Cn(B + φ).</summary>
        public static bool Inclusion(BeliefBase B, Formula phi)
        {
            var revised  = Revision.Revise(B, phi);
            var expanded = Revision.Expand(B, phi);

            foreach (var e in revised.Entries)
                if (!expanded.Entails(e.Formula)) return false;
            return true;
        }

        /// <summary>K*4 Vacuity:  if ¬φ ∉ Cn(B), then Cn(B * φ) = Cn(B + φ).</summary>
        public static bool Vacuity(BeliefBase B, Formula phi)
        {
            // Antecedent false  →  postulate holds vacuously.
            if (B.Entails(new Not(phi))) return true;

            return SameClosure(Revision.Revise(B, phi),
                               Revision.Expand(B, phi));
        }

        /// <summary>K*5 Consistency:  if φ is consistent, then B * φ is consistent.</summary>
        public static bool Consistency(BeliefBase B, Formula phi)
        {
            // If φ alone is inconsistent, the postulate makes no promise.
            if (!Resolution.IsConsistent(new[] { phi })) return true;

            return Revision.Revise(B, phi).IsConsistent();
        }

        /// <summary>K*6 Extensionality:  if ⊨ φ ↔ ψ, then Cn(B * φ) = Cn(B * ψ).</summary>
        public static bool Extensionality(BeliefBase B, Formula phi, Formula psi)
        {
            if (!LogicallyEquivalent(phi, psi)) return true;

            return SameClosure(Revision.Revise(B, phi),
                               Revision.Revise(B, psi));
        }
    }
}
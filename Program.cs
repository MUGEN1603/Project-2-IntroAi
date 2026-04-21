using System;
using System.Linq;

namespace BeliefRevision
{
    // Full demo: Stages 1–4 and AGM postulate checks.
    internal class Program
    {
        static void Main()
        {
            var p = F.P("p");
            var q = F.P("q");
            var r = F.P("r");

            
            // ========================================================
            //  Bob's belief base — from Lecture 9
            //  B = { p : 2, q : 1, p → ¬q : 0 }    (inconsistent)
            // ========================================================
            var Bob = new BeliefBase();
            
            //Priority says how strongly this belief is kept.
            //higher priority = more entrenched
            Bob.Add(p,                       priority: 2);
            Bob.Add(q,                       priority: 1);
            Bob.Add(F.Implies(p, F.Not(q)),  priority: 0);

            Header("Bob's belief base");
            Print("B", Bob);
            Console.WriteLine($"  consistent? {Bob.IsConsistent()}");

            // ========================================================
            //  Stage 3 — Contraction
            // ========================================================
            Header("Contraction");
            var B_contract_q = Contraction.Contract(Bob, q);
            Print("B ÷ q", B_contract_q);
            Console.WriteLine($"  entails q?  {B_contract_q.Entails(q)}  (expect False)");

            // ========================================================
            //  Stage 4 — Expansion and Revision
            // ========================================================
            Header("Expansion");
            var B_expand_not_q = Revision.Expand(Bob, F.Not(q));
            Print("B + ¬q", B_expand_not_q);
            Console.WriteLine($"  consistent? {B_expand_not_q.IsConsistent()}  (expansion doesn't check)");

            Header("Revision via Levi Identity");
            var B_revise_not_q = Revision.Revise(Bob, F.Not(q));
            Print("B * ¬q", B_revise_not_q);
            Console.WriteLine($"  consistent? {B_revise_not_q.IsConsistent()}  (expect True)");
            Console.WriteLine($"  entails ¬q? {B_revise_not_q.Entails(F.Not(q))}  (expect True)");

            var B_revise_r = Revision.Revise(Bob, r);
            Print("B * r", B_revise_r);
            Console.WriteLine($"  consistent? {B_revise_r.IsConsistent()}  (expect True)");

            // ========================================================
            //  AGM postulates
            // ========================================================
            Header("AGM postulate tests");

            // Use a few scenarios to exercise all five.
            CheckAll(Bob, F.Not(q),  "Bob revised by ¬q");
            CheckAll(Bob, r,         "Bob revised by r  (no conflict → Vacuity expected)");
            CheckAll(Bob, F.And(p, F.Not(p)), "Bob revised by (p ∧ ¬p)  (inconsistent input)");

            // Extensionality: q  vs.  ¬¬q
            Console.WriteLine();
            Console.WriteLine("Extensionality: revise Bob by q  vs.  ¬¬q  (logically equivalent)");
            bool ext = AgmPostulates.Extensionality(Bob, q, F.Not(F.Not(q)));
            Console.WriteLine($"  holds? {ext}  (expect True)");

            // ========================================================
            //  Interactive Mode
            // ========================================================
            InteractiveMode(Bob);
        }

        // --------------------------------------------------------------
        //  Helpers
        // --------------------------------------------------------------
        static void Header(string title)
        {
            Console.WriteLine();
            Console.WriteLine("───  " + title + "  ───");
        }

        static void Print(string name, BeliefBase B)
        {
            Console.WriteLine($"{name} = {B}");
        }

        static void CheckAll(BeliefBase B, Formula phi, string label)
        {
            Console.WriteLine();
            Console.WriteLine($"[{label}]   φ = {phi}");
            Console.WriteLine($"  Success        : {AgmPostulates.Success(B, phi)}");
            Console.WriteLine($"  Inclusion      : {AgmPostulates.Inclusion(B, phi)}");
            Console.WriteLine($"  Vacuity        : {AgmPostulates.Vacuity(B, phi)}");
            Console.WriteLine($"  Consistency    : {AgmPostulates.Consistency(B, phi)}");
            var doubleNeg = F.Not(F.Not(phi));
            Console.WriteLine($"  Extensionality : {AgmPostulates.Extensionality(B, phi, doubleNeg)}  (φ vs ¬¬φ)");
        }

        static void InteractiveMode(BeliefBase initialBase)
        {
            Console.WriteLine("\n=== Interactive Mode ===");
            Console.WriteLine("Starting from Bob's belief base (pre-loaded above).");
            Console.WriteLine("Type 'reset' to start from an empty base.");
            Console.WriteLine("Commands: revise <formula>, contract <formula>, expand <formula>, print, reset, exit");
            Console.WriteLine("Supported syntax: atoms (p, q, myAtom), not, and, or, implies, iff, and parentheses.");
            Console.WriteLine("Example: revise p and not q");

            var currentBase = initialBase.Copy();
            
            while (true)
            {
                Console.WriteLine($"\nCurrent Base: {currentBase}");
                Console.Write("> ");
                var input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input)) continue;
                var trimmed = input.Trim();
                if (trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
                if (trimmed.Equals("reset", StringComparison.OrdinalIgnoreCase))
                {
                    currentBase = new BeliefBase();
                    Console.WriteLine("Base reset to empty.");
                    continue;
                }
                if (trimmed.Equals("print", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Base: {currentBase}");
                    continue;
                }

                int spaceIndex = trimmed.IndexOf(' ');
                if (spaceIndex < 0)
                {
                    Console.WriteLine("Unknown command. Use: revise <formula>, contract <formula>, expand <formula>, print, or exit");
                    continue;
                }

                string command = trimmed.Substring(0, spaceIndex).ToLowerInvariant();
                string formulaStr = trimmed.Substring(spaceIndex + 1);

                try
                {
                    var formula = Parser.Parse(formulaStr);
                    Console.WriteLine($"Parsed formula: {formula}");

                    switch (command)
                    {
                        case "revise":
                            currentBase = Revision.Revise(currentBase, formula);
                            Console.WriteLine("Revision successful.");
                            break;
                        case "contract":
                            currentBase = Contraction.Contract(currentBase, formula);
                            Console.WriteLine("Contraction successful.");
                            break;
                        case "expand":
                            currentBase = Revision.Expand(currentBase, formula);
                            Console.WriteLine("Expansion successful.");
                            break;
                        default:
                            Console.WriteLine($"Unknown command: {command}. Expected 'revise', 'contract', or 'expand'.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing command: {ex.Message}");
                }
            }
        }
    }
}
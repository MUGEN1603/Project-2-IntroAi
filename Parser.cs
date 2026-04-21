using System;
using System.Collections.Generic;

namespace BeliefRevision
{
    public static class Parser
    {
        public static Formula Parse(string input)
        {
            var tokens = Tokenize(input);
            int pos = 0;
            var result = ParseIff(tokens, ref pos);
            if (pos < tokens.Count)
                throw new Exception($"Unexpected token at end: {tokens[pos]}");
            return result;
        }

        private static Formula ParseIff(List<string> tokens, ref int pos)
        {
            var left = ParseImplies(tokens, ref pos);
            while (pos < tokens.Count && (tokens[pos] == "iff" || tokens[pos] == "<->" || tokens[pos] == "↔"))
            {
                pos++;
                var right = ParseImplies(tokens, ref pos);
                left = F.Iff(left, right);
            }
            return left;
        }

        // Implication is RIGHT-associative by convention in propositional logic:
        // "p implies q implies r" parses as "p implies (q implies r)".
        // Achieved with a recursive call on the right operand instead of a loop.
        private static Formula ParseImplies(List<string> tokens, ref int pos)
        {
            var left = ParseOr(tokens, ref pos);
            if (pos < tokens.Count && (tokens[pos] == "implies" || tokens[pos] == "->" || tokens[pos] == "→"))
            {
                pos++;
                var right = ParseImplies(tokens, ref pos); // right-recursive = right-associative
                return F.Implies(left, right);
            }
            return left;
        }

        private static Formula ParseOr(List<string> tokens, ref int pos)
        {
            var left = ParseAnd(tokens, ref pos);
            while (pos < tokens.Count && (tokens[pos] == "or" || tokens[pos] == "|" || tokens[pos] == "||" || tokens[pos] == "∨"))
            {
                pos++;
                var right = ParseAnd(tokens, ref pos);
                left = F.Or(left, right);
            }
            return left;
        }

        private static Formula ParseAnd(List<string> tokens, ref int pos)
        {
            var left = ParseUnary(tokens, ref pos);
            while (pos < tokens.Count && (tokens[pos] == "and" || tokens[pos] == "&" || tokens[pos] == "&&" || tokens[pos] == "∧"))
            {
                pos++;
                var right = ParseUnary(tokens, ref pos);
                left = F.And(left, right);
            }
            return left;
        }

        private static Formula ParseUnary(List<string> tokens, ref int pos)
        {
            if (pos < tokens.Count && (tokens[pos] == "not" || tokens[pos] == "!" || tokens[pos] == "~" || tokens[pos] == "¬"))
            {
                pos++;
                return F.Not(ParseUnary(tokens, ref pos));
            }
            return ParsePrimary(tokens, ref pos);
        }

        private static Formula ParsePrimary(List<string> tokens, ref int pos)
        {
            if (pos >= tokens.Count)
                throw new Exception("Unexpected end of formula");

            var token = tokens[pos++];
            if (token == "(")
            {
                var expr = ParseIff(tokens, ref pos);
                if (pos >= tokens.Count || tokens[pos] != ")")
                    throw new Exception("Expected closing parenthesis ')'");
                pos++;
                return expr;
            }
            
            // Guard against operator keywords becoming atoms
            string[] keywords = { "not", "and", "or", "implies", "iff", "!", "~", "¬", "&", "&&", "∧", "|", "||", "∨", "->", "→", "<->", "↔" };
            if (Array.IndexOf(keywords, token) >= 0)
                throw new Exception($"Keyword '{token}' is not a valid atom name.");

            // Assume it's an atom
            return F.P(token);
        }

        private static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            var span = input.AsSpan();
            int i = 0;

            while (i < span.Length)
            {
                if (char.IsWhiteSpace(span[i]))
                {
                    i++;
                    continue;
                }

                if (span[i] == '(' || span[i] == ')')
                {
                    tokens.Add(span[i].ToString());
                    i++;
                    continue;
                }

                // Peek for `<->`
                if (i + 2 < span.Length && span.Slice(i, 3).SequenceEqual("<->"))
                {
                    tokens.Add("<->");
                    i += 3;
                    continue;
                }
                
                // Peek for `->`
                if (i + 1 < span.Length && span.Slice(i, 2).SequenceEqual("->"))
                {
                    tokens.Add("->");
                    i += 2;
                    continue;
                }

                // Peek for `&&`
                if (i + 1 < span.Length && span.Slice(i, 2).SequenceEqual("&&"))
                {
                    tokens.Add("&&");
                    i += 2;
                    continue;
                }

                // Peek for `||`
                if (i + 1 < span.Length && span.Slice(i, 2).SequenceEqual("||"))
                {
                    tokens.Add("||");
                    i += 2;
                    continue;
                }

                if (span[i] == '!' || span[i] == '~' || span[i] == '¬' || span[i] == '&' || span[i] == '|' || span[i] == '∧' || span[i] == '∨' || span[i] == '→' || span[i] == '↔')
                {
                    tokens.Add(span[i].ToString());
                    i++;
                    continue;
                }

                // Parse word (identifier or text operator)
                int start = i;
                while (i < span.Length && char.IsLetterOrDigit(span[i]))
                    i++;

                if (i > start)
                {
                    tokens.Add(span.Slice(start, i - start).ToString());
                }
                else
                {
                    // Unexpected char
                    tokens.Add(span[i].ToString());
                    i++;
                }
            }

            return tokens;
        }
    }
}

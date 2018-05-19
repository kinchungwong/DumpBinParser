using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser.Utility
{
    /// <summary>
    /// <para>
    /// Parses brackets in a C++ function prototype (declarations).
    /// </para>
    /// <para>
    /// This class requires that these four bracket markers are balanced:
    /// Round brackets, Curly braces, Square brackets, Angled brackets.
    /// </para>
    /// </summary>
    public class BalancedBracketParser
    {
        public string Text
        {
            get;
        }

        public class BracketPair
        {
            public char OpenChar { get; set; }
            public char CloseChar { get; set; }
            public int OpenIndex { get; set; }
            public int CloseIndex { get; set; }
            public int NestLevel { get; set; }
        }

        public List<BracketPair> BracketPairs
        {
            get;
        } = new List<BracketPair>();

        public List<Exception> Exceptions
        {
            get;
        } = new List<Exception>();

        public BalancedBracketParser(string text)
        {
            // empty is valid, cannot be null
            Text = text ?? string.Empty; 
            TryParse();
        }

        public int BracketCount
        {
            get
            {
                return BracketPairs.Count;
            }
        }

        public string GetBracketContent(int index)
        {
            if (index < 0)
            {
                return Text;
            }
            var pair = BracketPairs[index];
            return Text.Substring(pair.OpenIndex + 1, pair.CloseIndex - pair.OpenIndex - 1);
        }

        public bool Success
        {
            get
            {
                return Exceptions.Count == 0;
            }
        }

        private void TryParse()
        {
            string text = Text;
            int count = text.Length;
            int nestLevel = 0;
            Stack<int> unbalanced = new Stack<int>();
            for (int index = 0; index < count; ++index)
            {
                char c = text[index];
                if (c == '(' || c == '[' || c == '{' || c == '<')
                {
                    unbalanced.Push(index);
                    nestLevel++;
                }
                else if (c == ')' || c == ']' || c == '}' || c == '>')
                {
                    nestLevel--;
                    char closeChar = c;
                    if (unbalanced.Count == 0)
                    {
                        Exceptions.Add(new Exception("Unbalanced close symbol at index " + index));
                        return;
                    }
                    int openIndex = unbalanced.Peek();
                    char openChar = text[openIndex];
                    char expectedOpenChar = '\0';
                    switch (closeChar)
                    {
                        case ')':
                            expectedOpenChar = '(';
                            break;
                        case ']':
                            expectedOpenChar = '[';
                            break;
                        case '}':
                            expectedOpenChar = '{';
                            break;
                        case '>':
                            expectedOpenChar = '<';
                            break;
                    }
                    if (openChar != expectedOpenChar)
                    {
                        Exceptions.Add(new Exception("Mismatched close symbol " + closeChar + " at " + index +
                            ". The open symbol is " + openChar + " at " + openIndex + "."));
                        return;
                    }
                    var pair = new BracketPair()
                    {
                        OpenChar = openChar,
                        OpenIndex = openIndex,
                        CloseChar = closeChar,
                        CloseIndex = index,
                        NestLevel = (nestLevel + 1)
                    };
                    BracketPairs.Add(pair);
                    unbalanced.Pop();
                }
            }
            BracketPairs.Sort((BracketPair p1, BracketPair p2) => (p1.OpenIndex - p2.OpenIndex));
        }
    }
}

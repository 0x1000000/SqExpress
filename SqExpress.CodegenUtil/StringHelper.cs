using System;
using System.Text.RegularExpressions;

namespace SqExpress.CodeGenUtil
{
    internal class StringHelper
    {
        public static string DeSnake(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            char[]? result = null;
            int nextResultIndex = 0;

            bool afterSymbol = true;

            for (int i = 0; i < input.Length; i++)
            {
                var ch = input[i];

                if (!char.IsLetterOrDigit(ch))
                {
                    afterSymbol = true;
                    EnsureResult(i);
                }
                else
                {
                    if (afterSymbol && char.IsLower(ch))
                    {
                        OutResult(i, char.ToUpper(ch), true);
                    }
                    else
                    {
                        OutResult(i, ch, false);
                    }
                    afterSymbol = false;
                }

                if (result == null)
                {
                    nextResultIndex = i;
                }
            }

            return result == null ? input : new string(result, 0, nextResultIndex);

            void EnsureResult(int index)
            {
                if (result == null)
                {
                    result = new char[input.Length + 1];
                    input.CopyTo(0, result, 0, input.Length);
                    nextResultIndex = index;
                }
            }

            void OutResult(int index, char ch, bool changed)
            {
                var isFirstDigit = nextResultIndex == 0 && char.IsDigit(ch);
                if (result == null)
                {
                    if (!changed && !isFirstDigit)
                    {
                        return;
                    }

                    EnsureResult(index);
                }

                if (isFirstDigit)
                {
                    result![nextResultIndex++] = 'D';
                }

                result![nextResultIndex++] = ch;
            }
        }


        public static string AddNumberUntilUnique(string input, string delimiter, Predicate<string> test)
        {
            Regex regex = new Regex($"(.+?){delimiter}(\\d+)");
            while (!test(input))
            {
                int nextNo = 2; 
                var m = regex.Match(input);
                if (m.Success)
                {
                    input = m.Result("$1");
                    nextNo = int.Parse(m.Result("$2")) + 1;

                }

                input = input + delimiter + nextNo;
            }
            return input;
        }

    }
}
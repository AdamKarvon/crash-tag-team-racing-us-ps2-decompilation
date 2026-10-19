using System.Collections.Generic;

namespace NetP3DLib.Comparer;

class NaturalComparer : IComparer<string>
{
    private readonly bool _ignoreCase;

    public NaturalComparer(bool caseInsensitive)
    {
        _ignoreCase = caseInsensitive;
    }

    public int Compare(string left, string right)
    {
        if (left == null)
            return right == null ? 0 : -1;

        if (right == null)
            return 1;

        var leftIndex = 0;
        var rightIndex = 0;

        var leftLength = left.Length;
        var rightLength = right.Length;

        while (leftIndex < leftLength && rightIndex < rightLength)
        {
            var leftChar = left[leftIndex];
            var rightChar = right[rightIndex];

            bool leftIsDigit = leftChar >= '0' && leftChar <= '9';
            bool rightIsDigit = rightChar >= '0' && rightChar <= '9';

            if (leftIsDigit && rightIsDigit)
            {
                var leftNumber = 0L;
                while (leftIndex < leftLength)
                {
                    var current = left[leftIndex];
                    if (current < '0' || current > '9')
                        break;

                    leftNumber = leftNumber * 10 + (current - '0');
                    leftIndex++;
                }

                var rightNumber = 0L;
                while (rightIndex < rightLength)
                {
                    var current = right[rightIndex];
                    if (current < '0' || current > '9')
                        break;

                    rightNumber = rightNumber * 10 + (current - '0');
                    rightIndex++;
                }

                if (leftNumber != rightNumber)
                    return leftNumber < rightNumber ? -1 : 1;
            }
            else
            {
                if (_ignoreCase)
                {
                    leftChar = char.ToUpperInvariant(leftChar);
                    rightChar = char.ToUpperInvariant(rightChar);
                }

                if (leftChar != rightChar)
                    return leftChar < rightChar ? -1 : 1;

                leftIndex++;
                rightIndex++;
            }
        }

        return leftLength - rightLength;
    }
}
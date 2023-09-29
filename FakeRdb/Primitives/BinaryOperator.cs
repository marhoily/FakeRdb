namespace FakeRdb;

public enum UnaryOperator
{
    Not,
    // others
}

[Flags]
public enum BinaryOperator
{
    // Logical
    And =  1 | IsLogical,
    Or =  2 | IsLogical,

    // Arithmetic
    Multiplication =  3 | IsArithmetic,
    Division =  4 | IsArithmetic,
    Modulus =  5 | IsArithmetic,
    Addition =  6 | IsArithmetic,
    Subtraction =  7 | IsArithmetic,

    // Bitwise
    BinaryLeftShift =  8 | IsBitwise,
    BinaryRightShift =  9 | IsBitwise,
    BinaryAnd =  10 | IsBitwise,
    BinaryOr =  11 | IsBitwise,

    // Comparison
    Equal =  12 | IsComparison,
    Less =  13 | IsComparison,
    LessOrEqual =  14 | IsComparison,
    Greater =  15 | IsComparison,
    GreaterOrEqual =  16 | IsComparison,
    NotEqual =  17 | IsComparison,
    Is =  18 | IsComparison,
    IsNot =  19 | IsComparison,

    // Text Manipulation
    Concatenation =  20 | IsTextManipulation,
    In =  21 | IsTextManipulation,
    Like =  22 | IsTextManipulation,
    Glob =  23 | IsTextManipulation,
    Match =  24 | IsTextManipulation,
    RegExp =  25 | IsTextManipulation,

    // High bits for tagging categories
    IsArithmetic = 1 << 31,
    IsLogical = 1 << 29,
    IsComparison = 1 << 28,
    IsBitwise = 1 << 27,
    IsTextManipulation = 1 << 26
}


public static class BinaryOperatorExtensions
{
    public static bool IsInCategory(this BinaryOperator op, BinaryOperator category)
    {
        return (op & category) == category;
    }
}

namespace FakeRdb;

public enum SqliteTypeAffinity
{
    /// <summary>
    ///     A column with NUMERIC affinity may contain values using all five
    ///     storage classes. When text data is inserted into a NUMERIC column, the
    ///     storage class of the text is converted to INTEGER or REAL (in order of
    ///     preference) if the text is a well-formed integer or real literal,
    ///     respectively. If the TEXT value is a well-formed integer literal that is
    ///     too large to fit in a 64-bit signed integer, it is converted to REAL. For
    ///     conversions between TEXT and REAL storage classes, only the first 15
    ///     significant decimal digits of the number are preserved. If the TEXT value
    ///     is not a well-formed integer or real literal, then the value is stored as
    ///     TEXT. For the purposes of this paragraph, hexadecimal integer literals are
    ///     not considered well-formed and are stored as TEXT. If a floating point
    ///     value that can be represented exactly as an integer is inserted into a
    ///     column with NUMERIC affinity, the value is converted into an integer. No
    ///     attempt is made to convert NULL or BLOB values. A string might look like a
    ///     floating-point literal with a decimal point and/or exponent notation but as
    ///     long as the value can be expressed as an integer, the NUMERIC affinity will
    ///     convert it into an integer. Hence, the string '3.0e+5' is stored in a
    ///     column with NUMERIC affinity as the integer 300000, not as the floating
    ///     point value 300000.0.
    /// </summary>
    Numeric,

    /// <summary>
    ///     A column that uses INTEGER affinity behaves the same as a column with
    ///     NUMERIC affinity. The difference between INTEGER and NUMERIC affinity is
    ///     only evident in a CAST expression: The expression "CAST(4.0 AS INT)"
    ///     returns an integer 4, whereas "CAST(4.0 AS NUMERIC)" leaves the value as a
    ///     floating-point 4.0.
    /// </summary>
    Integer,

    /// <summary>
    ///     A column with REAL affinity behaves like a column with NUMERIC
    ///     affinity except that it forces integer values into floating point
    ///     representation.
    /// </summary>
    Real,

    /// <summary>
    ///     A column with TEXT affinity stores all data using storage classes
    ///     NULL, TEXT or BLOB. If numerical data is inserted into a column with TEXT
    ///     affinity it is converted into text form before being stored.
    /// </summary>
    Text,

    /// <summary>
    ///     A column with affinity BLOB does not prefer one storage class over
    ///     another and no attempt is made to coerce data from one storage class into
    ///     another.
    /// </summary>
    Blob,
    None = Blob
}
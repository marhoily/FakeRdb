using System.Diagnostics.CodeAnalysis;

// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace FakeRdb;

/// <summary>
///     Represents a parameter and its value in a <see cref="T:Microsoft.Data.Sqlite.SqliteCommand" />.
/// </summary>
/// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
/// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/types">Data Types</seealso>
public sealed class FakeDbParameter : DbParameter
{
    private string _parameterName = string.Empty;
    private object _value;
    private int? _size;
    private Type? _sqliteType;
    private string _sourceColumn = string.Empty;


    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Microsoft.Data.Sqlite.SqliteParameter" /> class.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public FakeDbParameter()
    {
    }


    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Microsoft.Data.Sqlite.SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter. Can be null.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/types">Data Types</seealso>
    public FakeDbParameter(string name, object? value)
    {
        ParameterName = name;
        Value = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Microsoft.Data.Sqlite.SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public FakeDbParameter(string? name, Type type)
    {
        ParameterName = name;
        Type = type;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Microsoft.Data.Sqlite.SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="size">The maximum size, in bytes, of the parameter.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public FakeDbParameter(string? name, Type type, int size)
        : this(name, type)
    {
        Size = size;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Microsoft.Data.Sqlite.SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="size">The maximum size, in bytes, of the parameter.</param>
    /// <param name="sourceColumn">The source column used for loading the value. Can be null.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public FakeDbParameter(string? name, Type type, int size, string? sourceColumn)
        : this(name, type, size)
    {
        SourceColumn = sourceColumn;
    }

    /// <summary>Gets or sets the type of the parameter.</summary>
    /// <value>The type of the parameter.</value>
    /// <remarks>Due to SQLite's dynamic type system, parameter values are not converted.</remarks>
    public override DbType DbType { get; set; } = DbType.String;

    /// <summary>Gets or sets the SQLite type of the parameter.</summary>
    /// <value>The SQLite type of the parameter.</value>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public Type Type
    {
        get => _sqliteType ?? _value.GetType();
        set => _sqliteType = value;
    }

    /// <summary>
    ///     Gets or sets the direction of the parameter. Only <see cref="F:System.Data.ParameterDirection.Input" /> is supported.
    /// </summary>
    /// <value>The direction of the parameter.</value>
    public override ParameterDirection Direction
    {
        get => ParameterDirection.Input;
        set
        {
            if (value != ParameterDirection.Input)
                throw new ArgumentException("Only input parameters are supported");
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the parameter is nullable.
    /// </summary>
    /// <value>A value indicating whether the parameter is nullable.</value>
    public override bool IsNullable { get; set; }

    /// <summary>Gets or sets the name of the parameter.</summary>
    /// <value>The name of the parameter.</value>
    public override string ParameterName
    {
        get => _parameterName;
        [param: AllowNull]
        set => _parameterName = value ?? string.Empty;
    }

    /// <summary>
    ///     Gets or sets the maximum size, in bytes, of the parameter.
    /// </summary>
    /// <value>The maximum size, in bytes, of the parameter.</value>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public override int Size
    {
        get
        {
            int? size = _size;
            if (size.HasValue)
                return size.GetValueOrDefault();
            if (_value is string str)
                return str.Length;
            return !(_value is byte[] numArray) ? 0 : numArray.Length;
        }
        set => _size = value >= -1 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, null);
    }

    /// <summary>
    ///     Gets or sets the source column used for loading the value.
    /// </summary>
    /// <value>The source column used for loading the value.</value>
    public override string SourceColumn
    {
        get => _sourceColumn;
        [param: AllowNull]
        set => _sourceColumn = value ?? string.Empty;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the source column is nullable.
    /// </summary>
    /// <value>A value indicating whether the source column is nullable.</value>
    public override bool SourceColumnNullMapping { get; set; }

    /// <summary>Gets or sets the value of the parameter.</summary>
    /// <value>The value of the parameter.</value>
    /// <remarks>Due to SQLite's dynamic type system, parameter values are not converted.</remarks>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/types">Data Types</seealso>
    public override object? Value
    {
        get => _value;
        set => _value = value;
    }

    /// <summary>
    ///     Resets the <see cref="P:Microsoft.Data.Sqlite.SqliteParameter.DbType" /> property to its original value.
    /// </summary>
    public override void ResetDbType() => ResetSqliteType();

    /// <summary>
    ///     Resets the <see cref="P:Microsoft.Data.Sqlite.SqliteParameter.Type" /> property to its original value.
    /// </summary>
    public void ResetSqliteType()
    {
        DbType = DbType.String;
        _sqliteType = null;
    }

}
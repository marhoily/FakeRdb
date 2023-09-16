using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace FakeRdb;

/// <summary>
///     Represents a parameter and its value in a <see cref="T:Microsoft.Data.Sqlite.SqliteCommand" />.
/// </summary>
/// <remarks>Due to SQLite's dynamic type system, parameter values are not converted.</remarks>
/// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
/// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/types">Data Types</seealso>
public sealed class FakeDbParameter : DbParameter
{
    private string _parameterName = string.Empty;
    private object _value;
    private int? _size;
    private Type? _sqliteType;
    private string _sourceColumn = string.Empty;
    private static readonly char[] _parameterPrefixes = new char[3]
    {
        '@',
        '$',
        ':'
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Microsoft.Data.Sqlite.SqliteParameter" /> class.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public FakeDbParameter()
    {
    }


#nullable enable
    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Microsoft.Data.Sqlite.SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter. Can be null.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/types">Data Types</seealso>
    public FakeDbParameter(string name, object? value)
    {
        this.ParameterName = name;
        this.Value = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Microsoft.Data.Sqlite.SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public FakeDbParameter(string? name, Type type)
    {
        this.ParameterName = name;
        this.Type = type;
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
        this.Size = size;
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
        this.SourceColumn = sourceColumn;
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
        get => this._sqliteType ?? _value.GetType();
        set => this._sqliteType = value;
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
        get => this._parameterName;
        [param: AllowNull]
        set => this._parameterName = value ?? string.Empty;
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
            int? size = this._size;
            if (size.HasValue)
                return size.GetValueOrDefault();
            if (this._value is string str)
                return str.Length;
            return !(this._value is byte[] numArray) ? 0 : numArray.Length;
        }
        set => this._size = value >= -1 ? new int?(value) : throw new ArgumentOutOfRangeException(nameof(value), (object)value, (string)null);
    }

    /// <summary>
    ///     Gets or sets the source column used for loading the value.
    /// </summary>
    /// <value>The source column used for loading the value.</value>
    public override string SourceColumn
    {
        get => this._sourceColumn;
        [param: AllowNull]
        set => this._sourceColumn = value ?? string.Empty;
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
        get => this._value;
        set => this._value = value;
    }

    /// <summary>
    ///     Resets the <see cref="P:Microsoft.Data.Sqlite.SqliteParameter.DbType" /> property to its original value.
    /// </summary>
    public override void ResetDbType() => this.ResetSqliteType();

    /// <summary>
    ///     Resets the <see cref="P:Microsoft.Data.Sqlite.SqliteParameter.Type" /> property to its original value.
    /// </summary>
    public void ResetSqliteType()
    {
        this.DbType = DbType.String;
        this._sqliteType = null;
    }

}
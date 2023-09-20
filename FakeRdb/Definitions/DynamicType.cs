namespace FakeRdb;

public sealed record DynamicType(SqliteStorageType StorageType, SqliteTypeAffinity TypeAffinity, Type RuntimeType)
{
    public static readonly DynamicType Text = new(SqliteStorageType.Text, SqliteTypeAffinity.Text, typeof(string));
    public static readonly DynamicType Bool = new(SqliteStorageType.Integer, SqliteTypeAffinity.Integer, typeof(bool));
    public static readonly DynamicType Integer = new(SqliteStorageType.Integer, SqliteTypeAffinity.Integer, typeof(long));
    public static readonly DynamicType Numeric = new(SqliteStorageType.Real, SqliteTypeAffinity.Numeric, typeof(decimal));
    public static readonly DynamicType Real = new(SqliteStorageType.Real, SqliteTypeAffinity.Real, typeof(double));
    public static readonly DynamicType Blob = new(SqliteStorageType.Blob, SqliteTypeAffinity.Blob, typeof(byte[]));
    public static readonly DynamicType Null = new(SqliteStorageType.Null, SqliteTypeAffinity.Blob, typeof(DBNull));

    public static implicit operator Type(DynamicType dynamicType) => dynamicType.RuntimeType;
}
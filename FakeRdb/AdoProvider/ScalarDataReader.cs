namespace FakeRdb;

public sealed class ScalarDataReader : DbDataReader
{
    private readonly object _scalarValue;
    private bool _hasRead;

    public ScalarDataReader(object scalarValue)
    {
        _scalarValue = scalarValue;
    }

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => _scalarValue;

    public override int Depth => 0;
    public override int FieldCount => 1;
    public override bool HasRows => true;
    public override bool IsClosed => false;
    public override int RecordsAffected => -1;

    public override bool GetBoolean(int ordinal) => (bool)_scalarValue;
    public override byte GetByte(int ordinal) => (byte)_scalarValue;
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
    public override char GetChar(int ordinal) => (char)_scalarValue;
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
    public override string GetDataTypeName(int ordinal) => _scalarValue.GetType().Name;
    public override DateTime GetDateTime(int ordinal) => (DateTime)_scalarValue;
    public override decimal GetDecimal(int ordinal) => (decimal)_scalarValue;
    public override double GetDouble(int ordinal) => (double)_scalarValue;
    public override Type GetFieldType(int ordinal) => _scalarValue.GetType();
    public override float GetFloat(int ordinal) => (float)_scalarValue;
    public override Guid GetGuid(int ordinal) => (Guid)_scalarValue;
    public override short GetInt16(int ordinal) => (short)_scalarValue;
    public override int GetInt32(int ordinal) => (int)_scalarValue;
    public override long GetInt64(int ordinal) => (long)_scalarValue;
    public override string GetName(int ordinal) => "Column1";
    public override int GetOrdinal(string name) => 0;
    public override string GetString(int ordinal) => _scalarValue.ToString() ?? "NULL";
    public override object GetValue(int ordinal) => _scalarValue;
    public override int GetValues(object[] values)
    {
        values[0] = _scalarValue;
        return 1;
    }
    public override bool IsDBNull(int ordinal) => _scalarValue == DBNull.Value;

    public override bool NextResult() => false;
    public override bool Read() => !_hasRead && (_hasRead = true);
    public override IEnumerator GetEnumerator() => new[] { _scalarValue }.GetEnumerator();

    public override void Close() { }
    public override DataTable GetSchemaTable() => throw new NotSupportedException();
    public override T GetFieldValue<T>(int ordinal) => (T)_scalarValue;

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());
    public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => Task.FromResult(GetFieldValue<T>(ordinal));
    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => Task.FromResult(IsDBNull(ordinal));
}
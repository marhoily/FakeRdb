namespace FakeRdb;

public sealed class DataSetDbReader : DbDataReader
{
    private readonly Table _table;
    private readonly int _recordsAffected = -1;
    private int _currentRow = -1;

    public DataSetDbReader(QueryResult queryResult)
    {
        _table = queryResult.Table;
        _recordsAffected = queryResult.RecordsCount;
    }

    public DataSetDbReader(Table t)
    {
        _table = t;
    }

    public override int Depth => 0;
    public override int FieldCount => _table.Columns.Length;

    public override object this[int ordinal] => GetValue(ordinal);

    public override object this[string name] => GetValue(GetOrdinal(name));

    public override bool HasRows => _table.RowCount > 0;

    public override bool IsClosed => false;

    public override int RecordsAffected => _recordsAffected;

    public override bool Read()
    {
        _currentRow++;
        return _currentRow < _table.RowCount;
    }

    public override bool NextResult()
    {
        return false; // No support for multiple result sets
    }

    public override void Close()
    {
        // No operation needed
    }

    public override bool GetBoolean(int ordinal)
    {
        return (bool)GetValue(ordinal);
    }

    public override byte GetByte(int ordinal)
    {
        return (byte)GetValue(ordinal);
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var value = (byte[])GetValue(ordinal);
        var bytesToCopy = Math.Min(length, value.Length - (int)dataOffset);
        Buffer.BlockCopy(value, (int)dataOffset, buffer, bufferOffset, bytesToCopy);
        return bytesToCopy;
    }

    public override char GetChar(int ordinal)
    {
        return (char)GetValue(ordinal);
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var value = (string)GetValue(ordinal);
        var charsToCopy = Math.Min(length, value.Length - (int)dataOffset);
        value.CopyTo((int)dataOffset, buffer, bufferOffset, charsToCopy);
        return charsToCopy;
    }

    public override string GetDataTypeName(int ordinal)
    {
        return _table.Columns[ordinal]
            .Header.ColumnType
            .ToString().ToUpperInvariant();
    }

    public override DateTime GetDateTime(int ordinal)
    {
        return (DateTime)GetValue(ordinal);
    }

    public override decimal GetDecimal(int ordinal)
    {
        return (decimal)GetValue(ordinal);
    }

    public override double GetDouble(int ordinal)
    {
        return (double)GetValue(ordinal);
    }

    public override Type GetFieldType(int ordinal)
    {
        throw new NotImplementedException();
        //return _queryResult.Headers[ordinal];
    }

    public override float GetFloat(int ordinal)
    {
        return (float)GetValue(ordinal);
    }

    public override Guid GetGuid(int ordinal)
    {
        return (Guid)GetValue(ordinal);
    }

    public override short GetInt16(int ordinal)
    {
        return (short)GetValue(ordinal);
    }

    public override int GetInt32(int ordinal)
    {
        return (int)GetValue(ordinal);
    }

    public override long GetInt64(int ordinal)
    {
        return (long)GetValue(ordinal);
    }

    public override string GetName(int ordinal)
    {
        return _table.Columns[ordinal].Header.Name;
    }

    public override int GetOrdinal(string name)
    {
        for (int i = 0; i < _table.Columns.Length; i++)
        {
            if (string.Equals(_table.Columns[i].Header.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    public override string GetString(int ordinal)
    {
        return (string)GetValue(ordinal);
    }

    public override object GetValue(int ordinal)
    {
        return _table.Columns[ordinal].Rows[_currentRow] ?? DBNull.Value;
    }

    public override int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, _table.Columns.Length);
        for (int i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }
        return count;
    }

    public override bool IsDBNull(int ordinal)
    {
        return GetValue(ordinal) == DBNull.Value;
    }

    public override IEnumerator GetEnumerator()
    {
        return _table.GetRows().GetEnumerator();
    }
}
namespace FakeRdb;

public class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _parameters = new();
    public void AddWithValue(string parameterName, object value)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            _parameters[index].Value = value;
        }
        else
        {
            _parameters.Add(
                new FakeDbParameter(parameterName, value));
        }
    }

    public override int Count => _parameters.Count;

    public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

    public override int Add(object value)
    {
        if (value is not DbParameter parameter)
        {
            throw new ArgumentException("The provided value is not a DbParameter.", nameof(value));
        }

        _parameters.Add(parameter);
        return _parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        foreach (var value in values)
        {
            Add(value);
        }
    }

    public override void Clear()
    {
        _parameters.Clear();
    }

    public override bool Contains(string parameterName)
    {
        return _parameters.Exists(p => string.Equals(p.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase));
    }

    public override bool Contains(object value)
    {
        return _parameters.Contains((DbParameter)value);
    }

    public override void CopyTo(Array array, int index)
    {
        ((ICollection)_parameters).CopyTo(array, index);
    }

    public override IEnumerator GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }

    protected override DbParameter GetParameter(int index)
    {
        return _parameters[index];
    }

    protected override DbParameter GetParameter(string parameterName)
    {
        return _parameters.Find(p => string.Equals(p.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentOutOfRangeException(nameof(parameterName));
    }

    public override int IndexOf(string parameterName)
    {
        for (int i = 0; i < _parameters.Count; i++)
        {
            var parameter = _parameters[i];
            if (string.Equals(parameter.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public override int IndexOf(object value)
    {
        return _parameters.IndexOf((DbParameter)value);
    }

    public override void Insert(int index, object value)
    {
        if (value is not DbParameter parameter)
        {
            throw new ArgumentException("The provided value is not a DbParameter.", nameof(value));
        }

        _parameters.Insert(index, parameter);
    }

    public override void Remove(object value)
    {
        _parameters.Remove((DbParameter)value);
    }

    public override void RemoveAt(int index)
    {
        _parameters.RemoveAt(index);
    }

    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            RemoveAt(index);
        }
    }

    protected override void SetParameter(int index, DbParameter value)
    {
        _parameters[index] = value;
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            _parameters[index] = value;
        }
        else
        {
            throw new IndexOutOfRangeException($"Parameter '{parameterName}' not found.");
        }
    }
}
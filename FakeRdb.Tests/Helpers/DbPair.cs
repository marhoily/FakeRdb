namespace FakeRdb.Tests;

public class DbPair
{
    private readonly DbConnection _referenceDb;
    private readonly DbConnection _targetDb;
    private readonly ITestOutputHelper _outputHelper;
    private string? _testName;
    private bool _errorExpected;
    private object? _bindings;
    private string? _sql;
    private bool _shouldPrint = true;

    public DbPair(DbConnection referenceDb, DbConnection targetDb,
        ITestOutputHelper outputHelper)
    {
        _referenceDb = referenceDb ?? throw new ArgumentNullException(nameof(referenceDb));
        _targetDb = targetDb ?? throw new ArgumentNullException(nameof(targetDb));
        _outputHelper = outputHelper;
    }

    public DbPair ExecuteOnBoth(string sql)
    {
        _sql = sql;
        return this;
    }

    public DbPair ExecuteOnBoth(Action<DbConnection> seeder)
    {
        if (_bindings != null) throw new NotSupportedException("Bindings with seed");
        return this;
    }

    public DbPair WithName(string testName)
    {
        _testName = testName;
        return this;
    }

    public DbPair WithBindings(object bindings)
    {
        _bindings = bindings;
        return this;
    }

    public DbPair Print(bool shouldPrint)
    {
        _shouldPrint = shouldPrint;
        return this;
    }

    public DbPair AnticipateError(bool errorExpected = true)
    {
        _errorExpected = errorExpected;
        return this;
    }

    public void AssertResultsAreIdentical()
    {
        if (_sql == null) throw new InvalidOperationException("Call ExecuteOnBoth() first!");
        if (_shouldPrint)
        {
            if (_testName != null) _outputHelper.WriteLine($"--- {_testName} ---");
            _outputHelper.WriteLine(_sql);
        }

        var cmd1 = _referenceDb.CreateCommand();
        cmd1.CommandText = _sql;
        var (expected, x1) = cmd1.SafeExecuteReader();

        var cmd2 = _targetDb.CreateCommand();
        cmd2.CommandText = _sql;
        var (actual, x2) = cmd2.SafeExecuteReader();
        if (_errorExpected)
        {
            x1.Should().NotBeNull();
            x2.Should().NotBeNull();
            ErrorEquivalence.Assert(x1!.Message, x2!.Message);
        }
        else
        {
            actual!.ShouldEqual(expected!, _outputHelper, _shouldPrint);
        }
    }
}
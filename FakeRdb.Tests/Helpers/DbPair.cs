using JetBrains.Annotations;

namespace FakeRdb.Tests;

public interface IAmReadyToAssert
{
    void AssertResultsAreIdentical();
}

public sealed class DbPair : IAmReadyToAssert
{
    private readonly DbConnection _referenceDb;
    private readonly DbConnection _targetDb;
    private ITestOutputHelper? _outputHelper;
    private string? _testName;
    private Outcome _expectedOutcome = Outcome.Success;
    private object?[]? _bindings;
    private string? _sql;
    private bool _shouldLog = true;

    public DbPair(DbConnection referenceDb, DbConnection targetDb)
    {
        _referenceDb = referenceDb ?? throw new ArgumentNullException(nameof(referenceDb));
        _targetDb = targetDb ?? throw new ArgumentNullException(nameof(targetDb));
    }

    [MustUseReturnValue]
    public DbPair LogQueryAndResultsTo(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        return this;
    }

    [MustUseReturnValue]
    public IAmReadyToAssert QueueForBothDbs(string sql)
    {
        _sql = sql;
        return this;
    }

    public DbPair ExecuteOnBoth(string sql)
    {
        var cmd1 = _referenceDb.CreateCommand();
        cmd1.CommandText = sql;
        cmd1.SafeExecuteReader();

        var cmd2 = _targetDb.CreateCommand();
        cmd2.CommandText = sql;
        cmd2.SafeExecuteReader();
        return this;
    }
    public DbPair ExecuteOnBoth(Action<DbConnection> seeder)
    {
        seeder(_referenceDb);
        seeder(_targetDb);
        return this;
    }

    [MustUseReturnValue]
    public DbPair WithName(string testName)
    {
        _testName = testName;
        return this;
    }

    [MustUseReturnValue]
    public DbPair WithBindings(object?[]? bindings)
    {
        _bindings = bindings;
        return this;
    }

    [MustUseReturnValue]
    public DbPair ShouldLog(bool shouldPrint)
    {
        _shouldLog = shouldPrint;
        return this;
    }


    [MustUseReturnValue]
    public DbPair Anticipate(Outcome outcome)
    {
        _expectedOutcome = outcome;
        return this;
    }

    public void AssertResultsAreIdentical()
    {
        if (_sql == null) throw new InvalidOperationException(
            "SQL query has not been queued. " +
            "Call QueueForBothDbs() before executing this method.");

        var referenceFactory = DbProviderFactories.GetFactory(_referenceDb)
                      ?? throw new InvalidOperationException();
        var targetFactory = DbProviderFactories.GetFactory(_targetDb)
                      ?? throw new InvalidOperationException();

        var cmd1 = _referenceDb.CreateCommand();
        cmd1.CommandText = _sql;
        if (_bindings != null)
            cmd1.AddPositionalParameters(referenceFactory, _bindings);
        var (referenceResult, referenceError) = cmd1.SafeExecuteReader();

        var cmd2 = _targetDb.CreateCommand();
        cmd2.CommandText = _sql;
        if (_bindings != null)
            cmd2.AddPositionalParameters(targetFactory, _bindings);
        var (targetResult, targetError) = cmd2.SafeExecuteReader();

        LogQueryAndTheResults(
            targetResult, referenceResult,
            targetError, referenceError);

        // reference: Success; target: Success
        if (referenceResult != null && targetResult != null)
        {
            targetResult.ShouldEqual(referenceResult);
            _expectedOutcome.Should().NotBe(Outcome.Error);
        }
        // reference: Error; target: Error
        else if (referenceError != null && targetError != null)
        {
            ErrorEquivalence.Assert(referenceError.Message, targetError.Message);
            _expectedOutcome.Should().NotBe(Outcome.Success);
        }
        // reference: Success; target: Error
        else if (referenceResult != null && targetError != null)
        {
            _expectedOutcome.Should().NotBe(Outcome.Error);
            throw targetError;
        }
        // reference: Error; target: Success
        else if (referenceError != null && targetResult != null)
        {
            _expectedOutcome.Should().NotBe(Outcome.Success);
            Assert.Fail("Mismatch: Target database returned a successful " +
                        "result when an error was expected based on the " +
                        "reference database.");
        }
        else throw new Exception(
            "Invariant violation: Both result and error cannot be null " +
            "for either the reference or target database.");
    }

    private void LogQueryAndTheResults(
        ReaderResult? targetResult, ReaderResult? referenceResult,
        Exception? targetError, Exception? referenceError)
    {
        if (_outputHelper is not {} o || !_shouldLog) return;
        if (_testName != null)
            o.WriteLine($"--- {_testName} ---");

        o.WriteLine(_sql);

        if (targetResult != null)
        {
            o.WriteLine("--- Target Result --- ");
            o.Print(targetResult);
        }

        if (referenceResult != null)
        {
            o.WriteLine("--- Reference Result ---");
            o.Print(referenceResult);
        }

        if (targetError != null)
        {
            o.WriteLine("--- Target Error --- ");
            o.WriteLine(targetError.Message);
        }

        if (referenceError != null)
        {
            o.WriteLine("--- Reference Error --- ");
            o.WriteLine(referenceError.Message);
        }
    }
}
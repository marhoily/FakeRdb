using System.Diagnostics;
using FakeRdb;

[assembly: DebuggerDisplay("{FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.ColumnExp))]
[assembly: DebuggerDisplay("{FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.UnaryExp))]
[assembly: DebuggerDisplay("{FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.LiteralExp))]
[assembly: DebuggerDisplay("{FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.BinaryExp))]
[assembly: DebuggerDisplay("{FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.BindExp))]
[assembly: DebuggerDisplay("{FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.AggregateExp))]
[assembly: DebuggerDisplay("{FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.ScalarExp))]
[assembly: DebuggerDisplay("{FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.InExp))]
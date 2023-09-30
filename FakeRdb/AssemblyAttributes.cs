using System.Diagnostics;
using FakeRdb;

[assembly: DebuggerDisplay("Column: {FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.ColumnExp))]
[assembly: DebuggerDisplay("Unary: {FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.UnaryExp))]
[assembly: DebuggerDisplay("Literal: {FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.LiteralExp))]
[assembly: DebuggerDisplay("Binary: {FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.BinaryExp))]
[assembly: DebuggerDisplay("Bind: {FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.BindExp))]
[assembly: DebuggerDisplay("Aggregate: {FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.AggregateExp))]
[assembly: DebuggerDisplay("Scalar: {FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.ScalarExp))]
[assembly: DebuggerDisplay("In: {FakeRdb.DebugPrint.Print(this),nq}", Target = typeof(IR.InExp))]
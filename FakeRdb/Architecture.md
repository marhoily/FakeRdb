## SQL Execution Workflow

To execute SQL, the process is broken down into three main steps:
1. Lex and parse the SQL code into an Abstract Syntax Tree (AST) using ANTLR.
2. Utilize an ANTLR-based visitor, named `AstToIrVisitor`, to convert the AST into an Intermediate Representation (IR).
3. Execute the generated IR using methods in the `IrExecutor` class.

### Lowering AST to IR

`AstToIrVisitor` walks the parse tree to perform the following simplifications:
- Unroll `*` in SELECT statements.
- Resolve table, column, and function names.
- Dereference aliases.
- Unquote and parse literals.
- Bind SQL parameters.
- Distinguish between plain and aggregate functions.

### Executing IR

The `IrExecutor` class provides a set of static methods, starting with `Execute(IR.SqlStatement[] list)`.

### In-Memory Database Representation

The database is held purely in memory and is represented by a set of data structures:
- Database
- Table
- Row
- TableSchema

### ADO.NET Provider

The implementation is intended to fully mimic the behavior of Microsoft.Data.Sqlite and allows for easy integration with Dapper.

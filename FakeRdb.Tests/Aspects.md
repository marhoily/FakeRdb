# SQL Syntactic Aspects
## Basic Syntax Elements

- Aliases (AS)
- Comments (-- for single line, /* */ for multi-line)
- Quoting / Escaping Identifiers (Brackets [], Double quotes "", Backticks `)
- String literals (Using single quotes ' ')
- Dot notation (schema.table.column)

## Expressions and Operators

- Arithmetic Operators (+, -, *, /)
- Comparison Operators (=, <> or !=, <, >, <=, >=)
- Logical Operators (AND, OR, NOT)
- Concatenation Operators (|| in SQLite, + in SQL Server, etc.)
- Set Membership Operators (IN, NOT IN)
- NULL Checks (IS NULL, IS NOT NULL)
- Pattern Matching (LIKE, GLOB)

Data Types in Column Definitions

    INTEGER, TEXT, REAL, BLOB, etc. (depending on the database system)

Default Values

    DEFAULT keyword in column definitions

SQL Placeholders and Parameters

    Positional Parameters (?)
    Named Parameters (@paramName)

Constraints Syntax

    ON DELETE CASCADE, ON UPDATE SET NULL, etc.

LIMIT and OFFSET

    LIMIT (and its variations depending on the RDBMS)

Built-in SQL Functions

    Type conversion functions (CAST, CONVERT)
    Conditional functions (CASE WHEN ... THEN ... ELSE ... END)
    COALESCE, NULLIF

JOIN Syntax Variations

    USING clause
    ON clause

Wildcards

    Asterisk (*) for selecting all columns

Temporal Features

    NOW(), CURRENT_DATE, CURRENT_TIME, etc.

Array Constructors and Operators (for databases that support arrays)

    Array literals, ANY, ALL, etc.

Common Table Expressions (CTEs) Syntax

    WITH clause

Window Specification

    OVER(PARTITION BY ... ORDER BY ...)

Hinting (For databases that support hints)

    Query hints, index hints, join hints, etc.

SQL Standard Clauses

    DISTINCT
    ALL

Upsert Features (depending on the database)

    INSERT ... ON DUPLICATE KEY UPDATE ...
    INSERT ... ON CONFLICT ...

Pivoting and Unpivoting Syntax

    For databases that support dynamic pivoting

Execution Control Statements (mostly for stored procedures)

    IF ... ELSE
    WHILE
    BREAK, CONTINUE

Variables and Parameters in Stored Procedures

    DECLARE, SET, OUTPUT

Cursors Syntax

    DECLARE CURSOR, FETCH, OPEN, CLOSE

Dynamic SQL (building and executing SQL on-the-fly)

    Using functions or procedures to execute dynamic strings as SQL queries

Backup and Restore Syntax

    Specific keywords and syntax depending on the operation and RDBMS

System and Session Variables

    Accessing and modifying system settings or session-level variables

Remember, the frequency and relevance of these syntactic features can depend largely on the specific use case, application, and the SQL dialect in question. The list is sorted based on general SQL usage, but there might be deviations depending on the specific context or RDBMS.
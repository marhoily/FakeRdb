# SQL Expression Analysis Categories

## Filter Expression Categories

- [ ] **SingleTableConditions**
  - **Description**: Conditions affecting only one table.
  - **Utility**: These can often be pushed down to the database for more efficient execution.

- [ ] **EquiJoinConditions**
  - **Description**: Conditions involving equijoins between tables (`Table1.ColumnX = Table2.ColumnY`).
  - **Utility**: Useful for query optimization, especially for join ordering.

- [ ] **NonEquiJoinConditions**
  - **Description**: Conditions involving non-equijoins between tables (`Table1.ColumnX > Table2.ColumnY`).
  - **Utility**: More complex to optimize but still important for understanding query behavior.

- [ ] **CompositeConditions**
  - **Description**: Conditions involving multiple columns combined using logical operators.
  - **Utility**: Often need special handling due to their complexity.

- [ ] **IsNullConditions**
  - **Description**: Checks for `NULL` values in a column.
  - **Utility**: Important for NULL-safe query optimization.

- [ ] **IsNotNullConditions**
  - **Description**: Checks for non-`NULL` values in a column.
  - **Utility**: Important for NULL-safe query optimization.

- [ ] **RangeConditions**
  - **Description**: Conditions specifying a range of values for a column (`BETWEEN` or `>`/`<`).
  - **Utility**: Can sometimes be optimized with index scans if such an index exists.

- [ ] **InListConditions**
  - **Description**: Conditions where a column must match one of a list of values (`IN`).
  - **Utility**: Useful for query optimization, especially when the list is large.

- [ ] **AggregationConditions**
  - **Description**: Conditions involving aggregate functions (`SUM`, `AVG`, etc.).
  - **Utility**: Requires special handling, especially when combined with GROUP BY clauses.

- [ ] **SubqueryConditions**
  - **Description**: Conditions that involve subqueries (correlated or uncorrelated).
  - **Utility**: Can significantly affect performance and require special optimization techniques.

- [ ] **ConstantConditions**
  - **Description**: Conditions that are always true or false (`1=1` or `0=1`).
  - **Utility**: Often used for control flow in SQL queries, can usually be optimized out.

- [ ] **FunctionBasedConditions**
  - **Description**: Conditions involving the output of a function (built-in or user-defined).
  - **Utility**: May require special optimization strategies depending on the function's behavior.


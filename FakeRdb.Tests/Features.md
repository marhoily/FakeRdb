## SQL Features

### DML (Data Manipulation Language) Operations
- `SELECT`
- `INSERT`
- `UPDATE`
- `DELETE`

### SQL Joins
- INNER JOIN
- LEFT JOIN / LEFT OUTER JOIN
- RIGHT JOIN / RIGHT OUTER JOIN
- FULL JOIN / FULL OUTER JOIN
- CROSS JOIN

### Filtering and Sorting
- `WHERE`
- `ORDER BY`
- `GROUP BY`
- `HAVING`

### Functions
- Aggregate functions (`COUNT`, `SUM`, `AVG`, `MAX`, `MIN`)
- Date functions
- String functions
- Mathematical functions
- System functions

### DDL (Data Definition Language) Operations
- `CREATE`
- `ALTER`
- `DROP`

### Transactions
- `BEGIN TRANSACTION`
- `COMMIT`
- `ROLLBACK`
- `SAVEPOINT`

### Constraints
- PRIMARY KEY
- FOREIGN KEY
- UNIQUE
- CHECK
- DEFAULT
- INDEX

### Subqueries
- In-line views
- Scalar subqueries
- Correlated subqueries
- EXISTS

### Views
- Regular views
- Materialized views

### Stored Procedures and Functions

### Triggers

### Sequences and Identity Columns

### Cursors

### Set Operations
- `UNION`
- `INTERSECT`
- `EXCEPT` / `MINUS`

### User Access Control
- `GRANT`
- `REVOKE`

### System Catalog Queries

### Temporary Tables and Table Variables

### Error Handling
- `TRY...CATCH`
- RAISE / THROW

### Common Table Expressions (CTEs)

### Partitioning

### Pivoting

### Window Functions
- `ROW_NUMBER()`
- `RANK()`
- `DENSE_RANK()`
- `NTILE()`

### Recursive Queries

### XML and JSON Features

### Full-Text Search

### External Data Querying

### Spatial and Geographical Features

### Data Warehousing Extensions

### Backup and Restore Commands

### Database Snapshots and Mirroring

### Service Broker / Messaging

## Update


1. **Update a Customer's Email Address**:
```sql
UPDATE customers
SET email = 'new.email@example.com'
WHERE customer_id = 1;
```

2. **Update an Order's Total Amount**:
```sql
UPDATE orders
SET total_amount = 300.00
WHERE order_id = 2;
```

3. **Increase All Orders Total Amount by a Percentage**:
```sql
UPDATE orders
SET total_amount = total_amount * 1.10; -- Increase all order totals by 10%
```

4. **Update Based on a Join Condition**:
```sql
UPDATE customers
SET email = 'updated.email@example.com'
WHERE customer_id IN (SELECT customer_id FROM orders WHERE order_date < '2023-09-05');
```

5. **Update Multiple Fields**:
```sql
UPDATE customers
SET customer_name = 'Johnny Doe', email = 'johnny.doe@example.com'
WHERE customer_id = 1;
```

6. **Update Based on Aggregate Functions**:
```sql
UPDATE orders
SET total_amount = total_amount * 0.90 -- giving 10% discount
WHERE total_amount = (SELECT MAX(total_amount) FROM orders);
```


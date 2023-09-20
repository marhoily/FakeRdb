namespace FakeRdb.Tests;

public static class DatabaseSeeder
{
    public static void Seed3Albums(this DbConnection connection)
    {
        var factory = DbProviderFactories.GetFactory(connection)
                      ?? throw new InvalidOperationException();
        using var createTable = connection.CreateCommand();
        createTable.CommandText =
            "CREATE TABLE Album (" +
            "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "Title TEXT, " +
            "Artist TEXT, " +
            "Year INTEGER)";
        createTable.ExecuteNonQuery();

        using var insertRow = connection.CreateCommand();
        insertRow.CommandText =
            "INSERT INTO Album (Title, Artist, Year) " +
            "VALUES (@Title, @Artist, @Year)";

        InsertTracks(insertRow, "Track 1", "Artist 1", 2021);
        InsertTracks(insertRow, "Track 2", "Artist 2", 2022);
        InsertTracks(insertRow, "Track 3", "Artist 3", 2023);

        return;

        void InsertTracks(DbCommand cmd, string title, string artist, int year)
        {
            cmd.SetParameter(factory, "@Title", title);
            cmd.SetParameter(factory, "@Artist", artist);
            cmd.SetParameter(factory, "@Year", year);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }
    }
    public static void SeedCustomersOrders(this DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            -- Create the 'customers' table
            CREATE TABLE customers (
                customer_id INTEGER PRIMARY KEY,
                customer_name TEXT,
                email TEXT
            );
            
            -- Create the 'orders' table
            CREATE TABLE orders (
                order_id INTEGER PRIMARY KEY,
                customer_id INTEGER,
                order_date TEXT,
                total_amount NUMERIC,
                FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
            );
            
            -- Insert records into the 'customers' table
            INSERT INTO customers (customer_id, customer_name, email) VALUES
                (1, 'John Doe', 'john.doe@example.com'),
                (2, 'Jane Smith', 'jane.smith@example.com'),
                (3, 'Michael Johnson', 'michael.johnson@example.com');
            
            -- Insert records into the 'orders' table
            INSERT INTO orders (order_id, customer_id, order_date, total_amount) VALUES
                (1, 1, '2023-09-01', 100.20),
                (2, 1, '2023-09-05', 250.00),
                (3, 2, '2023-09-10', 150.00);
            """;
        cmd.ExecuteNonQuery();


    }
    public static void SeedHeterogeneousData(this DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE T (X,Y,Z);
            INSERT INTO T (X, Y, Z) 
            VALUES 
                ('Banana', 5, '2022-01-10'),
                (12345, 'Apple', x'4D414E47'),   -- x'4D414E47' is a BLOB literal for 'MANG'
                ('Orange', 8.23, 'This is text.'),
                (98765, x'01020304', 999),
                (3.1415, 'Another text', x'ABCDE12345');
            """;
        cmd.ExecuteNonQuery();
    }
    public static void SeedColumnAffinityTable(this DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE t1(
                t  TEXT,     -- text affinity by rule 2
                nu NUMERIC,  -- numeric affinity by rule 5
                i  INTEGER,  -- integer affinity by rule 1
                r  REAL,     -- real affinity by rule 4
                no BLOB      -- no affinity by rule 3
            );
            """;
        cmd.ExecuteNonQuery();


    }
}
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
                (1, 1, '2023-09-01', 100.00),
                (2, 1, '2023-09-05', 250.00),
                (3, 2, '2023-09-10', 150.00);
            """;
        cmd.ExecuteNonQuery();


    }

}
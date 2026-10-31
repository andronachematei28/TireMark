using Microsoft.Data.Sqlite;
using System.IO;

namespace TireMark;

internal sealed record Car(string Id, string Name, string Plate, string Vin, byte[]? Photo, DateTime CreatedAt);
internal sealed record Record(string Id, string CarId, string Type, DateTime? PurchaseDate, DateTime? StartDate, DateTime ExpiryDate, decimal? PriceAmount, string CurrencyCode, string Company, DateTime CreatedAt);

internal sealed class GarageDatabase
{
    private readonly string _connectionString;

    public GarageDatabase(string path, string initialCountry = "RO")
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=DELETE;
            PRAGMA foreign_keys=ON;
            CREATE TABLE IF NOT EXISTS Cars (
                Id TEXT PRIMARY KEY NOT NULL,
                Name TEXT NOT NULL DEFAULT '',
                Plate TEXT NOT NULL DEFAULT '',
                Vin TEXT NOT NULL DEFAULT '',
                Photo BLOB NULL,
                CreatedAt TEXT NOT NULL,
                CountryCode TEXT NOT NULL DEFAULT 'RO'
            );
            CREATE TABLE IF NOT EXISTS Records (
                Id TEXT PRIMARY KEY NOT NULL,
                CarId TEXT NOT NULL REFERENCES Cars(Id) ON DELETE CASCADE,
                Type TEXT NOT NULL,
                PurchaseDate TEXT NULL,
                StartDate TEXT NULL,
                ExpiryDate TEXT NOT NULL,
                PriceAmount TEXT NULL,
                CurrencyCode TEXT NOT NULL DEFAULT 'RON',
                Company TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Records_CarType ON Records(CarId, Type, ExpiryDate);
            """;
        command.ExecuteNonQuery();
        var hasCountry = false;
        using (var columns = connection.CreateCommand())
        {
            columns.CommandText = "PRAGMA table_info(Cars);";
            using var reader = columns.ExecuteReader();
            while (reader.Read()) hasCountry |= reader.GetString(1) == "CountryCode";
        }
        if (!hasCountry)
        {
            using var countryMigration = connection.CreateCommand();
            countryMigration.CommandText = "ALTER TABLE Cars ADD COLUMN CountryCode TEXT NOT NULL DEFAULT 'RO'; UPDATE Cars SET CountryCode=$country;";
            countryMigration.Parameters.AddWithValue("$country", initialCountry);
            countryMigration.ExecuteNonQuery();
        }
        using (var columns = connection.CreateCommand())
        {
            columns.CommandText = "PRAGMA table_info(Records);";
            using var reader = columns.ExecuteReader();
            var hasLei = false; var hasAmount = false;
            while (reader.Read()) { var name = reader.GetString(1); hasLei |= name == "PriceLei"; hasAmount |= name == "PriceAmount"; }
            reader.Close();
            if (hasLei && !hasAmount)
            {
                using var rename = connection.CreateCommand();
                rename.CommandText = "ALTER TABLE Records RENAME COLUMN PriceLei TO PriceAmount;";
                rename.ExecuteNonQuery();
            }
        }
        using var migration = connection.CreateCommand();
        migration.CommandText = "ALTER TABLE Records ADD COLUMN Company TEXT NOT NULL DEFAULT '';";
        try { migration.ExecuteNonQuery(); } catch (SqliteException ex) when (ex.SqliteErrorCode == 1) { }
        using var currencyMigration = connection.CreateCommand();
        currencyMigration.CommandText = "ALTER TABLE Records ADD COLUMN CurrencyCode TEXT NOT NULL DEFAULT 'RON';";
        try { currencyMigration.ExecuteNonQuery(); } catch (SqliteException ex) when (ex.SqliteErrorCode == 1) { }
        using var schema = connection.CreateCommand();
        schema.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='Records';";
        var tableSql = (string?)schema.ExecuteScalar() ?? "";
        if (tableSql.Contains("CHECK(Type IN", StringComparison.OrdinalIgnoreCase))
        {
            using var upgrade = connection.CreateCommand();
            upgrade.CommandText = """
                CREATE TABLE Records_TireMark (
                    Id TEXT PRIMARY KEY NOT NULL,
                    CarId TEXT NOT NULL REFERENCES Cars(Id) ON DELETE CASCADE,
                    Type TEXT NOT NULL,
                    PurchaseDate TEXT NULL,
                    StartDate TEXT NULL,
                    ExpiryDate TEXT NOT NULL,
                    PriceAmount TEXT NULL,
                    CurrencyCode TEXT NOT NULL DEFAULT 'RON',
                    Company TEXT NOT NULL DEFAULT '',
                    CreatedAt TEXT NOT NULL
                );
                INSERT INTO Records_TireMark(Id,CarId,Type,PurchaseDate,StartDate,ExpiryDate,PriceAmount,CurrencyCode,Company,CreatedAt)
                SELECT Id,CarId,CASE Type WHEN 'rovinieta' THEN 'road_toll' WHEN 'itp' THEN 'inspection' ELSE Type END,
                       PurchaseDate,StartDate,ExpiryDate,PriceAmount,CurrencyCode,Company,CreatedAt FROM Records;
                DROP INDEX IF EXISTS IX_Records_CarType;
                DROP TABLE Records;
                ALTER TABLE Records_TireMark RENAME TO Records;
                CREATE INDEX IX_Records_CarType ON Records(CarId, Type, ExpiryDate);
                """;
            upgrade.ExecuteNonQuery();
        }
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=ON;";
        command.ExecuteNonQuery();
        return connection;
    }

    public List<Car> GetCars(string countryCode)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Plate, Vin, Photo, CreatedAt FROM Cars WHERE CountryCode=$country ORDER BY CreatedAt, Name;";
        command.Parameters.AddWithValue("$country", countryCode);
        using var reader = command.ExecuteReader();
        var cars = new List<Car>();
        while (reader.Read())
        {
            cars.Add(new Car(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : (byte[])reader[4], DateTime.Parse(reader.GetString(5))));
        }
        return cars;
    }

    public Car? GetCar(string id, string countryCode) => GetCars(countryCode).FirstOrDefault(car => car.Id == id);

    public List<Record> GetRecords(string carId, string? type = null)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, CarId, Type, PurchaseDate, StartDate, ExpiryDate, PriceAmount, CurrencyCode, Company, CreatedAt
            FROM Records WHERE CarId=$carId AND ($type IS NULL OR Type=$type)
            ORDER BY COALESCE(PurchaseDate, StartDate, ExpiryDate), ExpiryDate, CreatedAt;
            """;
        command.Parameters.AddWithValue("$carId", carId);
        command.Parameters.AddWithValue("$type", (object?)type ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        var records = new List<Record>();
        while (reader.Read())
        {
            records.Add(new Record(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                ReadDate(reader, 3), ReadDate(reader, 4), DateTime.Parse(reader.GetString(5)),
                reader.IsDBNull(6) ? null : decimal.Parse(reader.GetString(6), System.Globalization.CultureInfo.InvariantCulture),
                reader.GetString(7), reader.GetString(8), DateTime.Parse(reader.GetString(9))));
        }
        return records;
    }

    public void SaveCar(Car car, string countryCode)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Cars(Id,Name,Plate,Vin,Photo,CreatedAt,CountryCode) VALUES($id,$name,$plate,$vin,$photo,$created,$country)
            ON CONFLICT(Id) DO UPDATE SET Name=$name, Plate=$plate, Vin=$vin, Photo=$photo, CountryCode=$country;
            """;
        command.Parameters.AddWithValue("$id", car.Id);
        command.Parameters.AddWithValue("$name", car.Name);
        command.Parameters.AddWithValue("$plate", car.Plate);
        command.Parameters.AddWithValue("$vin", car.Vin);
        command.Parameters.AddWithValue("$photo", (object?)car.Photo ?? DBNull.Value);
        command.Parameters.AddWithValue("$created", car.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$country", countryCode);
        command.ExecuteNonQuery();
    }

    public void DeleteCountry(string countryCode)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Cars WHERE CountryCode=$country;";
        command.Parameters.AddWithValue("$country", countryCode);
        command.ExecuteNonQuery();
    }

    public void DeleteCar(string id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Cars WHERE Id=$id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void SaveRecord(Record record)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Records(Id,CarId,Type,PurchaseDate,StartDate,ExpiryDate,PriceAmount,CurrencyCode,Company,CreatedAt)
            VALUES($id,$car,$type,$purchase,$start,$expiry,$price,$currency,$company,$created)
            ON CONFLICT(Id) DO UPDATE SET Type=$type, PurchaseDate=$purchase, StartDate=$start, ExpiryDate=$expiry, PriceAmount=$price, CurrencyCode=$currency, Company=$company;
            """;
        command.Parameters.AddWithValue("$id", record.Id);
        command.Parameters.AddWithValue("$car", record.CarId);
        command.Parameters.AddWithValue("$type", record.Type);
        command.Parameters.AddWithValue("$purchase", DbDate(record.PurchaseDate));
        command.Parameters.AddWithValue("$start", DbDate(record.StartDate));
        command.Parameters.AddWithValue("$expiry", record.ExpiryDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$price", record.PriceAmount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$currency", record.CurrencyCode);
        command.Parameters.AddWithValue("$company", record.Company);
        command.Parameters.AddWithValue("$created", record.CreatedAt.ToString("O"));
        command.ExecuteNonQuery();
    }

    public void DeleteRecord(string id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Records WHERE Id=$id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static DateTime? ReadDate(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? null : DateTime.Parse(reader.GetString(index));
    private static object DbDate(DateTime? date) => date?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value;
}




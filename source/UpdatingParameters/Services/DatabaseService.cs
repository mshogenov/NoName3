using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace UpdatingParameters.Services;

public class DatabaseService
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public DatabaseService()
    {
        // Получаем путь к папке AppData текущего пользователя
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DuctParametersApp"
        );

        // Создаем директорию, если она не существует
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        // Формируем путь к файлу базы данных
        _databasePath = Path.Combine(appDataPath, "DuctParameters.db");
        _connectionString = $"Data Source={_databasePath};Version=3;";

        // Создаем базу данных, если она не существует
        CreateDatabaseIfNotExists();
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }
    /// <summary>
    /// Гарантирует наличие базы данных SQLite и таблицы "DuctParameters".
    /// 
    /// Метод выполняет следующие действия:
    /// 1. Проверяет, существует ли файл базы данных по указанному пути. Если нет,
    ///    создаёт новый файл базы данных SQLite.
    /// 2. Устанавливает соединение с базой данных.
    /// 3. Выполняет SQL-команду для создания таблицы "DuctParameters", если она ещё не существует.
    ///    Структура таблицы определена с необходимыми колонками и ограничениями.
    /// </summary>
    private void CreateDatabaseIfNotExists()
    {
        try
        {
            if (!File.Exists(_databasePath))
            {
                SQLiteConnection.CreateFile(_databasePath);
            }

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS DuctParameters (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Material TEXT NOT NULL,
                        Shape TEXT NOT NULL,
                        ExternalInsulation TEXT,
                        InternalInsulation TEXT,
                        Size REAL NOT NULL,
                        Thickness REAL NOT NULL
                    )";
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании базы данных или таблицы: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            // Альтернативно можно вести логирование ошибки или повторно выбросить исключение.
        }
    }
}
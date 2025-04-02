using System.Data.SQLite;
using System.Windows;
using Dapper;
using UpdatingParameters.Models;
using UpdatingParameters.Services;

namespace UpdatingParameters.Storages.Parameters;

public class DuctParametersRepository
{
    private readonly string _connectionString;

    public DuctParametersRepository()
    {
        var databaseService = new DatabaseService();
        _connectionString = databaseService.GetConnectionString();
    }
    /// <summary>
    /// Получает все записи параметров воздуховодов из базы данных.
    ///
    /// Метод устанавливает соединение с базой данных SQLite, выполняет SQL-запрос
    /// для выборки всех записей из таблицы "DuctParameters" и возвращает их 
    /// в виде списка объектов типа <see cref="DuctParameters"/>.
    ///
    /// В случае ошибки при подключении или выполнении запроса, метод отображает 
    /// сообщение об ошибке пользователю и возвращает пустой список.
    /// </summary>
    /// <returns>
    /// Список объектов <see cref="DuctParameters"/>, содержащий все записи из таблицы.
    /// Если произошла ошибка, возвращается пустой список.
    /// </returns>
    public List<DuctParameters> GetAll()
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection.Query<DuctParameters>("SELECT * FROM DuctParameters").ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при получении данных: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return [];
        }
    }
    /// <summary>
    /// Добавляет новые параметры воздуховода в базу данных.
    ///
    /// Метод выполняет валидацию обязательных полей перед сохранением.
    /// Если обязательные поля не заполнены, отображается предупреждение пользователю,
    /// и данные не сохраняются.
    ///
    /// При успешной валидации метод устанавливает соединение с базой данных SQLite
    /// и выполняет SQL-запрос для вставки новых данных в таблицу "DuctParameters".
    ///
    /// В случае возникновения ошибки при подключении или выполнении запроса,
    /// отображается сообщение об ошибке пользователю.
    /// </summary>
    /// <param name="parameters">
    /// Объект типа <see cref="DuctParameters"/>, содержащий данные для добавления в базу.
    /// </param>
    public void Add(DuctParameters parameters)
    {
        try
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(parameters.Material))
            {
                MessageBox.Show("Поле 'Материал' обязательно для заполнения", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(parameters.Shape))
            {
                MessageBox.Show("Поле 'Сечение' обязательно для заполнения", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            connection.Execute(@"
                INSERT INTO DuctParameters 
                (Material, Shape, ExternalInsulation, InternalInsulation, Size, Thickness)
                VALUES 
                (@Material, @Shape, @ExternalInsulation, @InternalInsulation, @Size, @Thickness)",
                parameters);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при добавлении данных: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    /// <summary>
    /// Обновляет существующую запись параметров воздуховода в базе данных.
    ///
    /// Метод устанавливает соединение с базой данных SQLite и выполняет SQL-запрос для
    /// обновления записи в таблице "DuctParameters", соответствующей заданному идентификатору.
    /// Новые значения полей берутся из объекта <see cref="DuctParameters"/>.
    ///
    /// В случае возникновения ошибки при подключении или выполнении запроса,
    /// метод отображает сообщение об ошибке пользователю.
    /// </summary>
    /// <param name="parameters">
    /// Объект типа <see cref="DuctParameters"/>, содержащий обновлённые данные для записи.
    /// Поле <c>Id</c> должно быть заполнено и соответствовать записи, которую необходимо обновить.
    /// </param>
    public void Update(DuctParameters parameters)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            connection.Execute(@"
                    UPDATE DuctParameters 
                    SET Material = @Material,
                        Shape = @Shape,
                        ExternalInsulation = @ExternalInsulation,
                        InternalInsulation = @InternalInsulation,
                        Size = @Size,
                        Thickness = @Thickness
                    WHERE Id = @Id",
                parameters);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    /// <summary>
    /// Удаляет запись параметров воздуховода из базы данных по заданному идентификатору.
    /// 
    /// Метод устанавливает соединение с базой данных SQLite и выполняет SQL-запрос для
    /// удаления записи из таблицы "DuctParameters", у которой поле <c>Id</c> соответствует заданному значению.
    /// 
    /// В случае возникновения ошибки при подключении или выполнении запроса,
    /// метод отображает сообщение об ошибке пользователю.
    /// </summary>
    /// <param name="id">
    /// Идентификатор (<c>Id</c>) записи, которую необходимо удалить из базы данных.
    /// </param>
    public void Delete(int id)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Проверка существования записи
            var record = connection.QueryFirstOrDefault<DuctParameters>("SELECT * FROM DuctParameters WHERE Id = @Id", new { Id = id });

            if (record == null)
            {
                MessageBox.Show("Запись с указанным Id не найдена.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Удаление записи
            connection.Execute("DELETE FROM DuctParameters WHERE Id = @Id", new { Id = id });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении данных: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public DuctParameters FindMatching(string material, string shape, 
        string externalInsulation, string internalInsulation, double size)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection.QueryFirstOrDefault<DuctParameters>(@"
                    SELECT * FROM DuctParameters 
                    WHERE Material = @Material 
                    AND Shape = @Shape 
                    AND ExternalInsulation = @ExternalInsulation 
                    AND InternalInsulation = @InternalInsulation 
                    AND abs(Size - @Size) < 0.001",
                new 
                { 
                    Material = material,
                    Shape = shape,
                    ExternalInsulation = externalInsulation,
                    InternalInsulation = internalInsulation,
                    Size = size
                });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при поиске данных: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }
}
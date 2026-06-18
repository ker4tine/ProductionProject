using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class CounterpartyImportHelper
    {
        public static bool ImportFromFile(string connectionString)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Выберите файл Заказчики.json";
                dialog.Filter = "JSON-файлы (*.json)|*.json|Все файлы (*.*)|*.*";
                if (dialog.ShowDialog() != DialogResult.OK) return false;

                try
                {
                    ImportResult result = Import(connectionString, dialog.FileName);
                    MessageBox.Show(
                        "Импорт завершён.\nДобавлено: " + result.Inserted +
                        "\nОбновлено: " + result.Updated +
                        "\nПропущено: " + result.Skipped,
                        "Импорт контрагентов",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Не удалось импортировать контрагентов. Проверьте структуру выбранного JSON-файла.\n\n" + ex.Message,
                        "Ошибка импорта",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        private static ImportResult Import(string connectionString, string filePath)
        {
            string json = File.ReadAllText(filePath);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            object[] rows = serializer.DeserializeObject(json) as object[];
            if (rows == null) throw new InvalidDataException("Корневой элемент JSON должен быть массивом.");

            ImportResult result = new ImportResult();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (object row in rows)
                        {
                            Dictionary<string, object> item = row as Dictionary<string, object>;
                            if (item == null) { result.Skipped++; continue; }

                            string id = GetValue(item, "id");
                            string name = GetValue(item, "name");
                            string inn = GetValue(item, "inn");
                            string address = GetValue(item, "addres");
                            if (string.IsNullOrWhiteSpace(address)) address = GetValue(item, "address");
                            string phone = GetValue(item, "phone");
                            string type = GetValue(item, "type");

                            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
                            {
                                result.Skipped++;
                                continue;
                            }

                            bool exists;
                            using (SqlCommand check = new SqlCommand("SELECT COUNT(*) FROM Counterparties WHERE counterparty_id=@id", connection, transaction))
                            {
                                check.Parameters.AddWithValue("@id", id);
                                exists = Convert.ToInt32(check.ExecuteScalar()) > 0;
                            }

                            string sql = exists
                                ? "UPDATE Counterparties SET counterparty_name=@name, inn=@inn, address=@address, phone=@phone, counterparty_type=@type WHERE counterparty_id=@id"
                                : "INSERT INTO Counterparties (counterparty_id, counterparty_name, inn, address, phone, counterparty_type) VALUES (@id,@name,@inn,@address,@phone,@type)";

                            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@id", id);
                                command.Parameters.AddWithValue("@name", name);
                                command.Parameters.AddWithValue("@inn", string.IsNullOrWhiteSpace(inn) ? (object)DBNull.Value : inn);
                                command.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address);
                                command.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                                command.Parameters.AddWithValue("@type", type);
                                command.ExecuteNonQuery();
                            }

                            if (exists) result.Updated++; else result.Inserted++;
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return result;
        }

        private static string GetValue(Dictionary<string, object> item, string key)
        {
            object value;
            return item.TryGetValue(key, out value) && value != null ? Convert.ToString(value).Trim() : "";
        }

        private sealed class ImportResult
        {
            public int Inserted;
            public int Updated;
            public int Skipped;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class CounterpartyImportHelper
    {
        private const string SelectSql = @"
SELECT counterparty_id AS [ID], counterparty_name AS [Наименование],
       inn AS [ИНН], address AS [Адрес], phone AS [Телефон],
       counterparty_type AS [Тип]
FROM Counterparties
ORDER BY counterparty_name";

        public static TabPage CreateCounterpartiesTab(string connectionString, bool canEdit)
        {
            TabPage page = new TabPage("Контрагенты") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            if (canEdit)
            {
                Button importButton = UiHelper.CreateButton("Импорт JSON", 130);
                importButton.Click += (s, e) => ImportFromFile(connectionString, grid, source);
                toolbar.Controls.Add(importButton);
            }

            Button refreshButton = UiHelper.CreateButton("Обновить", 100);
            refreshButton.Click += (s, e) => LoadGrid(connectionString, grid, source);
            toolbar.Controls.Add(refreshButton);
            UiHelper.AddStartsWithSearch(toolbar, source, "Наименование", "Поиск по наименованию:");

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(UiHelper.CreateTablePanel(grid, source));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Контрагенты", "Импорт заказчиков и поставщиков из JSON"));
            LoadGrid(connectionString, grid, source);
            return page;
        }

        private static void ImportFromFile(string connectionString, DataGridView grid, BindingSource source)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Выберите файл Заказчики.json";
                dialog.Filter = "JSON-файлы (*.json)|*.json|Все файлы (*.*)|*.*";
                if (dialog.ShowDialog() != DialogResult.OK) return;

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
                    LoadGrid(connectionString, grid, source);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка импорта: " + ex.Message, "Импорт контрагентов", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                                command.Parameters.AddWithValue("@inn", (object)inn ?? DBNull.Value);
                                command.Parameters.AddWithValue("@address", (object)address ?? DBNull.Value);
                                command.Parameters.AddWithValue("@phone", (object)phone ?? DBNull.Value);
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

        private static void LoadGrid(string connectionString, DataGridView grid, BindingSource source)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(SelectSql, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                    if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки контрагентов: " + ex.Message);
            }
        }

        private sealed class ImportResult
        {
            public int Inserted;
            public int Updated;
            public int Skipped;
        }
    }
}

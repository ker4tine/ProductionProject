using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class CrudHelper
    {
        public static TabPage CreateDictionaryTab(string connectionString, string title, string tableName, bool hasUnit, bool hasPrice, bool canEdit)
        {
            string query = BuildQuery(tableName, hasUnit, hasPrice);
            TabPage page = new TabPage(title) { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            if (canEdit)
            {
                Button btnAdd = UiHelper.CreateButton("Добавить", 100);
                Button btnEdit = UiHelper.CreateButton("Изменить", 100);
                Button btnDelete = UiHelper.CreateButton("Удалить", 100);

                btnAdd.Click += (s, e) => AddItem(connectionString, grid, source, query, title, tableName, hasUnit, hasPrice);
                btnEdit.Click += (s, e) => EditItem(connectionString, grid, source, query, title, tableName, hasUnit, hasPrice);
                btnDelete.Click += (s, e) => DeleteItem(connectionString, grid, source, query, tableName);

                toolbar.Controls.Add(btnAdd);
                toolbar.Controls.Add(btnEdit);
                toolbar.Controls.Add(btnDelete);
            }

            Button btnRefresh = UiHelper.CreateButton("Обновить", 100);
            btnRefresh.Click += (s, e) => LoadGrid(connectionString, grid, source, query);
            toolbar.Controls.Add(btnRefresh);
            UiHelper.AddStartsWithSearch(toolbar, source, "Артикул", "Поиск по артикулу:");

            Panel tablePanel = UiHelper.CreateTablePanel(grid, source);
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(tablePanel);
            content.Controls.Add(toolbar);

            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel(title, "Справочник производственной системы"));
            LoadGrid(connectionString, grid, source, query);
            return page;
        }

        private static string BuildQuery(string tableName, bool hasUnit, bool hasPrice)
        {
            if (hasUnit && hasPrice)
                return $"SELECT id AS [ID], code AS [Артикул], name AS [Наименование], unit AS [Единица измерения], price AS [Цена] FROM {tableName}";
            if (hasUnit)
                return $"SELECT id AS [ID], code AS [Артикул], name AS [Наименование], unit AS [Единица измерения] FROM {tableName}";
            return $"SELECT id AS [ID], code AS [Артикул], name AS [Наименование], price AS [Цена] FROM {tableName}";
        }

        private static void LoadGrid(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                    HideIdColumn(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private static void HideIdColumn(DataGridView grid)
        {
            if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
        }

        private static int GetSelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку.");
                return 0;
            }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void AddItem(string connectionString, DataGridView grid, BindingSource source, string query, string title, string tableName, bool hasUnit, bool hasPrice)
        {
            using (DictionaryEditForm form = new DictionaryEditForm("Добавление: " + title, hasUnit, hasPrice, GenerateCode(connectionString, tableName), "", "", 0, true))
            {
                if (form.ShowDialog() != DialogResult.OK) return;

                string sql;
                if (hasUnit && hasPrice)
                    sql = $"INSERT INTO {tableName} (code, name, unit, price) VALUES (@code, @name, @unit, @price)";
                else if (hasUnit)
                    sql = $"INSERT INTO {tableName} (code, name, unit) VALUES (@code, @name, @unit)";
                else
                    sql = $"INSERT INTO {tableName} (code, name, price) VALUES (@code, @name, @price)";

                ExecuteSave(connectionString, sql, form, hasUnit, hasPrice, 0);
                LoadGrid(connectionString, grid, source, query);
            }
        }

        private static string GenerateCode(string connectionString, string tableName)
        {
            string prefix = tableName == "Products" ? "PR" : tableName == "Materials" ? "MT" : "OP";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand($"SELECT ISNULL(MAX(id), 0) + 1 FROM {tableName}", connection))
            {
                connection.Open();
                int nextId = Convert.ToInt32(command.ExecuteScalar());
                return prefix + nextId.ToString("000");
            }
        }

        private static void EditItem(string connectionString, DataGridView grid, BindingSource source, string query, string title, string tableName, bool hasUnit, bool hasPrice)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;

            string code = Convert.ToString(grid.CurrentRow.Cells["Артикул"].Value);
            string name = Convert.ToString(grid.CurrentRow.Cells["Наименование"].Value);
            string unit = hasUnit ? Convert.ToString(grid.CurrentRow.Cells["Единица измерения"].Value) : "";
            decimal price = hasPrice ? Convert.ToDecimal(grid.CurrentRow.Cells["Цена"].Value) : 0;

            using (DictionaryEditForm form = new DictionaryEditForm("Изменение: " + title, hasUnit, hasPrice, code, name, unit, price, true))
            {
                if (form.ShowDialog() != DialogResult.OK) return;

                string sql;
                if (hasUnit && hasPrice)
                    sql = $"UPDATE {tableName} SET code = @code, name = @name, unit = @unit, price = @price WHERE id = @id";
                else if (hasUnit)
                    sql = $"UPDATE {tableName} SET code = @code, name = @name, unit = @unit WHERE id = @id";
                else
                    sql = $"UPDATE {tableName} SET code = @code, name = @name, price = @price WHERE id = @id";

                ExecuteSave(connectionString, sql, form, hasUnit, hasPrice, id);
                LoadGrid(connectionString, grid, source, query);
            }
        }

        private static void DeleteItem(string connectionString, DataGridView grid, BindingSource source, string query, string tableName)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;

            if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand($"DELETE FROM {tableName} WHERE id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(connectionString, grid, source, query);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления. Возможно, запись используется в заказах или спецификациях.\n" + ex.Message);
            }
        }

        private static void ExecuteSave(string connectionString, string sql, DictionaryEditForm form, bool hasUnit, bool hasPrice, int id)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@code", form.ItemCode);
                command.Parameters.AddWithValue("@name", form.ItemName);
                if (hasUnit) command.Parameters.AddWithValue("@unit", form.ItemUnit);
                if (hasPrice) command.Parameters.AddWithValue("@price", form.ItemPrice);
                if (id > 0) command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}

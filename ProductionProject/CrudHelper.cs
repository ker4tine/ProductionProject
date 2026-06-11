using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class CrudHelper
    {
        public static TabPage CreateDictionaryTab(
            string connectionString,
            string title,
            string tableName,
            bool hasUnit,
            bool hasPrice,
            bool canEdit)
        {
            string query = BuildQuery(tableName, hasUnit, hasPrice);

            TabPage page = new TabPage(title);
            DataGridView grid = CreateGrid();
            FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38 };

            Button btnRefresh = new Button { Text = "Обновить", Width = 100 };
            btnRefresh.Click += (s, e) => LoadGrid(connectionString, grid, query);
            panel.Controls.Add(btnRefresh);

            if (canEdit)
            {
                Button btnAdd = new Button { Text = "Добавить", Width = 100 };
                Button btnEdit = new Button { Text = "Изменить", Width = 100 };
                Button btnDelete = new Button { Text = "Удалить", Width = 100 };

                btnAdd.Click += (s, e) => AddItem(connectionString, grid, query, title, tableName, hasUnit, hasPrice);
                btnEdit.Click += (s, e) => EditItem(connectionString, grid, query, title, tableName, hasUnit, hasPrice);
                btnDelete.Click += (s, e) => DeleteItem(connectionString, grid, query, tableName);

                panel.Controls.Add(btnAdd);
                panel.Controls.Add(btnEdit);
                panel.Controls.Add(btnDelete);
            }

            page.Controls.Add(grid);
            page.Controls.Add(panel);
            LoadGrid(connectionString, grid, query);
            return page;
        }

        private static string BuildQuery(string tableName, bool hasUnit, bool hasPrice)
        {
            if (hasUnit && hasPrice)
                return $"SELECT id AS [Код], code AS [Артикул], name AS [Наименование], unit AS [Единица измерения], price AS [Цена] FROM {tableName}";

            if (hasUnit)
                return $"SELECT id AS [Код], code AS [Артикул], name AS [Наименование], unit AS [Единица измерения] FROM {tableName}";

            return $"SELECT id AS [Код], code AS [Артикул], name AS [Наименование], price AS [Цена] FROM {tableName}";
        }

        private static DataGridView CreateGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
        }

        private static void LoadGrid(string connectionString, DataGridView grid, string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    grid.DataSource = table;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private static int GetSelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку.");
                return 0;
            }

            return Convert.ToInt32(grid.CurrentRow.Cells["Код"].Value);
        }

        private static void AddItem(string connectionString, DataGridView grid, string query, string title, string tableName, bool hasUnit, bool hasPrice)
        {
            using (DictionaryEditForm form = new DictionaryEditForm("Добавление: " + title, hasUnit, hasPrice))
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
                LoadGrid(connectionString, grid, query);
            }
        }

        private static void EditItem(string connectionString, DataGridView grid, string query, string title, string tableName, bool hasUnit, bool hasPrice)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;

            string code = Convert.ToString(grid.CurrentRow.Cells["Артикул"].Value);
            string name = Convert.ToString(grid.CurrentRow.Cells["Наименование"].Value);
            string unit = hasUnit ? Convert.ToString(grid.CurrentRow.Cells["Единица измерения"].Value) : "";
            decimal price = hasPrice ? Convert.ToDecimal(grid.CurrentRow.Cells["Цена"].Value) : 0;

            using (DictionaryEditForm form = new DictionaryEditForm("Изменение: " + title, hasUnit, hasPrice, code, name, unit, price))
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
                LoadGrid(connectionString, grid, query);
            }
        }

        private static void DeleteItem(string connectionString, DataGridView grid, string query, string tableName)
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

                LoadGrid(connectionString, grid, query);
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

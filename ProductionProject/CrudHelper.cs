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
            string query = BuildQuery(tableName);
            TabPage page = new TabPage(title) { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            if (canEdit)
            {
                Button add = UiHelper.CreateButton("Добавить", 100);
                Button edit = UiHelper.CreateButton("Изменить", 100);
                Button delete = UiHelper.CreateButton("Удалить", 100);
                add.Click += (s, e) => AddItem(connectionString, grid, source, query, title, tableName, hasUnit, hasPrice);
                edit.Click += (s, e) => EditItem(connectionString, grid, source, query, title, tableName, hasUnit, hasPrice);
                delete.Click += (s, e) => DeleteItem(connectionString, grid, source, query, tableName);
                toolbar.Controls.Add(add);
                toolbar.Controls.Add(edit);
                toolbar.Controls.Add(delete);
            }

            Button refresh = UiHelper.CreateButton("Обновить", 100);
            refresh.Click += (s, e) => LoadGrid(connectionString, grid, source, query);
            toolbar.Controls.Add(refresh);
            UiHelper.AddStartsWithSearch(toolbar, source, "Артикул", "Поиск по артикулу:");

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(UiHelper.CreateTablePanel(grid, source));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel(title, "Справочник производственной системы"));
            LoadGrid(connectionString, grid, source, query);
            return page;
        }

        private static string BuildQuery(string tableName)
        {
            if (tableName == "Products")
                return "SELECT product_id AS [ID], product_code AS [Артикул], product_name AS [Наименование], unit_name AS [Единица измерения] FROM Products";
            if (tableName == "Materials")
                return "SELECT material_id AS [ID], material_code AS [Артикул], material_name AS [Наименование], unit_name AS [Единица измерения], material_price AS [Цена] FROM Materials";
            if (tableName == "Operations")
                return "SELECT operation_id AS [ID], operation_code AS [Артикул], operation_name AS [Наименование], operation_price AS [Цена] FROM Operations";
            throw new ArgumentException("Неизвестный справочник: " + tableName);
        }

        private static string IdColumn(string tableName)
        {
            if (tableName == "Products") return "product_id";
            if (tableName == "Materials") return "material_id";
            if (tableName == "Operations") return "operation_id";
            throw new ArgumentException("Неизвестный справочник: " + tableName);
        }

        private static string CodeColumn(string tableName)
        {
            if (tableName == "Products") return "product_code";
            if (tableName == "Materials") return "material_code";
            if (tableName == "Operations") return "operation_code";
            throw new ArgumentException("Неизвестный справочник: " + tableName);
        }

        private static string NameColumn(string tableName)
        {
            if (tableName == "Products") return "product_name";
            if (tableName == "Materials") return "material_name";
            if (tableName == "Operations") return "operation_name";
            throw new ArgumentException("Неизвестный справочник: " + tableName);
        }

        private static string PriceColumn(string tableName)
        {
            return tableName == "Materials" ? "material_price" : "operation_price";
        }

        private static void LoadGrid(string cs, DataGridView grid, BindingSource source, string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(cs))
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                    if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки данных: " + ex.Message); }
        }

        private static int SelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null) { MessageBox.Show("Выберите строку."); return 0; }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void AddItem(string cs, DataGridView grid, BindingSource source, string query, string title, string tableName, bool hasUnit, bool hasPrice)
        {
            using (DictionaryEditForm form = new DictionaryEditForm("Добавление: " + title, hasUnit, hasPrice, GenerateCode(cs, tableName), "", "", 0, true))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                string sql;
                if (hasUnit && hasPrice)
                    sql = $"INSERT INTO {tableName} ({CodeColumn(tableName)}, {NameColumn(tableName)}, unit_name, {PriceColumn(tableName)}) VALUES (@code,@name,@unit,@price)";
                else if (hasUnit)
                    sql = $"INSERT INTO {tableName} ({CodeColumn(tableName)}, {NameColumn(tableName)}, unit_name) VALUES (@code,@name,@unit)";
                else
                    sql = $"INSERT INTO {tableName} ({CodeColumn(tableName)}, {NameColumn(tableName)}, {PriceColumn(tableName)}) VALUES (@code,@name,@price)";
                ExecuteSave(cs, sql, form, hasUnit, hasPrice, 0);
                LoadGrid(cs, grid, source, query);
            }
        }

        private static string GenerateCode(string cs, string tableName)
        {
            string prefix = tableName == "Products" ? "PR" : tableName == "Materials" ? "MT" : "OP";
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand($"SELECT ISNULL(MAX({IdColumn(tableName)}),0)+1 FROM {tableName}", connection))
            {
                connection.Open();
                return prefix + Convert.ToInt32(command.ExecuteScalar()).ToString("000");
            }
        }

        private static void EditItem(string cs, DataGridView grid, BindingSource source, string query, string title, string tableName, bool hasUnit, bool hasPrice)
        {
            int id = SelectedId(grid); if (id == 0) return;
            string code = Convert.ToString(grid.CurrentRow.Cells["Артикул"].Value);
            string name = Convert.ToString(grid.CurrentRow.Cells["Наименование"].Value);
            string unit = hasUnit ? Convert.ToString(grid.CurrentRow.Cells["Единица измерения"].Value) : "";
            decimal price = hasPrice ? Convert.ToDecimal(grid.CurrentRow.Cells["Цена"].Value) : 0;

            using (DictionaryEditForm form = new DictionaryEditForm("Изменение: " + title, hasUnit, hasPrice, code, name, unit, price, true))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                string sql;
                if (hasUnit && hasPrice)
                    sql = $"UPDATE {tableName} SET {CodeColumn(tableName)}=@code, {NameColumn(tableName)}=@name, unit_name=@unit, {PriceColumn(tableName)}=@price WHERE {IdColumn(tableName)}=@id";
                else if (hasUnit)
                    sql = $"UPDATE {tableName} SET {CodeColumn(tableName)}=@code, {NameColumn(tableName)}=@name, unit_name=@unit WHERE {IdColumn(tableName)}=@id";
                else
                    sql = $"UPDATE {tableName} SET {CodeColumn(tableName)}=@code, {NameColumn(tableName)}=@name, {PriceColumn(tableName)}=@price WHERE {IdColumn(tableName)}=@id";
                ExecuteSave(cs, sql, form, hasUnit, hasPrice, id);
                LoadGrid(cs, grid, source, query);
            }
        }

        private static void DeleteItem(string cs, DataGridView grid, BindingSource source, string query, string tableName)
        {
            int id = SelectedId(grid); if (id == 0) return;
            if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try
            {
                using (SqlConnection connection = new SqlConnection(cs))
                using (SqlCommand command = new SqlCommand($"DELETE FROM {tableName} WHERE {IdColumn(tableName)}=@id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(cs, grid, source, query);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка удаления. Возможно, запись используется в заказах или спецификациях.\n" + ex.Message); }
        }

        private static void ExecuteSave(string cs, string sql, DictionaryEditForm form, bool hasUnit, bool hasPrice, int id)
        {
            using (SqlConnection connection = new SqlConnection(cs))
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

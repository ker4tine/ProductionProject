using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class OrderCrudHelper
    {
        public static TabPage CreateOrdersTab(string connectionString, bool canEdit)
        {
            string query = @"
                SELECT co.id AS [ID], c.name AS [Заказчик], c.phone AS [Телефон], co.order_date AS [Дата заказа]
                FROM CustomerOrders co
                JOIN Counterparties c ON co.customer_id = c.id
                ORDER BY co.order_date DESC, co.id DESC";

            return CreateOrderTab(connectionString, canEdit, "Заказы", "Заказы покупателей", "Работа с заказами клиентов", query, AddOrder, EditOrder, DeleteOrder);
        }

        public static TabPage CreateOrderItemsTab(string connectionString, bool canEdit)
        {
            string query = @"
                SELECT coi.id AS [ID], coi.order_id AS [OrderID], coi.product_id AS [ProductID],
                       c.name AS [Заказчик], co.order_date AS [Дата заказа], p.name AS [Продукция], coi.quantity AS [Количество], p.unit AS [Ед. изм.]
                FROM CustomerOrderItems coi
                JOIN CustomerOrders co ON coi.order_id = co.id
                JOIN Counterparties c ON co.customer_id = c.id
                JOIN Products p ON coi.product_id = p.id
                ORDER BY co.order_date DESC, coi.id DESC";

            return CreateOrderTab(connectionString, canEdit, "Позиции заказов", "Позиции заказов", "Продукция и количество по заказам", query, AddOrderItem, EditOrderItem, DeleteOrderItem);
        }

        private static TabPage CreateOrderTab(string connectionString, bool canEdit, string tabTitle, string header, string subtitle, string query,
            Action<string, DataGridView, BindingSource, string> addAction,
            Action<string, DataGridView, BindingSource, string> editAction,
            Action<string, DataGridView, BindingSource, string> deleteAction)
        {
            TabPage page = new TabPage(tabTitle) { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            grid.DataBindingComplete += (s, e) => HideServiceColumns(grid);
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            if (canEdit)
            {
                Button btnAdd = UiHelper.CreateButton("Добавить", 100);
                Button btnEdit = UiHelper.CreateButton("Изменить", 100);
                Button btnDelete = UiHelper.CreateButton("Удалить", 100);
                btnAdd.Click += (s, e) => addAction(connectionString, grid, source, query);
                btnEdit.Click += (s, e) => editAction(connectionString, grid, source, query);
                btnDelete.Click += (s, e) => deleteAction(connectionString, grid, source, query);
                toolbar.Controls.Add(btnAdd);
                toolbar.Controls.Add(btnEdit);
                toolbar.Controls.Add(btnDelete);
            }

            Button btnRefresh = UiHelper.CreateButton("Обновить", 100);
            btnRefresh.Click += (s, e) => LoadGrid(connectionString, grid, source, query);
            toolbar.Controls.Add(btnRefresh);

            Panel tablePanel = UiHelper.CreateTablePanel(grid, source);
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(tablePanel);
            content.Controls.Add(toolbar);

            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel(header, subtitle));
            LoadGrid(connectionString, grid, source, query);
            return page;
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
                    HideServiceColumns(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private static void HideServiceColumns(DataGridView grid)
        {
            HideColumn(grid, "ID");
            HideColumn(grid, "OrderID");
            HideColumn(grid, "ProductID");
        }

        private static void HideColumn(DataGridView grid, string name)
        {
            if (grid.Columns.Contains(name)) grid.Columns[name].Visible = false;
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

        private static int GetHiddenInt(DataGridView grid, string columnName)
        {
            return Convert.ToInt32(grid.CurrentRow.Cells[columnName].Value);
        }

        private static void AddOrder(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            using (OrderEditForm form = new OrderEditForm(connectionString, "Добавление заказа"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("INSERT INTO CustomerOrders (customer_id, order_date) VALUES (@customer, @date)", connection))
                {
                    command.Parameters.AddWithValue("@customer", form.CustomerId);
                    command.Parameters.AddWithValue("@date", form.OrderDate);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(connectionString, grid, source, query);
            }
        }

        private static void EditOrder(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;
            object customerId = GetCustomerId(connectionString, id);
            DateTime orderDate = Convert.ToDateTime(grid.CurrentRow.Cells["Дата заказа"].Value);

            using (OrderEditForm form = new OrderEditForm(connectionString, "Изменение заказа", customerId, orderDate))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("UPDATE CustomerOrders SET customer_id = @customer, order_date = @date WHERE id = @id", connection))
                {
                    command.Parameters.AddWithValue("@customer", form.CustomerId);
                    command.Parameters.AddWithValue("@date", form.OrderDate);
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(connectionString, grid, source, query);
            }
        }

        private static object GetCustomerId(string connectionString, int orderId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("SELECT customer_id FROM CustomerOrders WHERE id = @id", connection))
            {
                command.Parameters.AddWithValue("@id", orderId);
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        private static void DeleteOrder(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;
            if (MessageBox.Show("Удалить выбранный заказ?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("DELETE FROM CustomerOrders WHERE id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(connectionString, grid, source, query);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления. Возможно, у заказа есть позиции. Сначала удалите позиции заказа.\n" + ex.Message);
            }
        }

        private static void AddOrderItem(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            using (OrderItemEditForm form = new OrderItemEditForm(connectionString, "Добавление позиции заказа"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                SaveOrderItem(connectionString, "INSERT INTO CustomerOrderItems (order_id, product_id, quantity) VALUES (@order, @product, @quantity)", form, 0);
                LoadGrid(connectionString, grid, source, query);
            }
        }

        private static void EditOrderItem(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;
            int orderId = GetHiddenInt(grid, "OrderID");
            int productId = GetHiddenInt(grid, "ProductID");
            decimal quantity = Convert.ToDecimal(grid.CurrentRow.Cells["Количество"].Value);

            using (OrderItemEditForm form = new OrderItemEditForm(connectionString, "Изменение позиции заказа", orderId, productId, quantity))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                SaveOrderItem(connectionString, "UPDATE CustomerOrderItems SET order_id = @order, product_id = @product, quantity = @quantity WHERE id = @id", form, id);
                LoadGrid(connectionString, grid, source, query);
            }
        }

        private static void DeleteOrderItem(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;
            if (MessageBox.Show("Удалить выбранную позицию заказа?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("DELETE FROM CustomerOrderItems WHERE id = @id", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadGrid(connectionString, grid, source, query);
        }

        private static void SaveOrderItem(string connectionString, string sql, OrderItemEditForm form, int id)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@order", form.OrderId);
                command.Parameters.AddWithValue("@product", form.ProductId);
                command.Parameters.AddWithValue("@quantity", form.Quantity);
                if (id > 0) command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}

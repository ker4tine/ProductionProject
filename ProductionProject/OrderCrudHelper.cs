using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class OrderCrudHelper
    {
        private const string OrdersQuery = @"
SELECT co.customer_order_id AS [ID], c.counterparty_name AS [Заказчик],
       c.phone AS [Телефон], co.order_date AS [Дата заказа]
FROM CustomerOrders co
JOIN Counterparties c ON co.customer_id = c.counterparty_id
ORDER BY co.order_date DESC, co.customer_order_id DESC";

        private const string ItemsQuery = @"
SELECT coi.customer_order_item_id AS [ID], coi.customer_order_id AS [OrderID],
       coi.product_id AS [ProductID], c.counterparty_name AS [Заказчик],
       co.order_date AS [Дата заказа], p.product_name AS [Продукция],
       coi.quantity AS [Количество], p.unit_name AS [Ед. изм.]
FROM CustomerOrderItems coi
JOIN CustomerOrders co ON coi.customer_order_id = co.customer_order_id
JOIN Counterparties c ON co.customer_id = c.counterparty_id
JOIN Products p ON coi.product_id = p.product_id
ORDER BY co.order_date DESC, coi.customer_order_item_id DESC";

        public static TabPage CreateOrdersTab(string cs, bool canEdit)
        {
            return CreateTab(cs, canEdit, true, "Заказы", "Заказы покупателей", "Работа с заказами клиентов", OrdersQuery, AddOrder, EditOrder, DeleteOrder);
        }

        public static TabPage CreateOrderItemsTab(string cs, bool canEdit)
        {
            return CreateTab(cs, canEdit, false, "Позиции заказов", "Позиции заказов", "Продукция и количество по заказам", ItemsQuery, AddOrderItem, EditOrderItem, DeleteOrderItem);
        }

        private static TabPage CreateTab(string cs, bool canEdit, bool showImport, string tabTitle, string header, string subtitle, string query,
            Action<string, DataGridView, BindingSource, string> add,
            Action<string, DataGridView, BindingSource, string> edit,
            Action<string, DataGridView, BindingSource, string> delete)
        {
            TabPage page = new TabPage(tabTitle) { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            grid.DataBindingComplete += (s, e) => HideServiceColumns(grid);
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            if (canEdit)
            {
                Button addButton = UiHelper.CreateButton("Добавить", 100);
                Button editButton = UiHelper.CreateButton("Изменить", 100);
                Button deleteButton = UiHelper.CreateButton("Удалить", 100);
                addButton.Click += (s, e) => add(cs, grid, source, query);
                editButton.Click += (s, e) => edit(cs, grid, source, query);
                deleteButton.Click += (s, e) => delete(cs, grid, source, query);
                toolbar.Controls.Add(addButton);
                toolbar.Controls.Add(editButton);
                toolbar.Controls.Add(deleteButton);
            }

            if (showImport)
            {
                Button importButton = UiHelper.CreateButton("Импорт JSON", 130);
                importButton.Click += (s, e) => CounterpartyImportHelper.ImportFromFile(cs);
                toolbar.Controls.Add(importButton);
            }

            UiHelper.AddStartsWithSearch(toolbar, source, "Заказчик", "Поиск по заказчику:");

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(UiHelper.CreateTablePanel(grid, source));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel(header, subtitle));
            LoadGrid(cs, grid, source, query);
            return page;
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
                    HideServiceColumns(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void HideServiceColumns(DataGridView grid)
        {
            foreach (string name in new[] { "ID", "OrderID", "ProductID" })
                if (grid.Columns.Contains(name)) grid.Columns[name].Visible = false;
        }

        private static int SelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку.", "Данные не выбраны", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static int HiddenInt(DataGridView grid, string name)
        {
            return Convert.ToInt32(grid.CurrentRow.Cells[name].Value);
        }

        private static void AddOrder(string cs, DataGridView grid, BindingSource source, string query)
        {
            using (OrderEditForm form = new OrderEditForm(cs, "Добавление заказа"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                Execute(cs, "INSERT INTO CustomerOrders (customer_id, order_date) VALUES (@customer,@date)",
                    c => { c.Parameters.AddWithValue("@customer", form.CustomerId); c.Parameters.AddWithValue("@date", form.OrderDate); });
                LoadGrid(cs, grid, source, query);
            }
        }

        private static void EditOrder(string cs, DataGridView grid, BindingSource source, string query)
        {
            int id = SelectedId(grid); if (id == 0) return;
            object customerId = Scalar(cs, "SELECT customer_id FROM CustomerOrders WHERE customer_order_id=@id", id);
            DateTime date = Convert.ToDateTime(grid.CurrentRow.Cells["Дата заказа"].Value);
            using (OrderEditForm form = new OrderEditForm(cs, "Изменение заказа", customerId, date))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                Execute(cs, "UPDATE CustomerOrders SET customer_id=@customer, order_date=@date WHERE customer_order_id=@id",
                    c => { c.Parameters.AddWithValue("@customer", form.CustomerId); c.Parameters.AddWithValue("@date", form.OrderDate); c.Parameters.AddWithValue("@id", id); });
                LoadGrid(cs, grid, source, query);
            }
        }

        private static void DeleteOrder(string cs, DataGridView grid, BindingSource source, string query)
        {
            int id = SelectedId(grid); if (id == 0) return;
            if (MessageBox.Show("Удалить выбранный заказ?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                Execute(cs, "DELETE FROM CustomerOrders WHERE customer_order_id=@id", c => c.Parameters.AddWithValue("@id", id));
                LoadGrid(cs, grid, source, query);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить заказ. Возможно, у него есть позиции.\n\n" + ex.Message, "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void AddOrderItem(string cs, DataGridView grid, BindingSource source, string query)
        {
            using (OrderItemEditForm form = new OrderItemEditForm(cs, "Добавление позиции заказа"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                SaveItem(cs, "INSERT INTO CustomerOrderItems (customer_order_id, product_id, quantity) VALUES (@order,@product,@quantity)", form, 0);
                LoadGrid(cs, grid, source, query);
            }
        }

        private static void EditOrderItem(string cs, DataGridView grid, BindingSource source, string query)
        {
            int id = SelectedId(grid); if (id == 0) return;
            int orderId = HiddenInt(grid, "OrderID");
            int productId = HiddenInt(grid, "ProductID");
            decimal quantity = Convert.ToDecimal(grid.CurrentRow.Cells["Количество"].Value);
            using (OrderItemEditForm form = new OrderItemEditForm(cs, "Изменение позиции заказа", orderId, productId, quantity))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                SaveItem(cs, "UPDATE CustomerOrderItems SET customer_order_id=@order, product_id=@product, quantity=@quantity WHERE customer_order_item_id=@id", form, id);
                LoadGrid(cs, grid, source, query);
            }
        }

        private static void DeleteOrderItem(string cs, DataGridView grid, BindingSource source, string query)
        {
            int id = SelectedId(grid); if (id == 0) return;
            if (MessageBox.Show("Удалить выбранную позицию заказа?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Execute(cs, "DELETE FROM CustomerOrderItems WHERE customer_order_item_id=@id", c => c.Parameters.AddWithValue("@id", id));
            LoadGrid(cs, grid, source, query);
        }

        private static void SaveItem(string cs, string sql, OrderItemEditForm form, int id)
        {
            Execute(cs, sql, c =>
            {
                c.Parameters.AddWithValue("@order", form.OrderId);
                c.Parameters.AddWithValue("@product", form.ProductId);
                c.Parameters.AddWithValue("@quantity", form.Quantity);
                if (id > 0) c.Parameters.AddWithValue("@id", id);
            });
        }

        private static object Scalar(string cs, string sql, int id)
        {
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open(); return command.ExecuteScalar();
            }
        }

        private static void Execute(string cs, string sql, Action<SqlCommand> configure)
        {
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                configure(command); connection.Open(); command.ExecuteNonQuery();
            }
        }
    }
}

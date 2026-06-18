using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class CostTabHelper
    {
        private const string Query = @"
SELECT c.counterparty_name AS [Заказчик],
       p.product_name AS [Продукция],
       coi.quantity AS [Количество],
       SUM(coi.quantity *
           (ISNULL(s.material_qty, 0) * ISNULL(m.material_price, 0) +
            ISNULL(s.operation_qty, 0) * ISNULL(o.operation_price, 0))) AS [Полная стоимость]
FROM CustomerOrders co
JOIN Counterparties c ON co.customer_id = c.counterparty_id
JOIN CustomerOrderItems coi ON co.customer_order_id = coi.customer_order_id
JOIN Products p ON coi.product_id = p.product_id
JOIN Specifications s ON coi.product_id = s.product_id
LEFT JOIN Materials m ON s.material_id = m.material_id
LEFT JOIN Operations o ON s.operation_id = o.operation_id
GROUP BY c.counterparty_name, p.product_name, coi.quantity";

        public static TabPage CreateCostTab(string connectionString)
        {
            TabPage page = new TabPage("Расчет стоимости") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            UiHelper.AddStartsWithSearch(toolbar, source, "Заказчик", "Поиск по заказчику:");

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(UiHelper.CreateTablePanel(grid, source));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Расчет стоимости", "Итоговая стоимость заказов по спецификациям, материалам и операциям"));
            LoadGrid(connectionString, grid, source);
            return page;
        }

        private static void LoadGrid(string cs, DataGridView grid, BindingSource source)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(cs))
                using (SqlDataAdapter adapter = new SqlDataAdapter(Query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки расчета стоимости: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

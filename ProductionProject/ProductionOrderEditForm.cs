using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public class ProductionOrderEditForm : Form
    {
        private readonly string connectionString;
        private ComboBox cmbCustomerOrder;
        private ComboBox cmbProduct;
        private NumericUpDown nudQuantity;
        private DateTimePicker dtpProductionDate;

        public object CustomerOrderId { get; private set; }
        public int ProductId { get; private set; }
        public decimal Quantity { get; private set; }
        public DateTime ProductionDate { get; private set; }

        public ProductionOrderEditForm(string connectionString, string title,
            object customerOrderId = null, int productId = 0,
            decimal quantity = 1, DateTime? productionDate = null)
        {
            this.connectionString = connectionString;
            CustomerOrderId = customerOrderId;
            ProductId = productId;
            Quantity = quantity <= 0 ? 1 : quantity;
            ProductionDate = productionDate ?? DateTime.Today;

            BuildForm(title);
            LoadCustomerOrders();
            LoadProducts();
        }

        private void BuildForm(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(500, 340);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label { Text = "Заказ покупателя:", Location = new Point(30, 35), AutoSize = true });
            cmbCustomerOrder = new ComboBox { Location = new Point(180, 32), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cmbCustomerOrder);

            Controls.Add(new Label { Text = "Продукция:", Location = new Point(30, 82), AutoSize = true });
            cmbProduct = new ComboBox { Location = new Point(180, 79), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cmbProduct);

            Controls.Add(new Label { Text = "Количество:", Location = new Point(30, 129), AutoSize = true });
            nudQuantity = new NumericUpDown
            {
                Location = new Point(180, 126),
                Width = 260,
                Minimum = 1,
                Maximum = 100000,
                DecimalPlaces = 2,
                Value = Quantity
            };
            Controls.Add(nudQuantity);

            Controls.Add(new Label { Text = "Дата производства:", Location = new Point(30, 176), AutoSize = true });
            dtpProductionDate = new DateTimePicker
            {
                Location = new Point(180, 173),
                Width = 260,
                Format = DateTimePickerFormat.Short,
                Value = ProductionDate
            };
            Controls.Add(dtpProductionDate);

            Button ok = new Button { Text = "Сохранить", Location = new Point(135, 235), Width = 100 };
            ok.Click += BtnOk_Click;
            Controls.Add(ok);
            Controls.Add(new Button { Text = "Отмена", Location = new Point(250, 235), Width = 100, DialogResult = DialogResult.Cancel });
        }

        private void LoadCustomerOrders()
        {
            DataTable table = new DataTable();
            table.Columns.Add("customer_order_id", typeof(int));
            table.Columns.Add("display_name", typeof(string));
            table.Rows.Add(DBNull.Value, "Без привязки к заказу");

            const string sql = @"
SELECT co.customer_order_id,
       c.counterparty_name + N' от ' + CONVERT(nvarchar(10), co.order_date, 104) AS display_name
FROM CustomerOrders co
JOIN Counterparties c ON co.customer_id = c.counterparty_id
ORDER BY co.order_date DESC, co.customer_order_id DESC";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connection))
            {
                adapter.Fill(table);
            }

            cmbCustomerOrder.DisplayMember = "display_name";
            cmbCustomerOrder.ValueMember = "customer_order_id";
            cmbCustomerOrder.DataSource = table;

            if (CustomerOrderId != null && CustomerOrderId != DBNull.Value)
                cmbCustomerOrder.SelectedValue = CustomerOrderId;
            else
                cmbCustomerOrder.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            const string sql = "SELECT product_id, product_name FROM Products ORDER BY product_name";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connection))
            {
                DataTable table = new DataTable();
                adapter.Fill(table);
                cmbProduct.DisplayMember = "product_name";
                cmbProduct.ValueMember = "product_id";
                cmbProduct.DataSource = table;
                if (ProductId > 0) cmbProduct.SelectedValue = ProductId;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (cmbProduct.SelectedValue == null)
            {
                MessageBox.Show("Выберите продукцию.");
                return;
            }

            CustomerOrderId = cmbCustomerOrder.SelectedValue == null || cmbCustomerOrder.SelectedValue == DBNull.Value
                ? (object)DBNull.Value
                : cmbCustomerOrder.SelectedValue;
            ProductId = Convert.ToInt32(cmbProduct.SelectedValue);
            Quantity = nudQuantity.Value;
            ProductionDate = dtpProductionDate.Value.Date;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

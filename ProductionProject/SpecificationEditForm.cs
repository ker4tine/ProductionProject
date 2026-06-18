using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public class SpecificationEditForm : Form
    {
        private readonly string connectionString;
        private ComboBox cmbProduct;
        private ComboBox cmbType;
        private ComboBox cmbItem;
        private NumericUpDown nudQuantity;

        public object ProductId { get; private set; }
        public object MaterialId { get; private set; }
        public object OperationId { get; private set; }
        public decimal Quantity { get; private set; }

        public SpecificationEditForm(string connectionString, string title, object productId = null, object materialId = null, object operationId = null, decimal quantity = 1)
        {
            this.connectionString = connectionString;
            ProductId = productId;
            MaterialId = materialId;
            OperationId = operationId;
            Quantity = quantity <= 0 ? 1 : quantity;
            BuildForm(title);
            LoadProducts();
            LoadTypes();
        }

        private void BuildForm(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(460, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label { Text = "Продукция:", Location = new Point(30, 35), AutoSize = true });
            cmbProduct = new ComboBox { Location = new Point(150, 32), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cmbProduct);

            Controls.Add(new Label { Text = "Тип:", Location = new Point(30, 78), AutoSize = true });
            cmbType = new ComboBox { Location = new Point(150, 75), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.SelectedIndexChanged += (s, e) => LoadItems();
            Controls.Add(cmbType);

            Controls.Add(new Label { Text = "Материал/операция:", Location = new Point(30, 121), AutoSize = true });
            cmbItem = new ComboBox { Location = new Point(150, 118), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cmbItem);

            Controls.Add(new Label { Text = "Количество:", Location = new Point(30, 164), AutoSize = true });
            nudQuantity = new NumericUpDown { Location = new Point(150, 161), Width = 250, Minimum = 1, Maximum = 100000, DecimalPlaces = 2, Value = Quantity };
            Controls.Add(nudQuantity);

            Button ok = new Button { Text = "Сохранить", Location = new Point(115, 220), Width = 100 };
            ok.Click += BtnOk_Click;
            Controls.Add(ok);
            Controls.Add(new Button { Text = "Отмена", Location = new Point(230, 220), Width = 100, DialogResult = DialogResult.Cancel });
        }

        private void LoadProducts()
        {
            LoadCombo(cmbProduct,
                "SELECT product_id AS item_id, product_name AS item_name FROM Products ORDER BY product_name",
                ProductId);
        }

        private void LoadTypes()
        {
            cmbType.Items.Add("Материал");
            cmbType.Items.Add("Операция");
            cmbType.SelectedIndex = OperationId != null && OperationId.ToString() != "" ? 1 : 0;
        }

        private void LoadItems()
        {
            if (cmbType.SelectedItem == null) return;

            if (cmbType.SelectedItem.ToString() == "Материал")
                LoadCombo(cmbItem,
                    "SELECT material_id AS item_id, material_name AS item_name FROM Materials ORDER BY material_name",
                    MaterialId);
            else
                LoadCombo(cmbItem,
                    "SELECT operation_id AS item_id, operation_name AS item_name FROM Operations ORDER BY operation_name",
                    OperationId);
        }

        private void LoadCombo(ComboBox combo, string sql, object selectedId)
        {
            combo.Items.Clear();
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ComboItem item = new ComboItem(reader["item_id"], reader["item_name"].ToString());
                        combo.Items.Add(item);
                        if (selectedId != null && item.Id.ToString() == selectedId.ToString())
                            combo.SelectedItem = item;
                    }
                }
            }

            if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            ComboItem product = cmbProduct.SelectedItem as ComboItem;
            ComboItem item = cmbItem.SelectedItem as ComboItem;

            if (product == null || item == null || cmbType.SelectedItem == null)
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }

            ProductId = product.Id;
            Quantity = nudQuantity.Value;

            if (cmbType.SelectedItem.ToString() == "Материал")
            {
                MaterialId = item.Id;
                OperationId = DBNull.Value;
            }
            else
            {
                MaterialId = DBNull.Value;
                OperationId = item.Id;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private class ComboItem
        {
            public object Id { get; private set; }
            private string Name { get; set; }

            public ComboItem(object id, string name)
            {
                Id = id;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}

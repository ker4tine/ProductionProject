using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public class DictionaryEditForm : Form
    {
        private TextBox txtCode;
        private TextBox txtName;
        private TextBox txtUnit;
        private TextBox txtPrice;
        private readonly bool hasUnit;
        private readonly bool hasPrice;

        public string ItemCode { get; private set; }
        public string ItemName { get; private set; }
        public string ItemUnit { get; private set; }
        public decimal ItemPrice { get; private set; }

        public DictionaryEditForm(string title, bool hasUnit, bool hasPrice, string code = "", string name = "", string unit = "", decimal price = 0)
        {
            this.hasUnit = hasUnit;
            this.hasPrice = hasPrice;
            ItemCode = code;
            ItemName = name;
            ItemUnit = unit;
            ItemPrice = price;
            BuildForm(title);
        }

        private void BuildForm(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(380, hasPrice ? 310 : 270);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int y = 25;

            Controls.Add(new Label { Text = "Артикул:", Location = new Point(30, y + 3), AutoSize = true });
            txtCode = new TextBox { Location = new Point(150, y), Width = 170, Text = ItemCode };
            Controls.Add(txtCode);
            y += 38;

            Controls.Add(new Label { Text = "Наименование:", Location = new Point(30, y + 3), AutoSize = true });
            txtName = new TextBox { Location = new Point(150, y), Width = 170, Text = ItemName };
            Controls.Add(txtName);
            y += 38;

            if (hasUnit)
            {
                Controls.Add(new Label { Text = "Ед. измерения:", Location = new Point(30, y + 3), AutoSize = true });
                txtUnit = new TextBox { Location = new Point(150, y), Width = 170, Text = ItemUnit };
                Controls.Add(txtUnit);
                y += 38;
            }

            if (hasPrice)
            {
                Controls.Add(new Label { Text = "Цена:", Location = new Point(30, y + 3), AutoSize = true });
                txtPrice = new TextBox { Location = new Point(150, y), Width = 170, Text = ItemPrice.ToString("0.##") };
                Controls.Add(txtPrice);
                y += 38;
            }

            Button btnOk = new Button { Text = "Сохранить", Location = new Point(80, y + 15), Width = 100 };
            btnOk.Click += BtnOk_Click;

            Button btnCancel = new Button { Text = "Отмена", Location = new Point(195, y + 15), Width = 100, DialogResult = DialogResult.Cancel };

            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите наименование.");
                return;
            }

            decimal price = 0;
            if (hasPrice && !decimal.TryParse(txtPrice.Text.Trim(), out price))
            {
                MessageBox.Show("Цена должна быть числом.");
                return;
            }

            ItemCode = txtCode.Text.Trim();
            ItemName = txtName.Text.Trim();
            ItemUnit = hasUnit ? txtUnit.Text.Trim() : "";
            ItemPrice = price;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

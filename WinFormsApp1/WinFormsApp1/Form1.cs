namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.DataSource = DAL.Listar();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DAL.Inserir();
            dataGridView1.DataSource = DAL.Listar();
        }

        private void dataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex != -1)
            {
                //if (e.ColumnIndex != 1)
                //{
                if (!string.IsNullOrEmpty(dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString()))
                {
                    string codigo = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString() ?? "";
                    string strValue = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString() ?? "";
                    
                    Form2 _Form2 = new Form2(codigo, strValue);
                    _Form2.ShowDialog();
                    dataGridView1.DataSource = DAL.Listar();
                    //dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = _Form2.strvalue;
                    //dataGridView1.Refresh();
                }
                //}
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                dataGridView1.DataSource = DAL.Localizar(textBox1.Text);
            }
            else
            {
                MessageBox.Show("Digite a consulta");
            }
        }
    }
}
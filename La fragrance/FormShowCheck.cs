using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace La_fragrance
{
    public partial class FormShowCheck : Form
    {
       FormMain MyParent;
        public FormShowCheck(FormMain myParent)
        {
            MyParent = myParent;
            InitializeComponent();
        }

        private async void FillCheck()
        {
            double summaryInCheck;
            DateTime dateOfCheck;
            int checkId, sellerId;
            string sellerName;

            using (SqlCommand command = new SqlCommand($"SELECT TOP 1 [checkId], [summaryInCheck], [dateOfCheck], [sellerId] FROM [Check] ORDER BY [checkId] DESC", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    checkId = reader.GetInt32(0);
                    summaryInCheck = reader.GetDouble(1);
                    dateOfCheck = reader.GetDateTime(2);
                    sellerId = reader.GetInt32(3);
                }
            }

            using (SqlCommand command = new SqlCommand($"SELECT [usersName] FROM [Users] WHERE [userId] LIKE '{sellerId}'", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    sellerName = reader.GetString(0);
                }
            }

            using (SqlCommand command1 = new SqlCommand($"SELECT [goodsId], [priceOfSale], [count] FROM [Sales] WHERE [checkId] LIKE '{checkId}'", Program.connection))
            {
                using (SqlDataReader reader1 = await command1.ExecuteReaderAsync())
                {
                    if(reader1.HasRows)
                    {
                        while (await reader1.ReadAsync())
                        {
                            int goodsId = reader1.GetInt32(0);
                            double priceOfSale = reader1.GetDouble(1);
                            int count = reader1.GetInt32(2);
                            string goodsName;
                            int volume;

                            using (SqlCommand command2 = new SqlCommand($"SELECT [goodsName], [volume] FROM [Goods] WHERE [goodsId] LIKE {goodsId}", Program.connection))
                            {
                                using (SqlDataReader reader2 = await command2.ExecuteReaderAsync())
                                {
                                    await reader2.ReadAsync();
                                    goodsName = reader2.GetString(0);
                                    volume = reader2.GetInt32(1);
                                }
                            }

                            dataGridView1.Rows.Add(goodsId, goodsName, volume, count, priceOfSale);
                            label2.Text = Convert.ToString(checkId);
                            label3.Text = "Seller: " + sellerName;
                            label4.Text = Convert.ToString(dateOfCheck.ToShortDateString());
                            label5.Text = "Total: " + Convert.ToString(summaryInCheck) + " UAH";
                        }
                        Program.allowCheck = true;
                    } 
                    else
                    { 
                        this.Dispose();

                        MessageBox.Show("Check is empty");
                    }
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            int checkId;

            using (SqlCommand command = new SqlCommand($"SELECT TOP 1 [checkId], [summaryInCheck], [dateOfCheck], [sellerId] FROM [Check] ORDER BY [checkId] DESC", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    checkId = reader.GetInt32(0);
                }
            }

            DataGridViewCell goodsIdCell = dataGridView1.SelectedCells[0];
            int goodsIdInTable = Convert.ToInt32(goodsIdCell.Value);

            using (SqlCommand command = new SqlCommand($"DELETE FROM [Sales] WHERE [goodsId] LIKE '{goodsIdInTable}' AND [checkId] LIKE '{checkId}'", Program.connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            double summaryInCheck = 0;
            
            using (SqlCommand command = new SqlCommand($"SELECT [priceOfSale] FROM [Sales] WHERE [checkId] LIKE '{checkId}'", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            summaryInCheck += reader.GetDouble(0);
                        }
                    }
                }
            }

            using (SqlCommand command = new SqlCommand($"UPDATE [Check] SET [summaryInCheck] = '{summaryInCheck}' WHERE [checkId] = '{checkId}' ", Program.connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();

            FillCheck();
        }

        private void FormShowCheck_Load(object sender, EventArgs e)
        {
            FillCheck();
        }
    }
}

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
using System.Text.RegularExpressions;
using CrystalDecisions.CrystalReports.Engine;

namespace La_fragrance
{
    public partial class FormMain : Form
    {
        public SqlCommand SqlCommand { get; private set; }

        public FormMain()
        {
            InitializeComponent();

            FillGoods();
            FillUsers();
            this.Focus();
        }

        private async void FillGoods()
        {
            using (SqlCommand command1 = new SqlCommand($"SELECT * FROM [Goods] ORDER BY [categoryId]", Program.connection))
            {
                using (SqlDataReader reader1 = await command1.ExecuteReaderAsync())
                {
                    while (await reader1.ReadAsync())
                    {
                        int goodsId = reader1.GetInt32(0);
                        string goodsName = reader1.GetString(1);
                        int categoryId = reader1.GetInt32(2);
                        int volume = reader1.GetInt32(3);
                        double price = reader1.GetDouble(4);
                        string categoryName = "";

                        using(SqlCommand command2 = new SqlCommand($"SELECT [categoryName] FROM [Categories] WHERE [categoryId] LIKE '{categoryId}'", Program.connection))
                        {
                            using (SqlDataReader reader2 = await command2.ExecuteReaderAsync())
                            {
                                while (await reader2.ReadAsync())
                                {
                                    categoryName = reader2.GetString(0);
                                }
                            }
                        }

                        dataGridViewGoods.Rows.Add(goodsId, goodsName, categoryName, volume, price);

                    }
                }
            }
        }

        private async void FillUsers()
        {
            using (SqlCommand command = new SqlCommand($"SELECT * FROM [Users]", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int userId = reader.GetInt32(0);
                        string userName = reader.GetString(1);
                        string password = reader.GetString(2);
                        string isAdmin = Convert.ToString(reader.GetBoolean(3));

                        dataGridViewUsers.Rows.Add(userId, userName, password, isAdmin);
                    }
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        public bool allowAddToCheck = false;
        public bool allowSelectForActionGoods = false;
        public bool allowSelectForActionUsers = false;

        private void dataGridView1_DoubleClick(object sender, EventArgs e)
        {
            if (allowAddToCheck)
            {
                DataGridViewCell goodsIdCell = dataGridViewGoods.SelectedCells[0];
                int goodsId = Convert.ToInt32(goodsIdCell.Value);
                DataGridViewCell goodsPticeCell = dataGridViewGoods.SelectedCells[4];
                float goodsPrice = Convert.ToSingle(goodsPticeCell.Value);
                int count = Convert.ToInt32(numericUpDown1.Value);


                DialogResult dialogResult = MessageBox.Show("Add item to check?", "La fragrance", MessageBoxButtons.OKCancel);
                if (dialogResult == DialogResult.OK)
                {
                    InsertIntoSales(goodsId, count, goodsPrice);
                    numericUpDown1.Value = 1;
                    buttonShowCheck.Visible = true;
                }
                else if (dialogResult == DialogResult.Cancel)
                {
                    numericUpDown1.Value = 1;
                }
            }
           
        }

        private void buttonAddToCheck_Click(object sender, EventArgs e)
        {
            if (textBoxSelectedItem.Text == String.Empty)
            {
                MessageBox.Show("Select item from table");
            }
            else
            {
                DataGridViewCell goodsIdCell = dataGridViewGoods.SelectedCells[0];
                int goodsId = Convert.ToInt32(goodsIdCell.Value);
                DataGridViewCell goodsPticeCell = dataGridViewGoods.SelectedCells[4];
                float goodsPrice = Convert.ToSingle(goodsPticeCell.Value);
                int count = Convert.ToInt32(numericUpDown1.Value);

                InsertIntoSales(goodsId, count, goodsPrice);
                MessageBox.Show("Added successfully");

                numericUpDown1.Value = 1;
                textBoxSelectedItem.Clear();
                buttonShowCheck.Visible = true;
            }
        }

        private async void buttonCancelCreateCheck_Click(object sender, EventArgs e)
        {
            double summaryInCheck;
            int checkId;
            using (SqlCommand command = new SqlCommand($"SELECT TOP 1 [checkId], [summaryInCheck] FROM [Check] ORDER BY [checkId] DESC", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    checkId = reader.GetInt32(0);
                    summaryInCheck = reader.GetDouble(1);
                }
            }

            if (summaryInCheck == 0)
            {
                groupBoxCreateCheck.Visible = false;
                buttonShowCheck.Visible = false;
                allowAddToCheck = false;
                textBoxSelectedItem.Clear();
                numericUpDown1.Value = 1;

                optimazeDataBase();
            }
            else
            {
                DialogResult dialogResult = MessageBox.Show("Delete curent check?", "La fragrance", MessageBoxButtons.OKCancel);
                if (dialogResult == DialogResult.OK)
                {
                    groupBoxCreateCheck.Visible = false;
                    buttonShowCheck.Visible = false;
                    allowAddToCheck = false;
                    textBoxSelectedItem.Clear();
                    numericUpDown1.Value = 1;

                    using (SqlCommand command = new SqlCommand($"DELETE FROM [Sales] WHERE [checkId] LIKE '{checkId}'", Program.connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    using (SqlCommand command = new SqlCommand($"DELETE FROM [Check] WHERE [checkId] LIKE '{checkId}'", Program.connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                else if (dialogResult == DialogResult.Cancel)
                {
                    textBoxSelectedItem.Clear();
                    numericUpDown1.Value = 1;
                }
            }
        }

        private async void optimazeDataBase()
        {
            using (SqlCommand command = new SqlCommand($"DELETE FROM [Check] WHERE [summaryInCheck] LIKE '0'", Program.connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        private async void InsertIntoCheck(int userId, float summaryInCheck)
        {
            optimazeDataBase();

            using (SqlCommand command = new SqlCommand($"INSERT INTO [Check] ([dateOfCheck], [sellerId], [summaryInCheck]) VALUES (@date, {userId}, {summaryInCheck} )", Program.connection))
            {
                command.Parameters.AddWithValue("@date", DateTime.Now);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async void InsertIntoSales(int goodsId, int count, float goodsPrice)
        {
            int checkId, innerCount;
            double summaryInCheck, innerPriceOfSale;

            using (SqlCommand command = new SqlCommand($"SELECT TOP 1 [checkId], [summaryInCheck] FROM [Check] ORDER BY [checkId] DESC", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    checkId = reader.GetInt32(0);
                    summaryInCheck = reader.GetDouble(1);
                }
            }

            float priceOfSale = count * goodsPrice;
            summaryInCheck += priceOfSale;

            using (SqlCommand command = new SqlCommand($"UPDATE [Check] SET [summaryInCheck] = '{summaryInCheck}' WHERE [checkId] = '{checkId}' ", Program.connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            using (SqlCommand command = new SqlCommand($"SELECT COUNT [*] FROM [Sales] WHERE [goodsId] LIKE '{goodsId}' AND [checkId] LIKE '{checkId}'", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    if (reader.HasRows)
                    {
                        innerCount = reader.GetInt32(0);
                        count += innerCount;

                        using (SqlCommand command1 = new SqlCommand($"SELECT [priceOfSale] FROM [Sales] WHERE [goodsId] LIKE '{goodsId}' AND [checkId] LIKE '{checkId}'", Program.connection))
                        {
                            using (SqlDataReader reader1 = await command1.ExecuteReaderAsync())
                            {
                                await reader1.ReadAsync();
                                innerPriceOfSale = reader1.GetDouble(0);
                            }
                        }

                        using (SqlCommand command2 = new SqlCommand($"UPDATE [Sales] SET [count] = '{count}', [priceOfSale] = '{innerPriceOfSale * count}' WHERE [goodsId] LIKE '{goodsId}' AND [checkId] LIKE '{checkId}'  ", Program.connection))
                        {
                            await command2.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        using (SqlCommand command1 = new SqlCommand($"INSERT INTO [Sales] ([goodsId], [count], [priceOfSale], [checkId], [dateOfSale]) VALUES ({goodsId}, {count}, {priceOfSale}, {checkId}, @date)", Program.connection))
                        {
                            command1.Parameters.AddWithValue("@date", DateTime.Now);
                            await command1.ExecuteNonQueryAsync();
                        }
                    }
                    
                }
            }

        }


        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            int userId = Convert.ToInt32(label4.Text);

            groupBoxCreateCheck.Visible = true;
            groupBoxActionsGoods.Visible = false;

            allowAddToCheck = true;
            allowSelectForActionGoods = false;

            InsertIntoCheck(userId, 0);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            FormShowCheck formShowCheck = new FormShowCheck(this);

            if (formShowCheck.ShowDialog(this) == DialogResult.Cancel )
            {
                if (Program.allowCheck)
                {
                    DialogResult dialogResult = MessageBox.Show("Check is ready?", "La fragrance", MessageBoxButtons.OKCancel);
                    if (dialogResult == DialogResult.OK)
                    {
                        FormCheck formCheck = new FormCheck();
                        formCheck.Show();

                        groupBoxCreateCheck.Visible = false;
                        allowAddToCheck = false;
                        allowSelectForActionGoods = false;
                        Program.allowCheck = false;
                    } 
                }
                else
                {
                    int checkId;
                    using (SqlCommand command = new SqlCommand($"SELECT TOP 1 [checkId] FROM [Check] ORDER BY [checkId] DESC", Program.connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            await reader.ReadAsync();
                            checkId = reader.GetInt32(0);
                        }
                    }

                    using (SqlCommand command = new SqlCommand($"DELETE FROM [Check] WHERE [checkId] LIKE {checkId}", Program.connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    groupBoxCreateCheck.Visible = false;
                }                
            }           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private async void textBox1_TextChanged(object sender, EventArgs e)
        {
            using (SqlCommand command1 = new SqlCommand($"SELECT * FROM [Goods] WHERE [goodsName] LIKE '%{textBoxFindGoods.Text}%' ", Program.connection))
            {
                using (SqlDataReader reader1 = await command1.ExecuteReaderAsync())
                {
                    dataGridViewGoods.Rows.Clear();
                    dataGridViewGoods.Refresh();

                    while (await reader1.ReadAsync())
                    {
                        int goodsId = reader1.GetInt32(0);
                        string goodsName = reader1.GetString(1);
                        int categoryId = reader1.GetInt32(2);
                        int volume = reader1.GetInt32(3);
                        double price = reader1.GetDouble(4);
                        string categoryName = "";

                        using (SqlCommand command2 = new SqlCommand($"SELECT [categoryName] FROM [Categories] WHERE [categoryId] LIKE '{categoryId}'", Program.connection))
                        {
                            using (SqlDataReader reader2 = await command2.ExecuteReaderAsync())
                            {
                                while (await reader2.ReadAsync())
                                {
                                    categoryName = reader2.GetString(0);
                                }
                            }
                        }

                        dataGridViewGoods.Rows.Add(goodsId, goodsName, categoryName, volume, price);

                    }
                }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (allowSelectForActionGoods)
            {
                DataGridViewCell nameCell = dataGridViewGoods.SelectedCells[1];
                textBoxNameItem.Text = Convert.ToString(nameCell.Value);
                DataGridViewCell categoryCell = dataGridViewGoods.SelectedCells[2];
                comboBoxCategoryItem.Text = Convert.ToString(categoryCell.Value);
                DataGridViewCell volumeCell = dataGridViewGoods.SelectedCells[3];
                textBoxVolumeItem.Text = Convert.ToString(volumeCell.Value);
                DataGridViewCell priceCell = dataGridViewGoods.SelectedCells[4];
                textBoxPriceItem.Text = Convert.ToString(priceCell.Value);
            }

            if (allowAddToCheck)
            {
                DataGridViewCell nameCell = dataGridViewGoods.SelectedCells[1];
                textBoxSelectedItem.Text = Convert.ToString(nameCell.Value);
            }
        }

        private async void FillCategories()
        {
            using (SqlCommand command = new SqlCommand($"Select [categoryName] FROM [Categories]", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string categoryName = reader.GetString(0);

                        comboBoxCategoryItem.Items.Add(categoryName);
                    }
                }
            }
        }

        private bool IsValidGoods (string volume, string price)
        {
            if (Regex.IsMatch(volume, @"^\d+$") == false)
            {
                MessageBox.Show("Volume is not a number");
                return false;
            }
            if (Regex.IsMatch(price, @"^\d+$") == false)
            {
                MessageBox.Show("Price is not a number");
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool IsValidUser(string userName, string password)
        {
            if (Regex.IsMatch(userName, @"[0-9a-zA-Z]") == false)
            {
                MessageBox.Show("User Name is invalid (Must be in latin letters)");
                return false;
            }
            if (Regex.IsMatch(password, @"[0-9a-zA-Z]") == false)
            {
                MessageBox.Show("Password is invalid (Must be in latin letters)");
                return false;
            }
            if ((userName.Length < 5) || (userName.Length >= 20)) 
            {
                MessageBox.Show("User Name is invalid (Must be greater than 5 and less than 20)");
                return false;
            }
            if ((password.Length < 5) || (password.Length >= 15))
            {
                MessageBox.Show("Password is invalid (Must be greater than 5 and less than 15)");
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<int> GetCategoryId(string categoryName)
        {
            int categoryId = 0;
            using (SqlCommand command = new SqlCommand($"SELECT [categoryId] FROM [Categories] WHERE [categoryName] LIKE '{categoryName}'", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        categoryId = reader.GetInt32(0);
                    }
                }
            }
            return categoryId;
        }

        private void ClearTextboxGoods()
        {
            textBoxNameItem.Clear();
            textBoxVolumeItem.Clear();
            textBoxPriceItem.Clear();
            comboBoxCategoryItem.SelectedIndex = -1;
        }

        private void ClearTextboxUsers()
        {
            textBoxUserName.Clear();
            textBoxUsersPassword.Clear();
            checkBoxIsAdmin.Checked = false;
        }

        private void addItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            groupBoxCreateCheck.Visible = false;
            groupBoxActionsGoods.Visible = true;
            groupBoxActionsGoods.Text = "Add item";

            buttonAddItem.Visible = true;
            buttonChangeItem.Visible = false;
            buttonDeleteItem.Visible = false;

            ClearTextboxGoods();

            textBoxNameItem.Enabled = true;
            textBoxVolumeItem.Enabled = true;
            textBoxPriceItem.Enabled = true;
            comboBoxCategoryItem.Enabled = true;

            allowAddToCheck = false;
            allowSelectForActionGoods = false;

            comboBoxCategoryItem.Items.Clear();
            FillCategories();
        }

        private async void buttonAddItem_Click(object sender, EventArgs e)
        {
            if ((textBoxNameItem.Text == String.Empty) || (textBoxVolumeItem.Text == String.Empty) || (textBoxPriceItem.Text == String.Empty) || (comboBoxCategoryItem.Text == String.Empty))
            {
                MessageBox.Show("Some fields is empty");
            }
            else
            {
                if (IsValidGoods(textBoxVolumeItem.Text, textBoxPriceItem.Text))
                {
                    string goodsName = textBoxNameItem.Text;
                    int volume = Convert.ToInt32(textBoxVolumeItem.Text);
                    string category = comboBoxCategoryItem.SelectedItem.ToString();
                    double price = Convert.ToDouble(textBoxPriceItem.Text);
                    int categoryId = await GetCategoryId(category);

                    using (SqlCommand command = new SqlCommand($"INSERT INTO [Goods] ([goodsName], [categoryId], [volume], [price]) VALUES ('{goodsName}', {categoryId}, {volume}, {price})", Program.connection))
                    {
                        await command.ExecuteNonQueryAsync();

                    }

                    dataGridViewGoods.Rows.Clear();
                    dataGridViewGoods.Refresh();

                    FillGoods();

                    ClearTextboxGoods();
                }               
            }
        }

        private void changeItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            groupBoxCreateCheck.Visible = false;
            groupBoxActionsGoods.Visible = true;
            groupBoxActionsGoods.Text = "Change item";

            buttonAddItem.Visible = false;
            buttonChangeItem.Visible = true;
            buttonDeleteItem.Visible = false;

            ClearTextboxGoods();

            textBoxNameItem.Enabled = true;
            textBoxVolumeItem.Enabled = true;
            textBoxPriceItem.Enabled = true;
            comboBoxCategoryItem.Enabled = true;

            allowAddToCheck = false;
            allowSelectForActionGoods = true;

            comboBoxCategoryItem.Items.Clear();
            FillCategories();
        }
      
        private async void buttonChangeItem_Click(object sender, EventArgs e)
        {
            if ((textBoxNameItem.Text == String.Empty) || (textBoxVolumeItem.Text == String.Empty) || (textBoxPriceItem.Text == String.Empty) || (comboBoxCategoryItem.Text == String.Empty))
            {
                MessageBox.Show("Some fields is empty");
            }
            else
            {
                if (IsValidGoods(textBoxVolumeItem.Text, textBoxPriceItem.Text))
                {
                    DataGridViewCell idCell = dataGridViewGoods.SelectedCells[0];
                    int goodsId = Convert.ToInt32(idCell.Value);
                    int categoryId = await GetCategoryId(comboBoxCategoryItem.SelectedItem.ToString());

                    using (SqlCommand command = new SqlCommand($"UPDATE [Goods] SET [goodsName] = '{textBoxNameItem.Text}', [categoryId] = '{categoryId}', [volume] = '{textBoxVolumeItem.Text}', [price] = '{textBoxPriceItem.Text}' WHERE [goodsId] LIKE '{goodsId}'", Program.connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    dataGridViewGoods.Rows.Clear();
                    dataGridViewGoods.Refresh();

                    FillGoods();

                    ClearTextboxGoods();
                }
            }   
        }

        private void deleteItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            groupBoxCreateCheck.Visible = false;
            groupBoxActionsGoods.Visible = true;
            groupBoxActionsGoods.Text = "Delete item";

            buttonAddItem.Visible = false;
            buttonChangeItem.Visible = false;
            buttonDeleteItem.Visible = true;

            ClearTextboxGoods();

            textBoxNameItem.Enabled = false;
            textBoxVolumeItem.Enabled = false;
            textBoxPriceItem.Enabled = false;
            comboBoxCategoryItem.Enabled = false;

            allowAddToCheck = false;
            allowSelectForActionGoods = true;

            comboBoxCategoryItem.Items.Clear();
            FillCategories();
        }

        private async void buttonDeleteItem_Click(object sender, EventArgs e)
        {
            DataGridViewCell idCell = dataGridViewGoods.SelectedCells[0];
            int goodsId = Convert.ToInt32(idCell.Value);

            using (SqlCommand command = new SqlCommand($"DELETE FROM [Goods] WHERE [goodsId] LIKE '{goodsId}'", Program.connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            dataGridViewGoods.Rows.Clear();
            dataGridViewGoods.Refresh();

            FillGoods();

            ClearTextboxGoods();
        }

        private void buttonCancelActionsGoods_Click(object sender, EventArgs e)
        {
            ClearTextboxGoods();
            groupBoxActionsGoods.Visible = false;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private async void textBoxFindUsers_TextChanged(object sender, EventArgs e)
        {
            using (SqlCommand command = new SqlCommand($"SELECT * FROM [Users] WHERE [usersName] LIKE '%{textBoxFindUsers.Text}%'", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    dataGridViewUsers.Rows.Clear();
                    dataGridViewUsers.Refresh();

                    while (await reader.ReadAsync())
                    {
                        int userId = reader.GetInt32(0);
                        string userName = reader.GetString(1);
                        string password = reader.GetString(2);
                        string isAdmin = Convert.ToString(reader.GetBoolean(3));

                        dataGridViewUsers.Rows.Add(userId, userName, password, isAdmin);
                    }
                }
            }
        }

        private void dataGridViewUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (allowSelectForActionUsers)
            {
                DataGridViewCell usersNameCell = dataGridViewUsers.SelectedCells[1];
                textBoxUserName.Text = Convert.ToString(usersNameCell.Value);
                DataGridViewCell passwordCell = dataGridViewUsers.SelectedCells[2];
                textBoxUsersPassword.Text = Convert.ToString(passwordCell.Value);
                DataGridViewCell isAdminCell = dataGridViewUsers.SelectedCells[3];
                checkBoxIsAdmin.Checked = Convert.ToBoolean(isAdminCell.Value);
            }
        }

        private void addUserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            groupBoxActionsUser.Visible = true;
            groupBoxActionsUser.Text = "Add user";

            buttonAddUser.Visible = true;
            buttonChangeUserData.Visible = false;
            buttonDeleteUser.Visible = false;

            textBoxUserName.Enabled = true;
            textBoxUsersPassword.Enabled = true;
            checkBoxIsAdmin.Enabled = true;

            ClearTextboxUsers();

            allowSelectForActionUsers = false;
        }

        private async void buttonAddUser_Click(object sender, EventArgs e)
        {
            if ((textBoxUserName.Text == String.Empty) || (textBoxUsersPassword.Text == String.Empty))
            {
                MessageBox.Show("Some fileds is empty");
            }
            else
            {
                if (IsValidUser(textBoxUserName.Text, textBoxUsersPassword.Text))
                {
                    string usersName = textBoxUserName.Text;
                    string usersPassword = textBoxUsersPassword.Text;
                    bool isAdmin = checkBoxIsAdmin.Checked;

                    using (SqlCommand command = new SqlCommand($"INSERT INTO [Users] ([usersName], [password], [isAdmin]) VALUES ('{usersName}', '{usersPassword}', '{isAdmin}')", Program.connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    dataGridViewUsers.Rows.Clear();
                    dataGridViewUsers.Refresh();

                    FillUsers();

                    ClearTextboxUsers();
                }  
            }
        }

        private void changeUserDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            groupBoxActionsUser.Visible = true;
            groupBoxActionsUser.Text = "Change user data";

            buttonAddUser.Visible = false;
            buttonChangeUserData.Visible = true;
            buttonDeleteUser.Visible = false;

            textBoxUserName.Enabled = true;
            textBoxUsersPassword.Enabled = true;
            checkBoxIsAdmin.Enabled = true;

            ClearTextboxUsers();

            allowSelectForActionUsers = true;
        }

        private async void buttonChangeUserData_Click(object sender, EventArgs e)
        {
            if ((textBoxUserName.Text == String.Empty) || (textBoxUsersPassword.Text == String.Empty))
            {
                MessageBox.Show("Some fields is empty");
            }
            else
            {
                if (IsValidUser(textBoxUserName.Text, textBoxUsersPassword.Text))
                {
                    DataGridViewCell userIdCell = dataGridViewUsers.SelectedCells[0];
                    int userId = Convert.ToInt32(userIdCell.Value);

                    string usersName = textBoxUserName.Text;
                    string usersPassword = textBoxUsersPassword.Text;
                    bool isAdmin = checkBoxIsAdmin.Checked;

                    bool statusOfCurrentUser = false;

                    using (SqlCommand command = new SqlCommand($"SELECT [isAdmin] FROM [Users] WHERE [userId] LIKE '{label10.Text}'", Program.connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                statusOfCurrentUser = reader.GetBoolean(0);
                            }
                        }
                    }

                    if ((statusOfCurrentUser == true) && (userId == Convert.ToInt32(label10.Text)))
                    {
                        MessageBox.Show("No permission to change role");

                        using (SqlCommand command = new SqlCommand($"UPDATE [Users] SET [usersName] = '{usersName}', [password] = '{usersPassword}' WHERE [userId] LIKE '{userId}'", Program.connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }

                        dataGridViewUsers.Rows.Clear();
                        dataGridViewUsers.Refresh();

                        FillUsers();

                        ClearTextboxUsers();
                    }
                    else
                    {
                        using (SqlCommand command = new SqlCommand($"UPDATE [Users] SET [usersName] = '{usersName}', [password] = '{usersPassword}', [isAdmin] = '{isAdmin}' WHERE [userId] LIKE '{userId}'", Program.connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }

                        dataGridViewUsers.Rows.Clear();
                        dataGridViewUsers.Refresh();

                        FillUsers();

                        ClearTextboxUsers();
                    }
                }
            }
        }

        private void deleteUserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            groupBoxActionsUser.Visible = true;
            groupBoxActionsUser.Text = "Delete user";

            buttonAddUser.Visible = false;
            buttonChangeUserData.Visible = false;
            buttonDeleteUser.Visible = true;

            textBoxUserName.Enabled = false;
            textBoxUsersPassword.Enabled = false;
            checkBoxIsAdmin.Enabled = false;

            ClearTextboxUsers();

            allowSelectForActionUsers = true;
        }

        private async void buttonDeleteUser_Click(object sender, EventArgs e)
        {
            DataGridViewCell userIdCell = dataGridViewUsers.SelectedCells[0];
            int userId = Convert.ToInt32(userIdCell.Value);

            bool statusOfCurrentUser = false;

            using (SqlCommand command = new SqlCommand($"SELECT [isAdmin] FROM [Users] WHERE [userId] LIKE '{label10.Text}'", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        statusOfCurrentUser = reader.GetBoolean(0);
                    }
                }
            }

            if ((statusOfCurrentUser == true) && (userId == Convert.ToInt32(label10.Text)))
            {
                MessageBox.Show("No premisson to delete user");
            }
            else
            {
                using (SqlCommand command = new SqlCommand($"DELETE FROM [Users] WHERE [userId] LIKE '{userId}'", Program.connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                dataGridViewUsers.Rows.Clear();
                dataGridViewUsers.Refresh();

                FillUsers();

                ClearTextboxUsers();
            }
        }

        private void buttonCancelActionsUser_Click(object sender, EventArgs e)
        {
            ClearTextboxUsers();
            groupBoxActionsUser.Visible = false;
        }

        private void tabPage2_Leave(object sender, EventArgs e)
        {
            allowSelectForActionUsers = false;
            groupBoxActionsUser.Visible = false;
        }
    }
}

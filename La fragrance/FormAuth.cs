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
    public partial class FormAuth : Form
    {
        public FormAuth()
        {
            InitializeComponent();

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if ((textBox1.Text != String.Empty) && (textBox2.Text != String.Empty))
            {
                string passwordFromDB = await GetUserPassword(textBox1.Text);
                if (textBox2.Text == passwordFromDB)
                {
                    FormMain formMain = new FormMain();
                    int userId = await GetUserId(textBox1.Text);
                    bool userRole = await GetUserRole(textBox1.Text);
                    if (userRole == false)
                    {
                        formMain.tabControl1.Controls.Remove(formMain.tabPage2);

                    }
                    formMain.label2.Text = formMain.label12.Text = textBox1.Text;
                    formMain.label4.Text = formMain.label10.Text = Convert.ToString(userId);
                    formMain.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Authorisation Error");
                }
            }
            else
            {
                MessageBox.Show("Fields is empty");
            }
            
            
        }

        private async Task<string> GetUserPassword(string login)
        {
            using (SqlCommand command = new SqlCommand($"SELECT [password] FROM [USERS] WHERE [usersName] LIKE '{login}'", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    if (reader.HasRows)
                    {
                        string password = reader.GetString(0);
                        return password;
                    } 
                    else
                    {
                        return "";
                    }
                    
                }
            }
        }

        private async Task<bool> GetUserRole(string login)
        {
            using (SqlCommand command = new SqlCommand($"SELECT [isAdmin] FROM [USERS] WHERE [usersName] LIKE '{login}'", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    if (reader.HasRows)
                    {
                        bool isAdmin = reader.GetBoolean(0);
                        return isAdmin;
                    }
                    else
                    {
                        return false;
                    }

                }
            }
        }

        private async Task<int> GetUserId(string login)
        {
            using (SqlCommand command = new SqlCommand($"SELECT [userId] FROM [USERS] WHERE [usersName] LIKE '{login}'", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    if (reader.HasRows)
                    {
                        int userIdFromDB = reader.GetInt32(0);
                        return userIdFromDB;
                    } 
                    else
                    {
                        return 0;
                    }

                }
            }
        }
    }
}

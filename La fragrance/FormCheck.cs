using CrystalDecisions.CrystalReports.Engine;
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
    public partial class FormCheck : Form
    {
        public FormCheck()
        {
            InitializeComponent();

            FillCheck();
        }

        private async void FillCheck()
        {
            int checkId = 0;

            using (SqlCommand command = new SqlCommand($"SELECT TOP 1 [checkId] FROM [Check] ORDER BY [checkId] DESC", Program.connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    checkId = reader.GetInt32(0);
                }
            }

            SqlDataAdapter dataAdapter = new SqlDataAdapter($"SELECT [Sales].[checkId], [Goods].[goodsName], [Goods].[volume], [Sales].[count], [Sales].[priceOfSale], [Check].[dateOfCheck], [Check].[summaryInCheck] FROM[Sales], [Goods], [Check] WHERE([Sales].[goodsId] = [Goods].[goodsId]) AND([Sales].[checkId] = [Check].[checkId]) AND([Check].[checkId] LIKE '{checkId}')", Program.connection);
            DataSet1 dataSet = new DataSet1();
            dataAdapter.Fill(dataSet, "DataTable1");

            if (dataSet.DataTable1.Rows.Count != 0)
            {
                int indexOfLastRow = dataSet.DataTable1.Rows.Count - 1;

                dataSet.DataTable1.Rows[indexOfLastRow]["summaryInCheck"] += " UAH";

                DateTime dateOfCheck = Convert.ToDateTime(dataSet.DataTable1.Rows[indexOfLastRow]["dateOfCheck"]);
                dataSet.DataTable1.Rows[indexOfLastRow]["dateOfCheck"] = dateOfCheck.ToShortDateString();

            }

            ReportDocument reportDocument = new ReportDocument();
            reportDocument.Load("CrystalReport1.rpt");
            reportDocument.SetDataSource(dataSet);
            crystalReportViewer1.ReportSource = reportDocument;
        }
    }
}

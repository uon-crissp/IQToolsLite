using System;
using System.Collections.Generic;
using System.Text;
using ExampleBase;
using NetOffice;
using Excel = NetOffice.ExcelApi;
using NetOffice.ExcelApi.Enums;
using System.IO;
using System.Globalization;
using System.Windows.Forms;

namespace IQToolsLite
{
    class clsExcel : IExample
    {
        IHost _hostApplication;
        private static System.Data.DataTable dt = new System.Data.DataTable();
        private static string fileName = "";

        public clsExcel(System.Data.DataTable sdt, string sfileName)
        {
            dt = sdt; fileName = sfileName;
        }

        public void RunExample()
        {
            try { File.Delete(fileName); }
            catch (Exception ex) { }

            try
            {
                
                Excel.Application excelApplication = new Excel.Application();
                excelApplication.DisplayAlerts = false;

                // add a new workbook
                Excel.Workbook workBook = excelApplication.Workbooks.Add();
                Excel.Worksheet workSheet = (Excel.Worksheet)workBook.Worksheets[1];

                // we need some data to display
                Excel.Range dataRange = PutSampleData(workSheet);

                // create a nice diagram
                Excel.ChartObject chart = ((Excel.ChartObjects)workSheet.ChartObjects()).Add(70, 100, 375, 225);
                chart.Chart.SetSourceData(dataRange);

                // save the book 
                workBook.SaveAs(fileName);

                // close excel and dispose reference
                excelApplication.Quit();
                excelApplication.Dispose();

                // show dialog for the user(you!)
                //_hostApplication.ShowFinishDialog(null, fileName);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                //TODO Log Error Helper
                MessageBox.Show("The Excel file could not be created. Please check that you have Microsoft Excel 2003 or Higher Installed","IQTools",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            }
        }

        public string CreateExcel()
        {
            string returnString = "";
          

            try
            {

                Excel.Application excelApplication = new Excel.Application();
                excelApplication.DisplayAlerts = false;
                //Add Extension Here
                fileName += GetDefaultExtension(excelApplication);

                try { File.Delete(fileName); }
                catch (Exception ex) { returnString = ex.Message.ToString(); }
               
                Excel.Workbook workBook = excelApplication.Workbooks.Add();
                Excel.Worksheet workSheet = (Excel.Worksheet)workBook.Worksheets[1];

               
                Excel.Range dataRange = PutSampleData(workSheet);

                // save the book 
                workBook.SaveAs(fileName);

                // close excel and dispose reference
                excelApplication.Quit();
                excelApplication.Dispose();

                // show dialog for the user(you!)
                //_hostApplication.ShowFinishDialog(null, fileName);
                return "success";
            }
            catch (Exception ex)
            {
                returnString += ex.Message.ToString();
                return returnString;
               
            }
        }


        private static Excel.Range PutSampleData(Excel.Worksheet workSheet)
        {
            for (int j = 0; j < dt.Columns.Count; j++)
            {
                workSheet.Cells[1, j + 1].Value = dt.Columns[j].ColumnName;
            }

            //Populate rest of the data
            int iRow = 2;
            for (int rowNo = 0; rowNo < dt.Rows.Count; rowNo++)
            {
                for (int colNo = 0; colNo < dt.Columns.Count; colNo++)
                {
                    workSheet.Cells[iRow, colNo + 1].Value = dt.Rows[rowNo][colNo].ToString();
                }
                iRow++;
            }
            return workSheet.Range("$B2:$E6");
        }

        private static string GetDefaultExtension(Excel.Application application)
        {
            double Version = Convert.ToDouble(application.Version, CultureInfo.InvariantCulture);
            if (Version >= 12.00)
                return ".xlsx";
            else
                return ".xls";
        }

        public void Connect(IHost hostApplication)
        {
            _hostApplication = hostApplication;
        }

        public string Caption
        {
            get { return _hostApplication.LCID == 1033 ? "Example01" : "Beispiel01"; }
        }

        public string Description
        {
            get { return _hostApplication.LCID == 1033 ? "Background Colors and Borders for Cells" : "Hintergrundfarben und Rahmen in Zellen"; }
        }

        public UserControl Panel
        {
            get { return null; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ExampleBase;

namespace IQToolsLite
{
    class ExcelWriterCls
    {
        IHost _hostApplication;
        private Stream stream;
        private BinaryWriter writer;
        private DataGridView dataGridView1;

        private ushort[] clBegin = { 0x0809, 8, 0, 0x10, 0, 0 };
        private ushort[] clEnd = { 0x0A, 00 };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelWriter"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public ExcelWriterCls(DataGridView dg, string fileName)
        {
            try
            {
                this.stream = new FileStream(fileName + ".xls", FileMode.OpenOrCreate);
            }
            catch (Exception)
            {
                try
                {
                    this.stream = new FileStream(fileName + ".xlsx", FileMode.OpenOrCreate);
                }
                catch (Exception) { }
            }

            if (this.stream != null)
            {
                writer = new BinaryWriter(stream);
                dataGridView1 = dg;
            }
        }

        private void WriteUshortArray(ushort[] value)
        {
            for (int i = 0; i < value.Length; i++)
                writer.Write(value[i]);
        }

        /// <summary>
        /// Writes the text cell value.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="col">The col.</param>
        /// <param name="value">The string value.</param>
        public void WriteCell(int row, int col, string value)
        {
            ushort[] clData = { 0x0204, 0, 0, 0, 0, 0 };
            int iLen = value.Length;
            byte[] plainText = Encoding.ASCII.GetBytes(value);
            clData[1] = (ushort)(8 + iLen);
            clData[2] = (ushort)row;
            clData[3] = (ushort)col;
            clData[5] = (ushort)iLen;
            WriteUshortArray(clData);
            writer.Write(plainText);
        }

        /// <summary>
        /// Writes the integer cell value.
        /// </summary>
        /// <param name="row">The row number.</param>
        /// <param name="col">The column number.</param>
        /// <param name="value">The value.</param>
        public void WriteCell(int row, int col, int value)
        {
            ushort[] clData = { 0x027E, 10, 0, 0, 0 };
            clData[2] = (ushort)row;
            clData[3] = (ushort)col;
            WriteUshortArray(clData);
            int iValue = (value << 2) | 2;
            writer.Write(iValue);
        }

        /// <summary>
        /// Writes the double cell value.
        /// </summary>
        /// <param name="row">The row number.</param>
        /// <param name="col">The column number.</param>
        /// <param name="value">The value.</param>
        public void WriteCell(int row, int col, double value)
        {
            ushort[] clData = { 0x0203, 14, 0, 0, 0 };
            clData[2] = (ushort)row;
            clData[3] = (ushort)col;
            WriteUshortArray(clData);
            writer.Write(value);
        }

        /// <summary>
        /// Writes the empty cell.
        /// </summary>
        /// <param name="row">The row number.</param>
        /// <param name="col">The column number.</param>
        public void WriteCell(int row, int col)
        {
            ushort[] clData = { 0x0201, 6, 0, 0, 0x17 };
            clData[2] = (ushort)row;
            clData[3] = (ushort)col;
            WriteUshortArray(clData);
        }

        /// <summary>
        /// Must be called once for creating XLS file header
        /// </summary>
        public string BeginWrite()
        {
            if (this.stream != null)
            {
                WriteUshortArray(clBegin);

                try
                {
                    int cols = dataGridView1.Columns.Count;
                    // put headers
                    for (int i = 0; i < cols; i++)
                        WriteCell(0, i, dataGridView1.Columns[i].HeaderText);

                    // put data
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                        for (int j = 0; j < cols; j++)
                            if (dataGridView1.Rows[i].Cells[j].Value != null)
                                WriteCell(i + 1, j, dataGridView1.Rows[i].Cells[j].Value.ToString());

                    EndWrite();
                    stream.Close();

                    return "success";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            return "Un-expected problem occured!!";
        }

        /// <summary>
        /// Ends the writing operation, but do not close the stream
        /// </summary>
        public void EndWrite()
        {
            WriteUshortArray(clEnd);
            writer.Flush();
        }


        /*private static string GetDefaultExtension(Excel.Application application)
        {
            double Version = Convert.ToDouble(application.Version, CultureInfo.InvariantCulture);
            if (Version >= 12.00)
                return ".xlsx";
            else
                return ".xls";
        }*/

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

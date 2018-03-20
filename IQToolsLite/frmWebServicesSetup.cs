using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace IQToolsLite
{
    public partial class frmWebServicesSetup : Form
    {
        public frmWebServicesSetup()
        {
            InitializeComponent();
            WebservicesGridView();
        }

        private List<WebServiceItem> items = new List<WebServiceItem>();
        private void WebservicesGridView()
        {
            if (!File.Exists(webServiceList))
            {
                File.Create(webServiceList);
                System.Threading.Thread.Sleep(500);
            }

            try
            {
                using (StreamReader reader = new StreamReader(webServiceList))
                {
                    string line = reader.ReadLine();
                    while (!string.IsNullOrEmpty(line))
                    {
                        string[] entries = line.Split('*');

                        items.Add(new WebServiceItem() { Caption = entries[0], Url = entries[1] });
                        line = reader.ReadLine();
                    }
                }
            }
            catch (Exception ex) { }

            dataGridView2.DataSource = items;
            dataGridView2.Refresh();
        }

        private void SetForm(string caption, string url)
        {
            txtWebServiceCaption.BeginInvoke(new MethodInvoker(delegate()
            {
                txtWebServiceCaption.Text = caption;
            }));
            txtWebServiceUrl.BeginInvoke(new MethodInvoker(delegate()
            {
                txtWebServiceUrl.Text = url;
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if ((!string.IsNullOrEmpty(txtWebServiceCaption.Text) || (!string.IsNullOrEmpty(txtWebServiceUrl.Text))))
            {
                SetMsg("", Color.Black);
                SaveWebService(txtWebServiceCaption.Text, txtWebServiceUrl.Text);
            }
            else
            {
                SetMsg("Please make sure you've entered both a caption and a Url", Color.Red);
            }

            indexOfItem = 0;
            SetForm("", "");
        }

        private void SetMsg(string msg, Color color)
        {
            lblMsg.BeginInvoke(new MethodInvoker(delegate()
            {
                lblMsg.Text = msg;
                lblMsg.ForeColor = color;
            }));
        }

        private void SaveWebService(string caption, string url)
        {
            if (indexOfItem > 0)
            {
                items[indexOfItem] = new WebServiceItem() { Caption = caption, Url = url };
            }else
                items.Add(new WebServiceItem() { Caption = caption, Url = url });

            List<WebServiceItem> newItems = new List<WebServiceItem>();
            foreach (WebServiceItem item in items)
                newItems.Add(new WebServiceItem() { Caption = item.Caption, Url = item.Url });
            
            dataGridView2.DataSource = newItems;
            dataGridView2.EndEdit();
            dataGridView2.Refresh();

            SaveToFile();
        }

        List<string> logFiles = new List<string>();
        private string webServiceList = AppDomain.CurrentDomain.BaseDirectory + "WebserviceList.txt";
        private void SaveToFile()
        {
            if (!File.Exists(webServiceList))
            {
                File.Create(webServiceList);
                System.Threading.Thread.Sleep(500);
            }

            try
            {
                using (StreamWriter writer = File.CreateText(webServiceList))
                {
                    foreach (WebServiceItem item in items)
                        if (!string.IsNullOrEmpty(item.Caption))
                            writer.WriteLine(item.Caption +"*"+item.Url);
                    writer.Flush();
                    writer.Dispose();
                }
            }
            catch (Exception ex) { }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            indexOfItem = 0;
            SetForm("", "");
        }

        #region service list
        private int WebServID = 0;


        private int indexOfItem = 0;
        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            DataGridView dg = (DataGridView)sender;
            if ((dg.SelectedRows != null) && (dg.SelectedRows.Count > 0))
            {
                string a = dg.SelectedRows[0].Cells[0].Value.ToString();
                string b = dg.SelectedRows[0].Cells[1].Value.ToString();

                indexOfItem = items.FindIndex(x => (x.Caption == a) && (x.Url == b));
                SetForm(a, b);
            }
        }
        #endregion
    }

    public class WebServiceItem
    {
        public string Caption { get; set; }
        public string Url { get; set; }
    }
}

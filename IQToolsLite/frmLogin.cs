using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using IQToolsLite.IQTWebService;
using System.Configuration;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IQToolsLite
{
    public partial class frmLogin : Form
    {
        Thread thd;
        /*
            http://iqtools.dyndns.org/IQToolsWebService/Service1.asmx
            http://iqtools.dyndns.org/IQToolsWebServiceTest/Service1.asmx
            http://iqtoolske.dnsdynamic.com:81/IQToolsWebService/Service1.asmx
            http://localhost:55827/Service1.asmx
            http://localhost:52375/Service1.asmx
         */
        
        private string webServiceList = AppDomain.CurrentDomain.BaseDirectory + "WebserviceList.txt";

        private string fl = AppDomain.CurrentDomain.BaseDirectory + "CurrentService.txt";

        public frmLogin()
        {
            InitializeComponent();
            this.FormClosing += frmLogin_FormClosing;
        }

        void frmLogin_FormClosing(object sender, FormClosingEventArgs e)
        {   
            SetPreviousWebService(cboServer.Text);
        }

        private void lblQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void SetWebservices()
        {
            #region web services lists
            if (!File.Exists(webServiceList))
            {
                //Set default webservices
                using (StreamWriter writer = File.CreateText(webServiceList))
                {
                    writer.WriteLine("IQTools Server:*http://localhost/IQToolsWebService/Service1.asmx");                    
                    /*writer.WriteLine("IQTools Server:KE*http://iqtoolske.dnsdynamic.com:81/IQToolsWebService/Service1.asmx");
                    writer.WriteLine("IQTools Server:TZ*http://41.73.195.220/iqtoolslite/Service1.asmx");
                    writer.WriteLine("IQTools SSL Server:KE*https://iqtoolske.dnsdynamic.com/IQToolsWebService/Service1.asmx");
                    writer.WriteLine("IQTools SSL Server:CRS*https://iqtools.dyndns.org/IQToolsWebServiceSSL/Service1.asmx");
                    writer.WriteLine("IQTools Server:TEST*http://iqtools.dyndns.org/IQToolsWebServiceTest/Service1.asmx");*/
                    writer.Flush();
                    writer.Dispose();
                }

                System.Threading.Thread.Sleep(500);
            }

            try
            {
                //cboServer.Items.Clear();
                List<WebServiceItem> webServiceItems = new List<WebServiceItem>();
                cboServer.BeginInvoke(new MethodInvoker(delegate(){                    
                    webServiceItems.Add(new WebServiceItem() { Caption = "", Url = "" });
                    using (StreamReader reader = new StreamReader(webServiceList))
                    {
                        string line = reader.ReadLine();
                        while (!string.IsNullOrEmpty(line))
                        {
                            string[] entries = line.Split('*');
                            //
                            webServiceItems.Add(new WebServiceItem() { Caption = entries[0], Url = entries[1] });
                            line = reader.ReadLine();
                        }
                    }

                    cboServer.DisplayMember = "Caption";
                    cboServer.ValueMember = "Url";
                    cboServer.DataSource = webServiceItems;
                    cboServer.Refresh();
                    cboServer.SelectedIndex = 1;
                }));
            }
            catch (Exception ex) { }
            #endregion

            #region default web service
            string webServerUrl = cboServer.Text;
            if (!File.Exists(fl))
                File.Create(fl);
            string prevWebServ = GetPreviousWebService();

            if (!string.IsNullOrEmpty(prevWebServ))
            {
                cboServer.BeginInvoke(new MethodInvoker(delegate()
                {
                    cboServer.Text = prevWebServ;
                }));
            }
            //else
            //    SetPreviousWebService(webServerUrl);
            #endregion
        }

        private void frmLogin_Shown(object sender, EventArgs e)
        {
            SetWebservices();

            string webServerUrl = cboServer.Text;
            //Thread thd = new Thread(() => CheckServer(webServerUrl));
            if (thd != null)
            {
                if (thd.IsAlive == true) thd.Abort();
            }
            thd = new Thread(() => CheckServer(webServerUrl));
            thd.SetApartmentState(ApartmentState.STA);
            thd.Start();
        }
        
        private string GetPreviousWebService()
        {
            try
            {
                using (StreamReader reader = new StreamReader(fl))
                {
                    try
                    {
                        string str = reader.ReadLine();
                        reader.Close();
                        return str;
                    }
                    catch (Exception ex) { }
                }
            }
            catch (Exception e) { }
            return string.Empty;
        }

        private void SetPreviousWebService(string name)
        {
            using (StreamWriter stream = new StreamWriter(fl))
            {
                stream.Write(name);
                stream.Close();
            }
        }

        private void CheckServer(string url)
        {
            //ws.Url = "http://41.206.32.54/IQToolsWebService/Service1.asmx";
            //SetControlPropertyThreadSafe(lblStatus, "Text", "Checking Connection To IQTools Server...");
            //SetControlPropertyThreadSafe(btnLogin, "Enabled", false);

            if (!string.IsNullOrEmpty(url))
            {
                SetControlPropertyThreadSafe(lblStatus, "Text", "Checking Connection To IQTools Server...");
                SetControlPropertyThreadSafe(btnLogin, "Enabled", false);
                try
                {
                    // Call the certificate policy class that grant connection to the SSL site 
                    System.Net.ServicePointManager.CertificatePolicy = new CertificatePolicyCls();

                    Service1 ws = new Service1();
                    ws.Url = url;
                    SetControlPropertyThreadSafe(picServer, "Image", Properties.Resources.progress4);
                    try
                    {
                        if (ws.CheckAvailability() == "OK")
                        {
                            SetControlPropertyThreadSafe(lblStatus, "Text", "Server Is Available");
                            SetControlPropertyThreadSafe(btnLogin, "Enabled", true);
                            SetControlPropertyThreadSafe(picServer, "Image", null);
                            clsGbl.wsURL = ws.Url;
                            
                        }
                        else
                        {
                            SetControlPropertyThreadSafe(lblStatus, "Text", "Server Is Unavailable");
                            SetControlPropertyThreadSafe(btnLogin, "Enabled", false);
                            SetControlPropertyThreadSafe(picServer, "Image", null);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogErrorHelper(ws, "<<frmLogin.cs: CheckServer>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);

                        if (ex.Message.ToLower() == "unable to connect to the remote server")
                        {
                            SetControlPropertyThreadSafe(lblStatus, "Text", "Server Is Unavailable");
                            SetControlPropertyThreadSafe(btnLogin, "Enabled", false);
                            SetControlPropertyThreadSafe(picServer, "Image", null);
                        }
                        else if (ex.Message.ToLower() == "thread was being aborted.")
                        {
                            return;
                        }
                        else
                        {
                            MessageBox.Show(ex.Message, "Connection Error");
                            SetControlPropertyThreadSafe(lblStatus, "Text", "Server is Unavailable");
                            SetControlPropertyThreadSafe(btnLogin, "Enabled", false);
                            SetControlPropertyThreadSafe(picServer, "Image", null);
                        }
                    }
                    finally
                    {
                        //SetControlPropertyThreadSafe(picServer, "Image", null);
                    }
                }
                catch (Exception ex)
                {
                    SetControlPropertyThreadSafe(lblStatus, "Text", "Server is Unavailable");
                    SetControlPropertyThreadSafe(btnLogin, "Enabled", false);
                    SetControlPropertyThreadSafe(picServer, "Image", null);
                }
            }
        }

        private void LogErrorHelper(Service1 ws, string errorMsg, string application, int UserID)
        {
            if (ws == null)
                ws = new Service1();

            ws.ErrorLogging(errorMsg, application, UserID);
            ws.Dispose();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
           
            
            if (txtUserName.Text != "" && txtPassword.Text != "")
            {
                //Log In Using Web Service
                Service1 ws = new Service1();
                //MessageBox.Show(ws.Url);
                ws.Url = clsGbl.wsURL;

                try
                {
                    WebClient wc = new WebClient();
                    string ipadress = wc.DownloadString("http://checkip.dyndns.org");
                    ipadress = (new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")).Match(ipadress).Value;

                    var loc = ws.GetUserLocation(ipadress);

                    clsGbl.countryName = loc.CountryName;
                    clsGbl.countryCode = loc.CountryCode;
                    clsGbl.regionName = loc.RegionName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                           
              
                string[] response = ws.GetCredentials(txtUserName.Text.Trim(), txtPassword.Text.Trim());
                if (response[0] == "Success")
                {
                    //Set FullName and UserName to Global Variables
                    clsGbl.userID = Convert.ToInt32(response[2]);
                    clsGbl.userName = txtUserName.Text;
                    clsGbl.userFirstName = response[4];                    
                    clsGbl.userLastName = response[5];
                    clsGbl.Email = response[3];
                    Form frm = new frmMain(txtPassword.Text.Trim());
                    frm.Show();                    
                    this.Hide();
                }
                else if (response[0] == "NoUser")
                {
                    MessageBox.Show("The Specified User Does Not Exist Or has Been Disabled", "IQTools");
                }
                else if (response[0] == "WrongPassword")
                {
                    MessageBox.Show("The Password Entered Is Incorrect, Please Try Again", "IQTools");
                }
                else MessageBox.Show(response[0]);
            }
            else
                MessageBox.Show("Please Enter a User Name and Password to Log In", "IQTools");
        }

        private void UpdateLabel(string lblValue)
        {
            if (InvokeRequired)
            { this.Invoke(new MethodInvoker(delegate { lblStatus.Text = lblValue; })); }
        }

        private void UpdateButton(bool state)
        {
            if (InvokeRequired)
            { this.Invoke(new MethodInvoker(delegate { btnLogin.Enabled = state; })); }
        }

        private void cboServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string WebServerUrl = cboServer.SelectedValue.ToString();
                //Thread thd = new Thread(() => CheckServer(WebServerUrl));
                if (thd != null)
                {
                    if (thd.IsAlive == true) thd.Abort();
                }
                thd = new Thread(() => CheckServer(WebServerUrl));
                thd.SetApartmentState(ApartmentState.STA);
                thd.Start();
            }
            catch (Exception ex) {
                LogErrorHelper(null, "<<frmLogin: cboServer_SelectedIndexChanged>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                MessageBox.Show(ex.Message); 
            }

        }

        private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);

        public static void SetControlPropertyThreadSafe(Control control, string propertyName, object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate(SetControlPropertyThreadSafe), new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control, new object[] { propertyValue });
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string WebServerUrl = cboServer.Text;
            Thread thd = new Thread(() => CheckServer(WebServerUrl));
            thd.SetApartmentState(ApartmentState.STA);
            thd.Start();
        }

        private void SetServerPath(string url)
        {
            string appPath = Application.ExecutablePath;
            appPath += ".config";
            XmlDocument doc = new XmlDocument();
            doc.Load(appPath);
            XmlNode Node = doc.DocumentElement.ChildNodes.Item(1);
            Node = Node.ChildNodes.Item(0);
            Node = Node.ChildNodes.Item(0);
            Node = Node.ChildNodes.Item(0);
            Node.InnerText = url;
            doc.Save(appPath);
        }

        private void CreateAppSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string sectionName = "applicationSettings";
            int i = ConfigurationManager.AppSettings.Count;
            string newKey = "NewKey" + i.ToString();
            string newValue = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
            config.AppSettings.Settings.Add(newKey, newValue);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(sectionName);
            AppSettingsSection appSettingSection = (AppSettingsSection)config.GetSection(sectionName);
            MessageBox.Show(appSettingSection.SectionInformation.GetRawXml());
        }

        private void cboServer_TextChanged(object sender, EventArgs e)
        {
            btnLogin.Enabled = false;
            lblStatus.Text = "Refresh Connection";
        }

        private void cboServer_ControlAdded(object sender, ControlEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;

            MessageBox.Show(combo.Items.Count.ToString());
        }

        void webServ_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetWebservices();
        }

        private void lblSettings_Click(object sender, EventArgs e)
        {
            frmWebServicesSetup webServ = new frmWebServicesSetup();
            webServ.FormClosing += webServ_FormClosing;
            webServ.Show();
        }

        private void lnkForgottenPass_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string username = txtUserName.Text.Trim();
            if (!string.IsNullOrEmpty(username))
            {
                DialogResult dialogResult = MessageBox.Show("The Password will be sent to your email address", "Password Recovery", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    Service1 ws = new Service1();
                    ws.Url = clsGbl.wsURL;
                    string msg = ws.RetrievePassword(txtUserName.Text.Trim());
                    if (!string.IsNullOrEmpty(msg))
                        MessageBox.Show(msg);
                    else
                        MessageBox.Show("Your password has been sent via your email address!");
                }
                else if (dialogResult == DialogResult.No)
                {
                }
            }
            else
            {
                MessageBox.Show("Please enter your username name first!");
            }
        }

      

        public class GeoLocation
        {
          public string IPAddress { get; set; }
          public string CountryName { get; set; }
          public string CountryCode { get; set; }
          public string CityName { get; set; }
          public string RegionName { get; set; }
          public string ZipCode { get; set; }
        }

    }
}

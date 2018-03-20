using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using IQToolsLite.IQTWebService;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using ActiveDatabaseSoftware.ActiveQueryBuilder;
using System.Net;


namespace IQToolsLite
{
    public partial class frmMain : Form
    {
        string limitSQL = "";
        string selectedDB = "";
        string selectedEMR = "";
        public string Pass;

      // If the list is big we can opt for a table with a list of countries and respective EMR's
        private string[] EMR_COUNTRIES = 
        {
            "Kenya", "Tanzania", "Uganda", "Nigeria","Rwanda","Haiti","Guyana","Ethiopia"
        };

        private string[] ALL_EMR = 
        {
           "IQCare", "ISante", "IQChart","CTC2","ICAP"
        };
        
        private string[] KENYA_EMR =
        {
         "IQcare", "IQChart"
        };

        private string[] TANZANIA_EMR =
         {
         "CTC2"
         };

         private string[] HAITI_EMR =
         {
         "ISante"
         };

         private string[] NIGERIA_EMR =
         {
         "IQCare"
         };

        public frmMain(string pass)
        {            
            Pass = pass;
            InitializeComponent();
            GetAccountInfo();

            if (Pass == "CrsUser2013")
            {
                TestTab.SelectedIndex = 2;
                LblMsg.Text = "Please change password!";
            }
            //GetUserLocation ( );
            LimitDataGeographically ( );
           
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.Text += " | " + clsGbl.userFirstName;
            string country = cboCountries.SelectedItem.ToString();
            Thread fetchDB = new Thread(() => FetchDBs("IQCare",country));
            fetchDB.SetApartmentState(ApartmentState.STA);
            fetchDB.Start();

            Thread fetchQueries = new Thread(() => FetchQueries("IQCare"));
            fetchQueries.SetApartmentState(ApartmentState.STA);
            fetchQueries.Start();

            currentCountry = cboCountries.SelectedItem.ToString();
        }
               
        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void sqlTextEditor1_Leave(object sender, EventArgs e)
        {
            try
            {   
                queryBuilder1.SQL = sqlTextEditor1.Text;
            }
            catch (Exception ex)
            {
                LogErrorHelper(null, "<<frmMain.cs: frmMain.cs: sqlTextEditor1_Leave(object sender, EventArgs e)>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                MessageBox.Show(ex.Message, "Parsing error");
            }
        }

        public void LogErrorHelper(Service1 ws, string errorMsg, string application, int UserID)
        {
            if (ws == null)
                ws = new Service1();

            ws.ErrorLogging(errorMsg, application, UserID);
            ws.Dispose();
        }

        private void plainTextSQLBuilder1_SQLUpdated(object sender, EventArgs e)
        {
            // Text of SQL query has been updated by the query builder.
            sqlTextEditor1.Text = plainTextSQLBuilder1.SQL;

        }

        private SqlConnection connection = new SqlConnection();

        private void QueryBuilder_SetupProperties()
        {
            // Assign Expression Editor
            queryBuilder1.ExpressionEditor = expressionEditor1;
            sqlTextEditor1.QueryBuilder = queryBuilder1;


            /****************************************************/            
            //connection.ConnectionString = ConfigurationSettings.AppSettings["ConnString"].ToString();
            //connection.ConnectionString = "Data Source=SACCIE;Initial Catalog=IQTools_IQCare;User ID=sa;Password=love1111";
            //connection.Open();

            MSSQLMetadataProvider metadataProvider = new MSSQLMetadataProvider();
            metadataProvider.Connection = connection;

            UniversalSyntaxProvider syntaxProvider = new UniversalSyntaxProvider();

            queryBuilder1.MetadataProvider = metadataProvider;
            //queryBuilder1.SyntaxProvider = syntaxProvider;
            /****************************************************/
        }

        private void QueryBuilder_SetupMetadata(string emr)
        {
            queryBuilder1.MetadataContainer.LoadFromXMLFile("Resources\\IQTools_"+emr+".xml");            
        }

        private void QueryBuilder_Init(object sender, EventArgs e)
        {
            this.QueryBuilder_SetupProperties();
            //this.QueryBuilder_SetupMetadata(cboEMR.SelectedItem.ToString());
            this.QueryBuilder_SetupMetadata(cboEMR.Text);
            
        }

        private void LimitDataGeographically ( )
        {
          string countryCode = clsGbl.countryCode;
          cboEMR.Items.Clear ( );
          switch (countryCode.ToLower())
          { 
              case "tz":
              cboCountries.Items.Add ( "Tanzania" );
              foreach (string emr in TANZANIA_EMR) cboEMR.Items.Add (emr);
              break;

              case "ke":
              cboCountries.Items.Add ("Kenya");
              foreach (string emr in KENYA_EMR) cboEMR.Items.Add ( emr );
              break;

              case "ug":
              cboCountries.Items.Add ( "Uganda" );
              foreach (string emr in KENYA_EMR) cboEMR.Items.Add ( emr );
              break;

              case "ng":
              cboCountries.Items.Add ( "Nigeria" );
              foreach (string emr in NIGERIA_EMR) cboEMR.Items.Add ( emr );
              break;

            default:
              foreach (string country in EMR_COUNTRIES) cboCountries.Items.Add ( country );
              foreach (string emr in ALL_EMR) cboEMR.Items.Add (emr);
              break;
          }
          if (cboEMR.Items.Count > 0) cboEMR.SelectedIndex = 0;
          if (cboCountries.Items.Count > 0) cboCountries.SelectedIndex = 0;
        
        }

        private void tcQueries_Selected(object sender, TabControlEventArgs e)
        {
            queryBuilder1.Focus();
            Application.DoEvents();

            //string[] parameters = new string[3];
            if (e.TabPage == tpPreview)
            {
                getParams(dgvPreview, 1, "mssql");
                if (Continue)
                {
                    if (sqlTextEditor1.Text != "")
                    {
                        dgvPreview.DataSource = null; dgvPreview.Refresh();
                        selectedEMR = cboEMR.Text.ToLower();
                        //selectedDB = cboDBs.SelectedItem.ToString().ToLower();
                        selectedDB = cboDBs.SelectedItem.ToString();
                        if (selectedEMR == "iqcare" || selectedEMR == "ctc2" || selectedEMR == "iqchart" || selectedEMR == "icap")
                        {
                            //limitSQL = "SELECT TOP " + txtLimit.Text.Trim() + " * FROM (" + sqlTextEditor1.Text.Trim() + ")limit";
                            if (chkLimit.Checked)
                            {
                                if (sqlTextEditor1.Text.Trim().ToLower().Contains("distinct"))
                                {
                                    var regex = new Regex("select distinct");
                                    limitSQL = regex.Replace(sqlTextEditor1.Text.Trim().ToLower(), "select distinct top " + txtLimit.Text.Trim(), 1);
                                }
                                else
                                {
                                    var regex = new Regex("select");
                                    limitSQL = regex.Replace(sqlTextEditor1.Text.Trim().ToLower(), "select top " + txtLimit.Text.Trim(), 1);
                                }
                            }
                            else limitSQL = sqlTextEditor1.Text.Trim();
                        }
                        else if (selectedEMR == "isante")
                        {
                            if (chkLimit.Checked)
                            {
                                limitSQL = "SELECT * FROM (" + sqlTextEditor1.Text.Trim() + ")a LIMIT " + txtLimit.Text.Trim();
                            }
                            else limitSQL = sqlTextEditor1.Text.Trim();
                        }

                        Thread previewThread = new Thread(() => PreviewData(limitSQL, selectedEMR, selectedDB));
                        previewThread.SetApartmentState(ApartmentState.STA);
                        previewThread.Start();
                    }
                    else
                    {
                        MessageBox.Show("Please Select A Query To Preview", "IQTools", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        tcQueries.SelectedTab = tpDesign;
                        //tcQueries.SelectedTab = tpManage;
                    }
                }
                else
                {
                    tcQueries.SelectedTab = tpDesign;
                }
            }
            else if (e.TabPage == tpDesign)
            {
                try
                {
                    if (cboDBs.SelectedIndex < 1)
                        cboDBs.SelectedIndex = 0;
                }
                catch (Exception ex) { }
            }

            else if (e.TabPage == tpMerge)
            {
                if (sqlTextEditor1.Text != "")
                {
                    //txtMergeQuery.Text = queryBuilder1.SQL;
                    txtMergeQuery.Text = sqlTextEditor1.Text;
                    txtMergeQuery.Enabled = false;
                }
            }
        }

        private void getParams(DataGridView dgvPreview, int Rc, string provider)
        {
            switch (provider.Trim().ToLower())
            {
                case "mssql":
                    {
                        msSQLParams(dgvPreview, Rc);
                        break;
                    }
                case "mysql":
                    {
                        mySQLParams(dgvPreview, Rc);
                        break;
                    }
                default:
                    { msSQLParams(dgvPreview, Rc); break; }
                    
            }
        }
               
        private void mySQLParams(DataGridView dgv, int Rc)
        {
            throw new NotImplementedException();
        }

        private string CheckParameters(string sql)
        {
            if (sql.Replace(" ","").IndexOf("=@") >= 0)
            {
                //frmQryParameters paramsFrm = new frmQryParameters();
                //paramsFrm.ShowDialog();

                SqlCommand cmd = new SqlCommand();

                //if (queryBuilder1.Parameters.Count > 0)
                //{
                    Hashtable myParameters = new Hashtable(); int j = 0; myParameters.Clear();
                    for (int i = 0; i < queryBuilder1.Parameters.Count; i++)
                    {
                        j = 0;
                        SqlParameter p = new SqlParameter();
                        p.ParameterName = queryBuilder1.Parameters[i].FullName;
                        p.DbType = queryBuilder1.Parameters[i].DataType;
                        foreach (DictionaryEntry de in myParameters)
                        {
                            if (de.Key.ToString().Trim().ToLower() == queryBuilder1.Parameters[i].FullName.Trim().ToLower())
                            {
                                j = 1;
                                break;
                            }
                        }
                        if (j == 0)
                        {
                            cmd.Parameters.Add(p);
                            myParameters.Add(p.ParameterName, p.DbType);
                        }
                    }

                    using (frmQryParameters qpf = new frmQryParameters(queryBuilder1.Parameters, cmd))
                    {
                        qpf.ShowDialog();
                    }
                    //}

            }
            return sql;
        }

        private void PreviewData(string sql, string EMR, string DB)
        {
            //try catch finally
            clsGbl.SetControlPropertyThreadSafe(cboDBs, "Enabled", false);
            clsGbl.SetControlPropertyThreadSafe(picProgress1, "Image", Properties.Resources.progress2);
            clsGbl.SetControlPropertyThreadSafe(lblProgress1, "Text", "Fetching Data, Please wait");
            Service1 ws = new Service1();
            ws.Url = clsGbl.wsURL;
            DataTable dt = new DataTable();
            
            List<string> parameters = new List<string>();
            foreach (System.Data.SqlClient.SqlParameter par in cmd.Parameters)
            {
                parameters.Add(par.ParameterName);
                parameters.Add(par.SqlDbType.ToString());
                parameters.Add(par.Value.ToString());
            }                

            try 
            {
                if (EMR == "iqcare" || EMR == "iqchart" || EMR == "ctc2" || EMR == "icap")
                    dt = ws.GetDataDC(parameters.ToArray(), sql, DB, "mssql");
                    //dt = ws.GetDataDB(sql, DB, "mssql");
                else if (EMR == "isante")
                    dt = ws.GetDataDB(sql, DB, "mysql");
                //else if (EMR == "ctc2")
                //    dt = ws.GetDataDB(sql, DB, "mssql");
            }
            catch (Exception ex) {
                LogErrorHelper(ws, "<<frmMain.cs: PreviewData, GetDataDB(sql, DB, 'mysql')>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                MessageBox.Show(ex.Message, "Error"); 
            }

            UpdateDataGrid(dgvPreview, dt);
            clsGbl.SetControlPropertyThreadSafe(lblRowsReturned, "Text", dt.Rows.Count.ToString() + " Rows");
            clsGbl.SetControlPropertyThreadSafe(picProgress1, "Image", null);
            clsGbl.SetControlPropertyThreadSafe(lblProgress1, "Text", "");
            clsGbl.SetControlPropertyThreadSafe(cboDBs, "Enabled", true);
        }

        private void UpdateDataGrid(DataGridView dgv, DataTable dt)
        {
            if (InvokeRequired)
            { 
                this.Invoke(new MethodInvoker(delegate 
                {
                    //dgvPreview.DataSource = null; dgvPreview.Refresh();
                    //dgvPreview.DataSource = dt; dgvPreview.Refresh();
                    dgv.DataSource = null; dgv.Refresh();
                    dgv.DataSource = dt; dgv.Refresh();                    
                    
                }
                )); 
            }
        }

        /*private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);

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
        }*/

        private void FetchDBs(string emr, string country)
        {
            Service1 ws = new Service1();
            ws.Url = clsGbl.wsURL;
            DataTable dt = new DataTable();
            string dbCountry = "";
            //Country Codes
            if (country.ToLower() == "kenya")
                dbCountry = "KE";
            else if (country.ToLower() == "nigeria")
                dbCountry = "NG";
            else if (country.ToLower() == "uganda")
                dbCountry = "UG";
            else if (country.ToLower() == "haiti")
                dbCountry = "HT";
            else if (country.ToLower() == "tanzania")
                dbCountry = "TZ";
            else if (country.ToLower() == "guyana")
                dbCountry = "GY";
            else if (country.ToLower() == "ethiopia")
                dbCountry = "ET";
            else if (country.ToLower() == "zambia")
                dbCountry = "ZM";
            else if (country.ToLower() == "rwanda")
                dbCountry = "RW";

            try
            {
                dt = ws.GetDBs(dbCountry, emr);
                DataTableReader dr = dt.CreateDataReader();
                while (dr.Read())
                {
                    UpdateComboBox(dr[0].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                LogErrorHelper(null,ex.Message, "Fetch DBs", clsGbl.userID);
            }

            try
            {
                clsGbl.SetControlPropertyThreadSafe(cboDBs, "SelectedIndex", 0);
            }
            catch (Exception ex) 
            {
                LogErrorHelper(null, "<<frmMain.cs: FetchDBs, SetControlPropertyThreadSafe(cboDBs, 'SelectedIndex', 0)>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);

                if (ex.Message.Substring(0,28) == "InvalidArgument=Value of '0'")// is not valid for 'SelectedIndex'.Parameter name: SelectedIndex")
                {
                    MessageBox.Show("No DBs found on this Server for Selected Country", "IQTools");
                }
                else
                    MessageBox.Show(ex.Message, "IQTools");
            }
        }

        private void UpdateComboBox(string Item)
        {
            //throw new NotImplementedException();if (InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    //dgvPreview.DataSource = null; dgvPreview.Refresh();
                    //dgvPreview.DataSource = dt; dgvPreview.Refresh();
                    clbMergeDbs.Items.Add(Item);
                    cboDBs.Items.Add(Item);
                    
                }
                ));
            }

        }

        private void FetchQueries(string emr)
        {
            clsGbl.SetControlPropertyThreadSafe(picProgress1, "Image", Properties.Resources.progress2);
            clsGbl.SetControlPropertyThreadSafe(lblProgress1, "Text", "Fetching Data, Please wait");

            if (emr == "")
                emr = "IQCare";
            Service1 ws = new Service1();
            ws.Url = clsGbl.wsURL;
            DataTable dt = new DataTable();
         
            try
            {
                dt = ws.GetQueries(emr, clsGbl.userID);
                UpdateDataGrid(dgvQueries, dt);
            }
            catch (Exception ex) {
                LogErrorHelper(ws, "<<frmMain.cs: FetchQueries, GetDataDB>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                MessageBox.Show(ex.Message, "Error"); 
            }                        

            clsGbl.SetControlPropertyThreadSafe(picProgress1, "Image", null);
            clsGbl.SetControlPropertyThreadSafe(lblProgress1, "Text", "");
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (txtName.Text != "")
            {
                tcQueries.SelectedTab = tpDesign;
                //sqlTextEditor1.Text = dgvQueries.CurrentRow.Cells[4].Value.ToString();
                queryBuilder1.SQL = dgvQueries.CurrentRow.Cells[4].Value.ToString();

            }
            else
                MessageBox.Show("Please Select a Query To Open", "IQTools");
        }

        private void dgvQueries_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            txtName.Text = dgvQueries.CurrentRow.Cells[0].Value.ToString();
            txtDescription.Text = dgvQueries.CurrentRow.Cells[1].Value.ToString();
            if (dgvQueries.CurrentRow.Cells["OWNER"].Value.ToString() == clsGbl.userName)
            {
                btnEditQuery.Enabled = true;
            }
            else btnEditQuery.Enabled = false;

            //Set sqleditor to selected query to allow immediate preview
            if (dgvQueries.CurrentRow.Cells[4].Value.ToString() != "")
            {
                //sqlTextEditor1.Text = dgvQueries.CurrentRow.Cells["SQLSYNTAX"].Value.ToString();
                queryBuilder1.SQL = dgvQueries.CurrentRow.Cells[4].Value.ToString();
            }
        }

        private void chkSelect_CheckedChanged(object sender, EventArgs e)
        {
            int i = 0;
            for (i = 0; i < clbMergeDbs.Items.Count; i++)
            {
                clbMergeDbs.SetItemChecked(i, chkSelect.Checked);
            }
            if (chkSelect.Text == "Select All")
                chkSelect.Text = "Unselect All";
            else
                chkSelect.Text = "Select All";
        }

        private void btnMerge_Click(object sender, EventArgs e)
        {
            if (txtMergeQuery.Text != "" && clbMergeDbs.CheckedItems.Count > 0)
            {                
                //TODO DONE Get UID from Log In
                int i;
                //string emr = cboEMR.SelectedItem.ToString().ToLower();
                string emr = cboEMR.Text.ToLower();
                string[] selectedDBs = new string[clbMergeDbs.CheckedItems.Count];
                for (i = 0; i < clbMergeDbs.CheckedItems.Count; i++)
                {
                    selectedDBs[i] = clbMergeDbs.CheckedItems[i].ToString();
                }
                Service1 wss = new Service1();
                wss.Url = clsGbl.wsURL;
                string sendMerge = "";
                try
                {
                    //sendMerge = wss.Merge(emr, txtMergeQuery.Text.Trim(), "IQTools_"+cboEMR.SelectedItem.ToString(), selectedDBs, clsGbl.userID);
                    sendMerge = wss.Merge(emr, txtMergeQuery.Text.Trim(), "IQTools" , selectedDBs, clsGbl.userID);
                    //sendMerge = wss.Merge(emr, txtMergeQuery.Text.Trim(), "IQTools_CTC2", selectedDBs, clsGbl.userID);
                }
                catch (Exception ex) {
                    LogErrorHelper(null, "<<frmMain.cs: btnMerge_Click, wss.Merge>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                    MessageBox.Show(ex.Message, "Error"); 
                }
                if (sendMerge == "OK")
                {
                    MessageBox.Show("The Merge Request Has Been Received By The Server. Please Check Your Email In A Few Minutes", "IQTools",MessageBoxButtons.OK,MessageBoxIcon.Information); //TODO Better Message??
                }
            }
            else
            {
                MessageBox.Show("No Query Or Database Selected", "Error");
            }
        }

        private void cboDBs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tcQueries.SelectedTab == tpPreview && sqlTextEditor1.Text != "")
            {

                dgvPreview.DataSource = null; dgvPreview.Refresh();
                //TODO Handle Data Grid Errors
                //TODO Validate limitSQL to integers only                
                //string limitsql = "SELECT TOP " + txtLimit.Text.Trim() + " * FROM (" + sqlTextEditor1.Text.Trim() + ")limit";
                //string DB = cboDBs.SelectedItem.ToString();
                //Thread previewThread = new Thread(() => PreviewData(limitsql, DB));
                //previewThread.SetApartmentState(ApartmentState.STA);
                //previewThread.Start();

                //selectedDB = cboDBs.SelectedItem.ToString().ToLower();
                selectedDB = cboDBs.SelectedItem.ToString();
                if (selectedEMR == "iqcare" || selectedEMR == "ctc2" || selectedEMR == "iqchart" || selectedEMR == "icap")
                {
                    //limitSQL = "SELECT TOP " + txtLimit.Text.Trim() + " * FROM (" + sqlTextEditor1.Text.Trim() + ")limit";
                    //string DB = cboDBs.SelectedItem.ToString()a;
                    if (chkLimit.Checked)
                    {
                        var regex = new Regex("select");
                        limitSQL = regex.Replace(sqlTextEditor1.Text.Trim().ToLower(), "select top " + txtLimit.Text.Trim(), 1);
                    }
                    else limitSQL = sqlTextEditor1.Text.Trim();
                }
                else if (selectedEMR == "isante")
                {
                    if (chkLimit.Checked)
                    {
                        limitSQL = "SELECT * FROM (" + sqlTextEditor1.Text.Trim() + ")a LIMIT " + txtLimit.Text.Trim();
                    }
                    else limitSQL = sqlTextEditor1.Text.Trim();
                }

                Thread previewThread = new Thread(() => PreviewData(limitSQL, selectedEMR, selectedDB));
                previewThread.SetApartmentState(ApartmentState.STA);
                previewThread.Start();
                
            }
        }

        private void cboEMR_SelectedIndexChanged(object sender, EventArgs e) 
        {
            /*********** Clear design editor *********/
            //queryBuilder1.Clear();
            //sqlTextEditor1.Text = "";
            /*****************************************/
           string country = string.Empty;
            if (cboEMR.SelectedIndex != -1)
            {
                cboDBs.Items.Clear();
                clbMergeDbs.Items.Clear();
               if (cboCountries.Items.Count > 0)
               country = cboCountries.SelectedText.ToString();
                //if (cboEMR.SelectedItem.ToString() == "ISante")
                if (cboEMR.Text == "ISante")
                {
                    //queryBuilder1.SyntaxProvider = mySQLSyntaxProvider1;
                    //this.QueryBuilder_SetupMetadata("IQTools_ISante.xml");

                    Thread fetchDB = new Thread(() => FetchDBs("ISante", country));
                    fetchDB.SetApartmentState(ApartmentState.STA);
                    fetchDB.Start();

                    Thread fetchQueries = new Thread(() => FetchQueries("ISante"));
                    fetchQueries.SetApartmentState(ApartmentState.STA);
                    fetchQueries.Start();

                    queryBuilder1.SyntaxProvider = mySQLSyntaxProvider1;
                    //queryBuilder1.MetadataContainer.LoadFromXMLFile("Resources\\IQToolsXML.xml");
                    //queryBuilder1.RefreshMetadata();
                    this.QueryBuilder_SetupMetadata("ISante");
                }
                //else if (cboEMR.SelectedItem.ToString() == "IQCare")
                else if (cboEMR.Text == "IQCare")
                {
                    queryBuilder1.SyntaxProvider = mssqlSyntaxProvider1;
                    this.QueryBuilder_SetupMetadata("IQCare");
                    Thread fetchDB = new Thread(() => FetchDBs("IQCare", country));
                    fetchDB.SetApartmentState(ApartmentState.STA);
                    fetchDB.Start();

                    //Thread fetchQueries = new Thread(() => FetchQueries("IQCare"));
                    //fetchQueries.SetApartmentState(ApartmentState.STA);
                    //fetchQueries.Start();
                }
                //else if (cboEMR.SelectedItem.ToString() == "CTC")
                else if (cboEMR.Text == "CTC2")
                {
                    queryBuilder1.SyntaxProvider = mssqlSyntaxProvider1;
                    this.QueryBuilder_SetupMetadata("CTC2");
                    Thread fetchDB = new Thread(() => FetchDBs("CTC2", country));
                    fetchDB.SetApartmentState(ApartmentState.STA);
                    fetchDB.Start();

                    //Thread fetchQueries = new Thread(() => FetchQueries("CTC2"));
                    ////Thread fetchQueries = new Thread(() => FetchQueries("CTC"));
                    //fetchQueries.SetApartmentState(ApartmentState.STA);
                    //fetchQueries.Start();
                    //this.QueryBuilder_SetupMetadata("CTC2");
                }

                //else if (cboEMR.SelectedItem.ToString() == "IQChart")
                else if (cboEMR.Text == "IQChart")
                {
                    //queryBuilder1.SyntaxProvider = mssqlSyntaxProvider1;
                    //this.QueryBuilder_SetupMetadata("CTC2");
                    Thread fetchDB = new Thread(() => FetchDBs("IQChart", country));
                    fetchDB.SetApartmentState(ApartmentState.STA);
                    fetchDB.Start();
                    this.QueryBuilder_SetupMetadata("IQChart");

                    Thread fetchQueries = new Thread(() => FetchQueries("IQChart"));
                    fetchQueries.SetApartmentState(ApartmentState.STA);
                    fetchQueries.Start();
                }
                else if (cboEMR.Text == "ICAP")
                {
                    //queryBuilder1.SyntaxProvider = mssqlSyntaxProvider1;
                    //this.QueryBuilder_SetupMetadata("CTC2");
                    Thread fetchDB = new Thread(() => FetchDBs("ICAP", country));
                    fetchDB.SetApartmentState(ApartmentState.STA);
                    fetchDB.Start();
                    this.QueryBuilder_SetupMetadata("ICAP");

                    Thread fetchQueries = new Thread(() => FetchQueries("ICAP"));
                    fetchQueries.SetApartmentState(ApartmentState.STA);
                    fetchQueries.Start();
                }
            }
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            if (txtName.Text != "")
            {
                //tcQueries.SelectedTab = tpPreview;
                //sqlTextEditor1.Text = dgvQueries.CurrentRow.Cells[4].Value.ToString();
                queryBuilder1.SQL = dgvQueries.CurrentRow.Cells[4].Value.ToString();
                tcQueries.SelectedTab = tpPreview;

            }
            else
                MessageBox.Show("Please Select a Query To Preview", "IQTools");
        }

        //private string countrySwitchMessage = "The country you have selected has a different EMR and will open a blank designer. Are you sure to continue?";

        private string currentCountry = string.Empty;

        private void cboCountries_SelectedIndexChanged(object sender, EventArgs e) //SelectionChangedEventArgs , EventArgs
        {
            string country = cboCountries.SelectedItem.ToString();
            if (country.ToLower() == "kenya" || country.ToLower() == "nigeria" || country.ToLower() == "uganda")
            {
                SwitchCountryHelper("IQCare");
            }
            else if (country.ToLower() == "haiti")
            {
                //cboEMR.SelectedItem = "ISante";
                SwitchCountryHelper("ISante");
            }
            else if (country.ToLower() == "tanzania")
            {
                //cboEMR.SelectedItem = "CTC";
                SwitchCountryHelper("CTC2");
            }
            else if (country.ToLower() == "guyana" || country.ToLower() == "rwanda")
            {
                //cboEMR.SelectedIndex = -1;
                //cboEMR.SelectedIndex = cboEMR.FindString("IQChart");
                SwitchCountryHelper("IQChart");
            }
            else if (country.ToLower() == "ethiopia")
            {
                //cboEMR.SelectedItem = "ICAP";
                SwitchCountryHelper("ICAP");
            }
            //else if (country.ToLower() == "zambia")
            //    dbCountry = "ZM";
            //this.cboEMR_SelectedIndexChanged(sender, e);

            currentCountry = cboCountries.SelectedItem.ToString();
        }

        private void SwitchCountryHelper(string emr)
        {
            if ((cboEMR.Items != null) && (cboEMR.Items.Count > 0) && (cboEMR.SelectedItem != null) && (cboEMR.SelectedItem.ToString() != emr))
            {
                //if ((MessageBox.Show(countrySwitchMessage, "IQTools", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes))
                //{
                    //queryBuilder1.Clear();
                    //sqlTextEditor1.Text = "";
                    //tcQueries.SelectedTab = tpDesign;

                    if (emr == "IQCare")
                    {
                        cboEMR.SelectedIndex = -1;
                        cboEMR.SelectedIndex = cboEMR.FindString("IQCare");
                    }
                    else if (emr == "IQChart")
                    {
                        cboEMR.SelectedIndex = -1;
                        cboEMR.SelectedIndex = cboEMR.FindString("IQChart");
                    }
                    else
                        cboEMR.SelectedItem = emr;
                //}
                //else
                //{
                //    this.BeginInvoke((MethodInvoker)delegate { cboCountries.SelectedItem = currentCountry; });
                //}
            }
            else
            {
                cboEMR.SelectedItem = emr;
            }
        }

        private void btnCollapse_Click(object sender, EventArgs e)
        {
           
            if (btnCollapse.Text == ">>")
            {
                splitContainer7.SplitterDistance = splitContainer7.Width - 42;
                btnCollapse.Text = "<<";
            }
            else if (btnCollapse.Text == "<<")
            {
                splitContainer7.SplitterDistance = splitContainer7.Width - 332;
                btnCollapse.Text = ">>";
            }
        }

        private void btnSaveQuery_Click(object sender, EventArgs e)
        {
            Service1 ws = new Service1();
            ws.Url = clsGbl.wsURL;
            //MessageBox.Show(ws.SaveQuery(clsGbl.userID.ToString(), cboEMR.SelectedItem.ToString(), queryBuilder1.SQL, txtQName.Text,txtQDesc.Text));
            string sql = queryBuilder1.SQL.Replace("'", "''");
            string response = "";
            if (txtQName.Text != "" && txtQDesc.Text != "")
            {
                try
                {
                    if (btnSaveQuery.Text == "Save")
                    {
                        response = ws.SaveQuery(clsGbl.userID.ToString(), cboEMR.Text, sql, txtQName.Text, txtQDesc.Text);
                    }
                    else if (btnSaveQuery.Text == "Modify")
                    {
                        response = ws.UpdateQuery(clsGbl.userID.ToString(), cboEMR.Text, sql, txtQName.Text, txtQDesc.Text);
                    }
                }
                catch (Exception ex) {
                    LogErrorHelper(ws, "<<frmMain.cs: btnSaveQuery_Click>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                    MessageBox.Show(ex.Message); 
                }
                if (response == "Success")
                    MessageBox.Show("Query Successfully Saved", "IQTools");
                else MessageBox.Show("Error Occured During the Save Operation", "IQTools");
            }
            else
            {
                MessageBox.Show("Please Enter a Query Name and Description And Try Again", "IQTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            //FetchQueries(cboEMR.SelectedItem.ToString());
            dgvQueries.DataSource = null;
            dgvQueries.Refresh();
            string emr = cboEMR.Text;
            Thread fetchQueries = new Thread(() => FetchQueries(emr));
            fetchQueries.SetApartmentState(ApartmentState.STA);
            fetchQueries.Start();

        }

        private void btnNewQuery_Click(object sender, EventArgs e)
        {
            tcQueries.SelectedTab = tpDesign;
            sqlTextEditor1.Text = "";
            queryBuilder1.SQL = "";
        }

        private void btnExcel_Click(object sender, EventArgs e)
        {
            string fileName = Application.StartupPath +"\\iqtoolsLite"+DateTime.Now.Hour.ToString()+DateTime.Now.Minute.ToString()+DateTime.Now.Second.ToString(); //Add extension later
            if (dgvPreview.DataSource != null)
            {
                Thread exportExcel = new Thread(() => ExportToExcel(fileName));
                                
                exportExcel.SetApartmentState(ApartmentState.STA);
                exportExcel.Start();
            }
        }

        private void ExportToExcel(string fileName)
        {            
            clsGbl.SetControlPropertyThreadSafe(picProgress1, "Image", Properties.Resources.progress2);
            clsGbl.SetControlPropertyThreadSafe(lblProgress1, "Text", "Exporting Data, Please wait");
                       
            ExcelWriterCls exl = new ExcelWriterCls(dgvPreview, fileName);
            if (exl.BeginWrite() == "success")
            {
                try
                {
                    System.Diagnostics.Process.Start(fileName + ".xlsx");
                }
                catch(Exception ex)//Log ex to database
                {
                    try { System.Diagnostics.Process.Start(fileName + ".xls"); }
                    catch (Exception ex1) { MessageBox.Show("File could not be opened", "IQTools"); } //Log ex1 to database
                }
            }
            else
            {
                MessageBox.Show("The Excel file could not be created. Please check that you have Microsoft Excel 2003 or Higher Installed", "IQTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            clsGbl.SetControlPropertyThreadSafe(picProgress1, "Image", null);
            clsGbl.SetControlPropertyThreadSafe(lblProgress1, "Text", "");

            /*
            try { File.Delete(fileName); }
            catch (Exception ex) {
                LogErrorHelper(null, "<<frmMain.cs: ExportToExcel,  File.Delete>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                MessageBox.Show(ex.Message); 
            }

            Excel.Application eApp = new Excel.Application();
            Excel.Range eRange;
            Excel.Workbook eBook = eApp.Workbooks.Add(Type.Missing);
            Excel.Worksheet eSheet = (Excel.Worksheet)eBook.ActiveSheet;
            eSheet.Name = "IQToolsLite";
            int success = 0;

            try
            {
                SetControlPropertyThreadSafe(picProgress1, "Image", Properties.Resources.progress2);
                SetControlPropertyThreadSafe(lblProgress1, "Text", "Exporting Data, Please wait");                

                try
                {
                    foreach (Excel.Worksheet ws in eBook.Worksheets)
                    {
                        if (ws.Name != "IQToolsLite")
                            ws.Delete();
                    }
                }
                catch (Exception ex) {
                    LogErrorHelper(null, "<<frmMain.cs: ExportToExcel,  File.Delete>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                    MessageBox.Show(ex.Message); 
                }
                eApp.Visible = false;
                int iRow = 2;

                //Set Column Names
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    eSheet.Cells[1, j + 1] = dt.Columns[j].ColumnName;
                }

                //Populate rest of the data
                for (int rowNo = 0; rowNo < dt.Rows.Count; rowNo++)
                {
                    for (int colNo = 0; colNo < dt.Columns.Count; colNo++)
                    {
                        eSheet.Cells[iRow, colNo + 1] = dt.Rows[rowNo][colNo].ToString();
                    }
                    iRow++;
                }

                eRange = eSheet.get_Range(eSheet.Cells[1, 1], eSheet.Cells[1, dt.Columns.Count]);
                eRange.Font.Bold = true;
                eSheet = null;
                eRange = null;

                eBook.SaveAs(fileName, Excel.XlFileFormat.xlWorkbookNormal,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Excel.XlSaveAsAccessMode.xlExclusive,
                    Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing);
                //eBook.SaveAs(fileName, XlFileFormat.xlWorkbookNormal);

                success = 1;

            }
            catch (Exception ex) {
                LogErrorHelper(null, "<<frmMain.cs: ExportToExcel>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                MessageBox.Show(ex.Message); 
            }
            finally
            {
                eBook.Close(Type.Missing, Type.Missing, Type.Missing);
                eBook = null;
                eApp.Quit();
                
                SetControlPropertyThreadSafe(picProgress1, "Image", null);
                SetControlPropertyThreadSafe(lblProgress1, "Text", "");
                if (success == 1) System.Diagnostics.Process.Start(fileName);
            }
          */
        }

        private void btnEditQuery_Click(object sender, EventArgs e)
        {
            if (txtName.Text != "")
            {
                tcQueries.SelectedTab = tpDesign;
                queryBuilder1.SQL = dgvQueries.CurrentRow.Cells[4].Value.ToString();
                txtQName.Text = txtName.Text;
                txtQDesc.Text = txtDescription.Text;
                btnSaveQuery.Text = "Modify";
                txtQName.Enabled = false;
            }
            else MessageBox.Show("Please Select A Query To Edit", "IQTools");
        }                

        private void tcMain_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == tpHelp)
            {
                try
                {
                    //webBrowser1.Navigate(@".\Resources\Help\index.html");
                    //Help.ShowHelp(webBrowser1, "C:\\UserGuide.chm");
                    string curDir = Directory.GetCurrentDirectory();
                    this.webBrowser1.Url = new Uri(String.Format("file:///{0}/Resources/Help/index.html", curDir));
                }
                catch (Exception ex) {
                    LogErrorHelper(null, "<<frmMain.cs: tcMain_Selected>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                    MessageBox.Show(ex.Message); 
                }
            }
        }

        private void chkLimit_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkLimit.Checked)
            {
                label3.Text = "Do Not Limit";
                txtLimit.Enabled = false;
            }
            else if (chkLimit.Checked)
            {
                label3.Text = "Limit Results";
                txtLimit.Enabled = true;
            }
        }

        #region Account update
        private void GetAccountInfo()
        {
            UserID.Text = clsGbl.userID.ToString();
            Firstname.Text = clsGbl.userFirstName;
            Lastname.Text = clsGbl.userLastName;
            Email.Text = clsGbl.Email;
        }

        private void GetUserLocation ( )
        {
          Service1 ws = new Service1 ( );
          WebClient wc = new WebClient ( );
          string ipadress = wc.DownloadString ( "http://checkip.dyndns.org" );
          ipadress = (new Regex ( @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b" )).Match ( ipadress ).Value;

          var loc = ws.GetUserLocation ( ipadress );

          clsGbl.countryName = loc.CountryName;
          clsGbl.countryCode = loc.CountryCode;
          clsGbl.regionName = loc.RegionName;
        }
        private void BtnSaveDetails_Click_1(object sender, EventArgs e)
        {
            string email = Email.Text.Trim();
            string firstname = Firstname.Text.Trim();
            string lastname = Lastname.Text.Trim();

            if ((!string.IsNullOrEmpty(email)) || (!string.IsNullOrEmpty(firstname)) || (!string.IsNullOrEmpty(firstname)))
            {
                Service1 ws = new Service1();
                try
                {
                    LblMsg.Text = ws.ChangeDetails(clsGbl.userID, email, firstname, lastname);
                }
                catch (Exception ex) {
                    LogErrorHelper(ws, "<<frmMain.cs: BtnSaveDetails_Click_1, ChangeDetails>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                }
            }
        }

        private void BtnSubmit_Click_1(object sender, EventArgs e)
        {   
            string oldPass = OldPassword.Text.Trim();
            string pass = NewPassword.Text.Trim();
            string confirm = ConfirmPassword.Text.Trim();

            if (!string.IsNullOrEmpty(oldPass))
            {
                if ((!string.IsNullOrEmpty(pass)) && (!string.IsNullOrEmpty(confirm)))
                {
                    if (pass == confirm)
                    {
                        Service1 ws = new Service1();
                        ws.Url = clsGbl.wsURL;
                        try{
                            LblMsg.Text = ws.ChangePassword(clsGbl.userID, oldPass, pass);
                        }
                        catch (Exception ex)
                        {
                            LogErrorHelper(ws, "<<frmMain.cs: BtnSubmit_Click_1, ChangePassword>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
                        }
                    }
                    else
                        LblMsg.Text = "Passwords mismatch! Please make sure your new and confirm passwords match!!!";
                }
                else
                    LblMsg.Text = "Please make sure to fill both new and confirm password fields!!!";
            }
            else
                LblMsg.Text = "Please enter old password!!!";
        }
        #endregion

        /*
        private void msSQLParams(DataGridView dgv, int Rc)
        {
            //dgv.DataError += new DataGridViewDataErrorEventHandler(dgv_DataError);
            SqlCommand cmd = new SqlCommand(queryBuilder1.SQL);

            if (queryBuilder1.Parameters.Count > 0)
            {
                Hashtable myParameters = new Hashtable(); int j = 0; myParameters.Clear();
                for (int i = 0; i < queryBuilder1.Parameters.Count; i++)
                {
                    j = 0;
                    SqlParameter p = new SqlParameter();
                    p.ParameterName = queryBuilder1.Parameters[i].FullName;
                    p.DbType = queryBuilder1.Parameters[i].DataType;
                    foreach (DictionaryEntry de in myParameters)
                    {
                        if (de.Key.ToString().Trim().ToLower() == queryBuilder1.Parameters[i].FullName.Trim().ToLower())
                        {
                            j = 1;
                            break;
                        }
                    }
                    if (j == 0)
                    {
                        cmd.Parameters.Add(p);
                        myParameters.Add(p.ParameterName, p.DbType);
                    }
                }

                using (frmQryParameters qpf = new frmQryParameters(queryBuilder1.Parameters, cmd))
                {
                    qpf.ShowDialog();
                }

            }
        }*/

        private SqlCommand cmd = new SqlCommand();
        private bool Continue = true;
        private void msSQLParams(DataGridView dgv, int Rc)
        {
            dgv.DataError += new DataGridViewDataErrorEventHandler(dgv_DataError);
            dgv.Columns.Clear();
            dgv.DataSource = null;
            cmd.Parameters.Clear();

            //if (queryBuilder1.MetadataProvider != null && queryBuilder1.MetadataProvider.Connected && queryBuilder1.SQL.ToString().Length > 1)
            if (queryBuilder1.MetadataProvider != null && queryBuilder1.SQL.ToString().Length > 1)
            {
                //SqlCommand cmd = new SqlCommand();
                if (queryBuilder1.Parameters.Count > 0)
                {
                    Hashtable myParameters = new Hashtable();
                    int j = 0; myParameters.Clear();
                    for (int i = 0; i < queryBuilder1.Parameters.Count; i++)
                    {
                        j = 0;
                        SqlParameter p = new SqlParameter();
                        p.ParameterName = queryBuilder1.Parameters[i].FullName;
                        p.DbType = queryBuilder1.Parameters[i].DataType;
                        foreach (DictionaryEntry de in myParameters)
                        {
                            if (de.Key.ToString().Trim().ToLower() == queryBuilder1.Parameters[i].FullName.Trim().ToLower())
                            {
                                j = 1;
                                break;
                            }
                        }
                        if (j == 0)
                        {
                            cmd.Parameters.Add(p);
                            myParameters.Add(p.ParameterName, p.DbType);
                        }
                    }

                    using (frmQryParameters qpf = new frmQryParameters(queryBuilder1.Parameters, cmd))
                    {
                        qpf.ShowDialog();
                        Continue = qpf.Continue;
                    }
                }
                else
                {
                    Continue = true;
                }
                
              
            }
        }

        void dgv_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
                throw new NotImplementedException();
        }

        private void dgvPreview_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Service1 ws = new Service1();
            ws.Url = clsGbl.wsURL;
            try
            {
                ws.LogUserHistory(clsGbl.userID, "logout");
            }
            catch (Exception ex)
            {
                LogErrorHelper(ws, "<<frmMain.cs: frmMain_FormClosing>> : " + ex.Message, clsGbl.applicationName, clsGbl.userID);
            }

            try
            {
                connection.Close();
            }
            catch (Exception ex) { }
        }

       
        
    }
}

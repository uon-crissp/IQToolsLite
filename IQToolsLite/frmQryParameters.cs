using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ActiveDatabaseSoftware.ActiveQueryBuilder;

namespace IQToolsLite
{
    public partial class frmQryParameters : Form
    {
        private ParameterList parameters;
        private DbCommand command;
        public bool Continue;
        //private string sqlString;

        public frmQryParameters(ParameterList pl, DbCommand cmd)
        {
            Continue = true;
            parameters = pl;
            command = cmd;
            //sqlString = sql;

            InitializeComponent();

            int j = 0;
            Hashtable myParameters = new Hashtable();
            myParameters.Clear();

            for (int i = 0; i < parameters.Count; i++)
            {
                Parameter p = parameters[i];
                string s = "";

                j = 0;
                foreach (DictionaryEntry de in myParameters)
                {
                    if (de.Key.ToString().Trim().ToLower() == p.FullName.Trim().ToLower())
                    {
                        j = 1;
                        break;
                    }
                }
                if (j == 0)
                {
                    if (p.ComparedObject != "")
                    { s += p.ComparedObject + "."; }

                    if (p.ComparedField != "")
                    { s += p.ComparedField + " "; }

                    s += p.CompareOperator;

                    int row = dgvParameters.Rows.Add();
                    dgvParameters.Rows[row].Cells[0].Value = p.FullName;

                    dgvParameters.Rows[row].Cells[1].Value = s;
                    dgvParameters.Rows[row].Cells[2].Value = p.DataType.ToString();

                    myParameters.Add(p.FullName, p.DataType.ToString());
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            clsGbl.mrgQueries = new Hashtable();
            clsGbl.mrgQueries.Clear();
            try
            {
                foreach (DictionaryEntry de in clsGbl.mrgQueries)
                {
                    if (de.Key.ToString().ToLower().Trim() == clsGbl.currQuery.ToLower().Trim())
                    {
                        for (int i = 0; i < dgvParameters.Rows.Count; i++)
                        {
                            string QryRef = de.Value.ToString() + "0000";
                            command.Parameters[i].Value = dgvParameters.Rows[i].Cells[3].Value;

                            if (clsGbl.mrgParams.ContainsKey(QryRef.Substring(0, 3) + command.Parameters[i].ParameterName))
                                clsGbl.mrgParams.Remove(QryRef.Substring(0, 3) + command.Parameters[i].ParameterName);

                            clsGbl.mrgParams.Add(QryRef.Substring(0, 3) + command.Parameters[i].ParameterName, command.Parameters[i].Value);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "IQTools"); }

            for (int i = 0; i < dgvParameters.Rows.Count; i++) 
            command.Parameters[i].Value = dgvParameters.Rows[i].Cells[3].Value;
            //clsGbl.currQuery = command.
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Continue = false;
        }

        

        

    }
}

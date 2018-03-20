using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.Reflection;

namespace IQToolsLite
{
    public class clsGbl
    {
        public static string userName = "";
        public static int userID = 0;
        public static string userFirstName = "";
        public static string userLastName = "";
        public static string Email = "";
        public static string wsURL = "  http://41.73.195.220/iqtoolslite/Service1.asmx";
        //public static struct webService { public static string wsURL = ""; public static string wsName = "";} 
        public static Hashtable mrgParams = null;
        public static Hashtable mrgQueries = null;
        public static string currQuery = "";
        public static string[] parameters = null;
        public static string applicationName = "IQToolsLite";
        public static string PMMSType = "";
        public static string countryName = "";
        public static string countryCode = "";
        public static string regionName = "";
     
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

    }
}

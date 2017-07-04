using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using IdealAutomate.Core;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Collections;
using System.Data;

using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices;
using Microsoft.Win32;
using System.Linq;

namespace IdealSqlTracer {

    /// <summary>
    /// Steps to use:
    ///  1. Get Latest https://xp-dev.com/svn/IdealAutomate/trunk for IdealSqlTracer
    ///  2. Build and Run IdealSqlTracer
    ///  3. A red dialog box will appear on the screen telling you the trace has started and you 
    ///     need to perform the action on the website that you want to trace.  
    ///     After the action on the website is done, click the okay button in the red 
    ///     dialog box to end the trace and have the formatted sql appear in notepad.
    ///    
    /// IdealSqlTracer allows you to filter out the noise in SQL Profiler so that you can 
    /// get just the SQL that is generated behind the scenes when you are performing some 
    /// action on a web page in your localhost environment. IdealSqlTracer uses the open 
    /// source IdealAutomateCore that allows one to automate any activity on your computer 
    /// with C# for free.  If you ever need to stop a process that you have automated while it 
    /// is running, just hit the break key on your computer. The source code for IdealAutomateCore 
    /// is hosted at https://xp-dev.com/svn/IdealAutomate/
    /// 
    /// If you just want some of the generated sql on the page, you can temporarily 
    /// add the following to lines to your code where you want to start selecting the generated sql:
    ///    con = new SqlConnection("Server=yourserver;Initial Catalog=yourdatabase;Integrated Security=SSPI");
    ///    SqlCommand cmd = new SqlCommand();
    ///    cmd.CommandText = "select top 1 name from sysobjects where name = 'START_TRACE'";
    ///    cmd.Connection = con;
    ///    string strStartTrace = cmd.ExecuteScalar();
    ///    con.Close();

    ///    
    /// Then, you temporarily add the following line at the end of where you 
    /// want to stop selecting the generated sql:
    ///    
    ///    con1 = new SqlConnection("Server=yourserver;Initial Catalog=yourdatabase;Integrated Security=SSPI");
    ///    SqlCommand cmd1 = new SqlCommand();
    ///    cmd1.CommandTex1t = "select top 1 name from sysobjects where name = 'END_TRACE'";
    ///    cmd1.Connection1 = con1;
    ///    string strStartTrace = cmd1.ExecuteScalar();
    ///    con1.Close();
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            var window = new Window() //make sure the window is invisible
            {
                Width = 0,
                Height = 0,
                Left = -2000,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
            };
            window.Show();
            IdealAutomate.Core.Methods myActions = new Methods();
            InitializeComponent();
            myActions.DebugMode = true;
            string myBigSqlString = "";
            string strScriptName = "IdealSqlTracer";
            string settingsDirectory = GetAppDirectoryForScript(strScriptName);
            string fileName;
            string strSavedDomainName;
            fileName = "DomainName.txt";
            strSavedDomainName = ReadValueFromAppDataFile(settingsDirectory, fileName);
            if (strSavedDomainName == "") {
                strSavedDomainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            }

            List<ControlEntity> myListControlEntity = new List<ControlEntity>();
            ControlEntity myControlEntity = new ControlEntity();
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Heading;
            myControlEntity.Text = "Domain Name";
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "myLabel2";
            myControlEntity.Text = "Enter Domain Name";
            myControlEntity.RowNumber = 0;
            myControlEntity.ColumnNumber = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.TextBox;
            myControlEntity.ID = "myDomainName";
            myControlEntity.Text = strSavedDomainName;
            myControlEntity.ToolTipx = "To find Windows Domain, Open the Control Panel, click the System and Security " + System.Environment.NewLine + "category, and click System. Look under “Computer name, " + System.Environment.NewLine + "domain and workgroup settings” here. If you see “Domain”:" + System.Environment.NewLine + "followed by the name of a domain, your computer is joined to a domain." + System.Environment.NewLine + "Most computers running at home do not have a domain as they do" + System.Environment.NewLine + "not use Active Directory";
            myControlEntity.RowNumber = 0;
            myControlEntity.ColumnNumber = 1;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "mylabel";
            myControlEntity.ColumnSpan = 2;
            myControlEntity.Text = "(Leave domain name blank if not using server in Active Directory)";
            myControlEntity.RowNumber = 1;
            myControlEntity.ColumnNumber = 0;
            myControlEntity.Checked = true;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            string strButtonPressed = myActions.WindowMultipleControls(ref myListControlEntity, 300, 500, -1, 0);

            if (strButtonPressed == "btnCancel") {
                myActions.MessageBoxShow("Okay button not pressed - Script Cancelled");
                goto myExitApplication;
            }

            string strDomainName = myListControlEntity.Find(x => x.ID == "myDomainName").Text;

            fileName = "DomainName.txt";
            WriteValueToAppDirectoryFile(settingsDirectory, fileName, strDomainName); 

            ArrayList myServers = new ArrayList();
            List<string> servers = new List<string>();
            List<string> listLocalServers = new List<string>();

            // Get servers from the registry (if any)
            RegistryKey key = RegistryKey.OpenBaseKey(
              Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32);
            key = key.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server");
            object installedInstances = null;
            if (key != null) { installedInstances = key.GetValue("InstalledInstances"); }
            List<string> instances = null;
            if (installedInstances != null) { instances = ((string[])installedInstances).ToList(); }
            if (System.Environment.Is64BitOperatingSystem) {
                /* The above registry check gets routed to the syswow portion of 
                 * the registry because we're running in a 32-bit app. Need 
                 * to get the 64-bit registry value(s) */
                key = RegistryKey.OpenBaseKey(
                        Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
                key = key.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server");
                installedInstances = null;
                if (key != null) { installedInstances = key.GetValue("InstalledInstances"); }
                string[] moreInstances = null;
                if (installedInstances != null) {
                    moreInstances = (string[])installedInstances;
                    if (instances == null) {
                        instances = moreInstances.ToList();
                    } else {
                        instances.AddRange(moreInstances);
                    }
                }
            }
            foreach (string item in instances) {
                string name = System.Environment.MachineName;
                if (item != "MSSQLSERVER") { name += @"\" + item; }
                if (!servers.Contains(name.ToUpper())) {
                    myServers.Add(name.ToUpper());
                    listLocalServers.Add(name.ToUpper());
                }
            }

            try {
                string myldap = FriendlyDomainToLdapDomain(strDomainName);

                string distinguishedName = string.Empty;
                string connectionPrefix = "LDAP://" + myldap;
                DirectoryEntry entry = new DirectoryEntry(connectionPrefix);

                DirectorySearcher mySearcher = new DirectorySearcher(entry);
                mySearcher.Filter = "(&(objectClass=Computer)(operatingSystem=Windows Server*) (!cn=wde*))";
                mySearcher.PageSize = 1000;
                mySearcher.PropertiesToLoad.Add("name");

                SearchResultCollection result = mySearcher.FindAll();
                foreach (SearchResult item in result) {
                    // Get the properties for 'mySearchResult'.
                    ResultPropertyCollection myResultPropColl;

                    myResultPropColl = item.Properties;

                    foreach (Object myCollection in myResultPropColl["name"]) {
                        myServers.Add(myCollection.ToString());
                    }
                }

                entry.Close();
                entry.Dispose();
                mySearcher.Dispose();
            } catch (Exception) {
                // do not show exception because they may not be using active directory
            }
            myServers.Sort();
            fileName = "Servers.txt";
            WriteArrayListToAppDirectoryFile(settingsDirectory, fileName, myServers);

            myListControlEntity = new List<ControlEntity>();
            myControlEntity = new ControlEntity();
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Heading;
            myControlEntity.Text = "Select Server";
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "myLabel2";
            myControlEntity.Text = "Select Server";
            myControlEntity.RowNumber = 0;
            myControlEntity.ColumnNumber = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.ComboBox;
            myControlEntity.ID = "myComboBox";
            myControlEntity.Text = "";
            List<ComboBoxPair> cbp = new List<ComboBoxPair>();
            fileName = "Servers.txt";
            myServers = ReadAppDirectoryFileToArrayList(settingsDirectory, fileName);
            foreach (var item in myServers) {
                cbp.Add(new ComboBoxPair(item.ToString(), item.ToString()));
            }
            myControlEntity.ListOfKeyValuePairs = cbp;
            fileName = "ServerSelectedValue.txt";
            myControlEntity.SelectedValue = ReadValueFromAppDataFile(settingsDirectory, fileName);
            myControlEntity.RowNumber = 0;
            myControlEntity.ColumnNumber = 1;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());
            int intRowCtr = 1;

            intRowCtr++;
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "lblAlternateServer";
            myControlEntity.Text = "Alternate Server";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.TextBox;
            myControlEntity.ID = "txtAlternateServer";
            fileName = "txtAlternateServer.txt";
            myControlEntity.Text = ReadValueFromAppDataFile(settingsDirectory, fileName);
            myControlEntity.ToolTipx = "If server was not in dropdown, you can type it here; otherwise, leave blank";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 1;
            myControlEntity.ColumnSpan = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            intRowCtr++;
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "lblUserName";
            myControlEntity.Text = "UserName";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.TextBox;
            myControlEntity.ID = "txtUserName";
            fileName = "txtUserName.txt";
            myControlEntity.Text = ReadValueFromAppDataFile(settingsDirectory, fileName);
            myControlEntity.ToolTipx = "User Name for logging into server";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 1;
            myControlEntity.ColumnSpan = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            intRowCtr++;
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "lblPassword";
            myControlEntity.Text = "Password";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.PasswordBox;
            myControlEntity.ID = "txtPassword";
            fileName = "txtPss.txt";
            string strPass = "";          
            string inputFullFileName1 = Path.Combine(settingsDirectory, fileName.Replace(".", "Encrypted."));
            string outputFullFileName1 = Path.Combine(settingsDirectory, fileName);
            if (File.Exists(inputFullFileName1)) {                
                EncryptDecrypt ed1 = new EncryptDecrypt();
                ed1.DecryptFile(inputFullFileName1, outputFullFileName1);
                strPass = ReadValueFromAppDataFile(settingsDirectory, fileName);
            }
            File.Delete(outputFullFileName1);
            myControlEntity.Text = strPass;
            myControlEntity.ToolTipx = "Password for logging into server";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 1;
            myControlEntity.ColumnSpan = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            intRowCtr++;
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.CheckBox;
            myControlEntity.ID = "myCheckBox";
            myControlEntity.Text = "Remember Password";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 0;
            fileName = "RememberPassword.txt";
            string strRememberPassword = ReadValueFromAppDataFile(settingsDirectory, fileName);
            if (strRememberPassword.ToLower() == "true") {
                myControlEntity.Checked = true;
            } else {
                myControlEntity.Checked = false;
            }
            myControlEntity.ForegroundColor = System.Windows.Media.Colors.Red;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            strButtonPressed = myActions.WindowMultipleControls(ref myListControlEntity, 300, 500, -1, 0);

            if (strButtonPressed == "btnCancel") {
                myActions.MessageBoxShow("Okay button not pressed - Script Cancelled");
                goto myExitApplication;
            }

            bool boolRememberPassword = myListControlEntity.Find(x => x.ID == "myCheckBox").Checked;
            fileName = "RememberPassword.txt";
            WriteValueToAppDirectoryFile(settingsDirectory, fileName, boolRememberPassword.ToString());
            string strServerName = myListControlEntity.Find(x => x.ID == "myComboBox").SelectedValue;
            fileName = "ServerSelectedValue.txt";
            WriteValueToAppDirectoryFile(settingsDirectory, fileName, strServerName);
            string strAlternateServer = myListControlEntity.Find(x => x.ID == "txtAlternateServer").Text;
            fileName = "txtAlternateServer.txt";
            WriteValueToAppDirectoryFile(settingsDirectory, fileName, strAlternateServer);
            string strUserName = myListControlEntity.Find(x => x.ID == "txtUserName").Text;
            fileName = "txtUserName.txt";
            WriteValueToAppDirectoryFile(settingsDirectory, fileName, strUserName);
            string strPassword = myListControlEntity.Find(x => x.ID == "txtPassword").Text;
            fileName = "txtPss.txt";
            if (boolRememberPassword) {
                WriteValueToAppDirectoryFile(settingsDirectory, fileName, strPassword);
                string inputFullFileName = Path.Combine(settingsDirectory, fileName);
                string outputFullFileName = Path.Combine(settingsDirectory, fileName.Replace(".","Encrypted."));
                EncryptDecrypt ed = new EncryptDecrypt();
                ed.EncryptFile(inputFullFileName, outputFullFileName);
                File.Delete(inputFullFileName);
            }
             
            if (strAlternateServer.Trim() != "") {
                strServerName = strAlternateServer;
            }

            myListControlEntity = new List<ControlEntity>();

            myControlEntity = new ControlEntity();
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Heading;
            myControlEntity.Text = "Select Database";
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "myLabel2";
            myControlEntity.Text = "Select Database";
            myControlEntity.RowNumber = 0;
            myControlEntity.ColumnNumber = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());
            ArrayList myDatabases = new ArrayList();
            string serverName = strServerName;
            DataTable dtDatabases = GetDatabases(serverName);
            try {
                for (int i = 0; i < dtDatabases.Rows.Count; i++) {
                    DataRow dr = dtDatabases.Rows[i];
                    myDatabases.Add(dr["sysdbreg_name"]);
                }

            } catch (Exception ex) {
                string exception = ex.Message;
                myActions.MessageBoxShow(exception);
            }

            fileName = "Databases.txt";
            WriteArrayListToAppDirectoryFile(settingsDirectory, fileName, myDatabases);
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.ComboBox;
            myControlEntity.ID = "myComboBox";
            myControlEntity.Text = "";
            cbp = new List<ComboBoxPair>();
            fileName = "Databases.txt";
            myDatabases = ReadAppDirectoryFileToArrayList(settingsDirectory, fileName);
            foreach (var item in myDatabases) {
                cbp.Add(new ComboBoxPair(item.ToString(), item.ToString()));
            }
            myControlEntity.ListOfKeyValuePairs = cbp;
            fileName = "DatabaseSelectedValue.txt";
            myControlEntity.SelectedValue = ReadValueFromAppDataFile(settingsDirectory, fileName);
            myControlEntity.RowNumber = 0;
            myControlEntity.ColumnNumber = 1;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            intRowCtr = 0;

            intRowCtr++;
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "lblLocalOutputFolder1";
            myControlEntity.Text = "IMPORTANT: SQL Server needs full control permission to the TraceFile.trc that" + System.Environment.NewLine + "is in the output folder. Failure to do this results in a long list of repetitive error messages";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ForegroundColor = System.Windows.Media.Colors.White;
            myControlEntity.BackgroundColor = System.Windows.Media.Colors.Red;
            myControlEntity.FontWeight = FontWeights.ExtraBold;
            myControlEntity.ColumnNumber = 0;
            myControlEntity.ColumnSpan = 2;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            intRowCtr++;
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "lblLocalOutputFolder";
            myControlEntity.Text = "Local Output Folder";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.TextBox;
            myControlEntity.ID = "txtLocalOutputFolder";
            fileName = "txtLocalOutputFolder.txt";
            myControlEntity.Text = ReadValueFromAppDataFile(settingsDirectory, fileName);
            myControlEntity.ToolTipx = "If the SQL Server you are tracing is installed on your local computer, " 
                + System.Environment.NewLine + "specify the folder where the trace can be written to. " 
                + System.Environment.NewLine + "SQLServerMSSQLUser needs Full Control permission rights must be specified for this folder."
                 + System.Environment.NewLine + "EXAMPLE: C:\\Data\\";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 1;
            myControlEntity.ColumnSpan = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            intRowCtr++;
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "lblRemoteOutputFolder";
            myControlEntity.Text = "RemoteOutputFolder";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.TextBox;
            myControlEntity.ID = "txtRemoteOutputFolder";
            fileName = "txtRemoteOutputFolder.txt";
            myControlEntity.Text = ReadValueFromAppDataFile(settingsDirectory, fileName);
            myControlEntity.ToolTipx = "If the SQL Server you are tracing is installed on a remote computer, " + 
            System.Environment.NewLine + "specify the folder where the trace can be written to." + 
            System.Environment.NewLine + "SQLServer must have Full Control permission rights for must be specified for the file TraceFile.trc. " + 
            System.Environment.NewLine + "If you do not have rights to the remote computer, use shared drive." +
            System.Environment.NewLine + "EXAMPLE: \\\\NetworkShare\\Users\\Wade\\Data\\";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 1;
            myControlEntity.ColumnSpan = 0;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            intRowCtr++;
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.Label;
            myControlEntity.ID = "lblLocalOutputFolder";
            myControlEntity.Text = "IMPORTANT: if not using localhost, you must uncheck to see any results";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ForegroundColor = System.Windows.Media.Colors.White;
            myControlEntity.BackgroundColor = System.Windows.Media.Colors.Red;
            myControlEntity.FontWeight = FontWeights.ExtraBold;
            myControlEntity.ColumnNumber = 0;
            myControlEntity.ColumnSpan = 2;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            intRowCtr++;
            myControlEntity.ControlEntitySetDefaults();
            myControlEntity.ControlType = ControlType.CheckBox;
            myControlEntity.ID = "myCheckBox";
            myControlEntity.Text = "Using localhost (w3wp and dllhost only)";
            myControlEntity.RowNumber = intRowCtr;
            myControlEntity.ColumnNumber = 0;
            myControlEntity.Checked = true;
            myControlEntity.ForegroundColor = System.Windows.Media.Colors.Red;
            myListControlEntity.Add(myControlEntity.CreateControlEntity());

            strButtonPressed = myActions.WindowMultipleControls(ref myListControlEntity, 300, 500, -1, 0);

            if (strButtonPressed == "btnCancel") {
                myActions.MessageBoxShow("Okay button not pressed - Script Cancelled");
                goto myExitApplication;
            }

            string strDatabaseName = myListControlEntity.Find(x => x.ID == "myComboBox").SelectedValue;
            bool boolUsingLocalhost = myListControlEntity.Find(x => x.ID == "myCheckBox").Checked;
            fileName = "DatabaseSelectedValue.txt";
            WriteValueToAppDirectoryFile(settingsDirectory, fileName, strDatabaseName);
            string strLocalOutputFolder = myListControlEntity.Find(x => x.ID == "txtLocalOutputFolder").Text;
            fileName = "txtLocalOutputFolder.txt";
            WriteValueToAppDirectoryFile(settingsDirectory, fileName, strLocalOutputFolder);
            string strRemoteOutputFolder = myListControlEntity.Find(x => x.ID == "txtRemoteOutputFolder").Text;
            fileName = "txtRemoteOutputFolder.txt";
            WriteValueToAppDirectoryFile(settingsDirectory, fileName, strRemoteOutputFolder);

            // Run SqlProfiler
            // Get ProcessID for w3wp.exe
            if (!strRemoteOutputFolder.EndsWith("\\")) {
                strRemoteOutputFolder = strRemoteOutputFolder + "\\";
            }
            if (!strLocalOutputFolder.EndsWith("\\")) {
                strLocalOutputFolder = strLocalOutputFolder + "\\";
            }
            string strTraceFullFileName = strRemoteOutputFolder + "TraceFile.trc";
            string strServerType = "Remote";
            if (listLocalServers.Contains(strServerName.ToUpper())) {
                strTraceFullFileName = strLocalOutputFolder + "TraceFile.trc";
                strServerType = "Local";
            }

            strDatabaseName = strDatabaseName.Replace("[", "").Replace("]", "");
            int intTraceID = 0;
            SqlConnection thisConnection = new SqlConnection("server=" + strServerName + ";" + "Persist Security Info=True;User ID=" + strUserName + ";Password=" + strPassword + ";database=" + strDatabaseName + "");
            //First insert some records
            //Create Command object
            SqlCommand myCommand = thisConnection.CreateCommand();

            List<int> w3wp_PID = new List<int>();
            List<int> dllhost_PID = new List<int>();
            Process[] localAll = Process.GetProcesses();
            try {
                // Open Connection
                thisConnection.Open();
                Console.WriteLine("Connection Opened");
                myCommand.CommandText = "declare @trace_id int " +
                "select @trace_id = id from sys.traces " +
                @"where path = '" + strTraceFullFileName + "' " +
                "if @trace_id is not null " +
                "begin " +
                "	exec sp_trace_setstatus @trace_id, 0  " +
                "	exec sp_trace_setstatus @trace_id, 2  " +
                "end; ";
                myCommand.ExecuteNonQuery();
                // need to delete trace file if it exists
                if (System.IO.File.Exists(strTraceFullFileName)) {
                    System.IO.File.Delete(strTraceFullFileName);
                }

                // Create INSERT statement with named parameters
                myCommand.CommandText = "/* sys.traces shows the existing sql traces on the server */ " +
                "/* " +
                "select * from sys.traces " +
                "*/  " +
                "/*create a new trace, make sure the @tracefile must NOT exist on the disk yet*/ " +
                "declare @tracefile nvarchar(500) set @tracefile=N'" + strTraceFullFileName.Replace(".trc", "") + "' " +
                "declare @trace_id int " +
                "declare @maxsize bigint " +
                "set @maxsize =1 " +
                "exec sp_trace_create @trace_id output,2,@tracefile ,@maxsize " +
                "select @trace_id " +
                "  " +
                "/* add the events of insterest to be traced, and add the result columns of interest " +
                "  Note: look up in sys.traces to find the @trace_id, here assuming this is the first trace in the server, therefor @trce_id = 5 " +
                "*/ " +
                "declare @on bit " +
                "set @on=1 " +
                "declare @current_num int " +
                "set @current_num =1 " +
                "while(@current_num <65) " +
                "      begin " +
                "	  /* " +
                "      add events to be traced, id 14 is the login event, you add other events per your own requirements, the event id can be found @ BOL http://msdn.microsoft.com/en-us/library/ms186265.aspx " +
                "      */ " +
                "	  exec sp_trace_setevent @trace_id,10, @current_num,@on " +
                "      set @current_num=@current_num+1 " +
                "      end " +
                "set @current_num =1 " +
                "while(@current_num <65) " +
                "      begin " +
                "      exec sp_trace_setevent @trace_id,12, @current_num,@on " +
                "      set @current_num=@current_num+1 " +
                "      end " +
                " " +
                "/* set some filters " +
                "   " +
                "--exec sp_trace_setfilter [ @traceid = ] trace_id    " +
                "--          , [ @columnid = ] column_id   " +
                "--          , [ @logical_operator = ] logical_operator   " +
                "--          , [ @comparison_operator = ] comparison_operator   " +
                "--          , [ @value = ] value   " +
                "-- Columns " +
                "-- ApplicationName (10) " +
                "-- NTUserName (6) " +
                "-- TextData (1) " +
                "-- Comparison operators (0: equal; 1: not equal; 6: like; 7: Not like) " +
                "--Filters: " +
                "-- DatabaseName (35) " +
                "--Application Name: (10) " +
                "--Like: (6) " +
                "--.N% " +
                "--Micro% " +
                " " +
                "--NTUserName: (6) " +
                "--Not Like: " +
                "--myusername " +
                 " " +
                "--TextData: (1) " +
                "--Not Like: " +
                "--exec sp_reset_connection " +
                "*/ " +
                " " +
                "exec sp_trace_setfilter  @trace_id, 35, 1, 0, N'" + strDatabaseName + "';   " +
                   "  " +
                "exec sp_trace_setfilter  @trace_id, 1, 0, 7, N'exec sp_reset_connection';   " +
                " " +
                "exec sp_trace_setfilter  @trace_id, 1, 0, 7, N'/* sys.traces%';  " +
                " " +
                "/* " +
                " " +
                "--turn on the trace: status=1 " +
                "-- use sys.traces to find the @trace_id, here assuming this is the first trace in the server, so @trce_id = 5 " +
                "*/ " +
                "exec sp_trace_setstatus  @trace_id,1 " +
                "  " +
                "/* pivot the traced event */ " +
                "/* " +
                "select LoginName,DatabaseName,TextData,ClientProcessID,* from fn_trace_gettable(N'" + strTraceFullFileName + "',default) " +
                "*/ " +
                "  " +
                "/* stop trace. Please manually delete the trace file on the disk " +
                "-- use sys.traces to find the @trace_id, here assuming this is the first trace in the server, so @trce_id = 5 " +
                "declare @trace_id int " +
                "set @trace_id=2 " +
                "exec sp_trace_setstatus @trace_id,0 " +
                "exec sp_trace_setstatus @trace_id,2 " +
                "*/ ";
                
                // Prepare command for repeated execution
                myCommand.Prepare();

                Console.WriteLine("Executing {0}", myCommand.CommandText);
                intTraceID = (int)myCommand.ExecuteScalar();
                Console.WriteLine("TraceID : {0}", intTraceID);
                myActions.WindowShape("GreenBox", "", "", " Trace has been started;\r\n Please perform website\\desktop app action;\r\n After website\\desktop app action completes,\r\n click stop button in this green box to end trace", 400, 500);

                foreach (var item in localAll) {
                    if (item.ProcessName == "w3wp") {
                        w3wp_PID.Add(item.Id);
                    }
                    if (item.ProcessName == "dllhost") {
                        dllhost_PID.Add(item.Id);
                    }
                }
                myCommand.CommandText = @"select TextData,ClientProcessID, DatabaseName from fn_trace_gettable(N'" + strTraceFullFileName + "',default)";
                SqlDataReader reader = myCommand.ExecuteReader();
                //(CommandBehavior.SingleRow)
                string strTextData = "";
                int intClientProcessID = -1;
                string strRowDatabaseName = "";

                while (reader.Read()) {
                    if (!reader.IsDBNull(0)) {
                        strTextData = reader.GetString(0) ?? "";
                    }
                    if (!reader.IsDBNull(1)) {
                        intClientProcessID = reader.GetInt32(1);
                    }
                    if (!reader.IsDBNull(2)) {
                        strRowDatabaseName = reader.GetString(2) ?? "";
                    }

                    bool boolListItemGood = false;
                    foreach (var PID in w3wp_PID) {
                        if (intClientProcessID == PID) {
                            boolListItemGood = true;
                        }
                    }
                    foreach (var PID in dllhost_PID) {
                        if (intClientProcessID == PID) {
                            boolListItemGood = true;
                        }
                    }
                    if (boolUsingLocalhost == false) {
                        if (strRowDatabaseName == strDatabaseName) {
                            boolListItemGood = true;
                        }
                    }
                    if (boolListItemGood) {

                        string mySqlString = "";
                        mySqlString = strTextData;
                        if (mySqlString.Contains("END_TRACE")) {
                            goto myExit;
                        }
                        if (mySqlString.Contains("START_TRACE")) {
                            myBigSqlString = "";
                        } else {
                            myBigSqlString += "\r\n";
                            myBigSqlString += mySqlString;
                        }

                    }
                }
                reader.Close();
                myCommand.CommandText = "declare @trace_id int " +
        "set @trace_id=  " + intTraceID.ToString() + " " +
        "exec sp_trace_setstatus @trace_id,0 " +
        "exec sp_trace_setstatus @trace_id,2 ";
                myCommand.ExecuteNonQuery();

            } catch (SqlException ex) {
                // Display error
                myActions.MessageBoxShow("Error: " + ex.ToString());
                Console.WriteLine("Error: " + ex.ToString());
            } finally {
                // Close Connection
                thisConnection.Close();
                Console.WriteLine("Connection Closed");
            }
            
            myExit:
            // close SQL Profiler
            //myActions.TypeText("%fx", 900);
            if (myBigSqlString == "") {
                myActions.MessageBoxShow("No SQL was generated that was running under w3wp,dllhost, or the selected db ");
                goto myExitApplication;
            }
            string strOutFile = @"c:\\Data\UnformattedSql.sql";
            if (File.Exists(strOutFile)) {
                File.Delete(strOutFile);
            }
            using (System.IO.StreamWriter filex = new System.IO.StreamWriter(strOutFile)) {

                filex.Write(myBigSqlString);
            }

            try {
                string strAppPath = System.AppDomain.CurrentDomain.BaseDirectory;
                strAppPath = strAppPath.Replace("bin\\Debug\\", "");
                myActions.RunSync(strAppPath + "SqlFormatter.exe", @"c:\\Data\UnformattedSql.sql /o:c:\\Data\FormattedSql.sql");

            } catch (Exception ex) {
                myActions.MessageBoxShow(ex.Message);
                myActions.MessageBoxShow(ex.StackTrace);
                goto myExitApplication;
            }

            string strExecutable = @"C:\Windows\system32\notepad.exe";
            string strContent = @"c:\\Data\FormattedSql.sql";
            Process.Start(strExecutable, string.Concat("", strContent, ""));
            myExitApplication:
            Application.Current.Shutdown();
        }

        private ArrayList ReadAppDirectoryFileToArrayList(string settingsDirectory, string fileName) {
            ArrayList myArrayList = new ArrayList();
            string settingsPath = Path.Combine(settingsDirectory, fileName);
            StreamReader reader = File.OpenText(settingsPath);
            while (!reader.EndOfStream) {
                string myLine = reader.ReadLine();
                myArrayList.Add(myLine);
            }
            reader.Close();
            return myArrayList;
        }

        private string ReadValueFromAppDataFile(string settingsDirectory, string fileName) {
            StreamReader file = null;
            string strValueRead = "";
            string settingsPath = Path.Combine(settingsDirectory, fileName);
            if (File.Exists(settingsPath)) {
                file = File.OpenText(settingsPath);
                strValueRead = file.ReadToEnd();
                file.Close();
            }
            return strValueRead;
        }

        public string FriendlyDomainToLdapDomain(string friendlyDomainName) {
            string ldapPath = null;
            try {
                DirectoryContext objContext = new DirectoryContext(
                    DirectoryContextType.Domain, friendlyDomainName);
                Domain objDomain = Domain.GetDomain(objContext);
                ldapPath = objDomain.Name;
            } catch (DirectoryServicesCOMException e) {
                ldapPath = e.Message.ToString();
            }
            return ldapPath;
        }
        private void WriteValueToAppDirectoryFile(string settingsDirectory, string fileName, string strValueToWrite) {
            StreamWriter writer = null;
            string settingsPath = Path.Combine(settingsDirectory, fileName);
            // Hook a write to the text file.
            writer = new StreamWriter(settingsPath);
            // Rewrite the entire value of s to the file
            writer.Write(strValueToWrite);
            writer.Close();
        }

        private void WriteArrayListToAppDirectoryFile(string settingsDirectory, string fileName, ArrayList arrayListToWrite) {
            StreamWriter writer = null;
            string settingsPath = Path.Combine(settingsDirectory, fileName);
            // Hook a write to the text file.
            writer = new StreamWriter(settingsPath);
            // Rewrite the entire value of s to the file
            foreach (var item in arrayListToWrite) {
                writer.WriteLine(item.ToString());                
            }
            writer.Close();
        }

        private string GetAppDirectoryForScript(string strScriptName) {
            string settingsDirectory =
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\IdealAutomate\\" + strScriptName;
            if (!Directory.Exists(settingsDirectory)) {
                Directory.CreateDirectory(settingsDirectory);
            }
            return settingsDirectory;
        }

        private DataTable GetDatabases(string ServerName) {
            string queryString =
      "SELECT name FROM master.dbo.sysdatabases " +
      "";
            // Define Connection String
            string strConnectionString = null;
            strConnectionString = @"Data Source=" + ServerName + ";Integrated Security=SSPI";
            // Define .net fields to hold each column selected in query
            String str_sysdbreg_name;
            // Define a datatable that we will define columns in to match the columns
            // selected in the query. We will use sqldatareader to read the results
            // from the sql query one row at a time. Then we will add each of those
            // rows to the datatable - this is where you can modify the information
            // returned from the sql query one row at a time. 
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(strConnectionString)) {
                SqlCommand command = new SqlCommand(queryString, connection);

                connection.Open();

                SqlDataReader reader = command.ExecuteReader();
                // Define a column in the table for each column that was selected in the sql query 
                // We do this before the sqldatareader loop because the columns only need to be  
                // defined once. 

                DataColumn column = null;
                column = new DataColumn("sysdbreg_name", Type.GetType("System.String"));
                dt.Columns.Add(column);
                // Read the results from the sql query one row at a time 
                while (reader.Read()) {
                    // define a new datatable row to hold the row read from the sql query 
                    DataRow dataRow = dt.NewRow();
                    // Move each field from the reader to a holding field in .net 
                    // ******************************************************************** 
                    // The holding field in .net is where you can alter the contents of the 
                    // field 
                    // ******************************************************************** 
                    // Then, you move the contents of the holding .net field to the column in 
                    // the datarow that you defined above 
                    if (!(reader.IsDBNull(0))) {
                        str_sysdbreg_name = reader.GetString(0);
                        dataRow["sysdbreg_name"] = str_sysdbreg_name;
                    }
                    // Add the row to the datatable 
                    dt.Rows.Add(dataRow);
                }

                // Call Close when done reading. 
                reader.Close();
            }
           
            return dt;
        }
    }
}


# IdealSqlTracer
IdealSqlTracer is an alternative to SQL Profiler. It formats and puts all of the SQL in notepad.<br/>

  Steps to use:<br/>
      1. Get Latest source for IdealSqlTracer at https://github.com/harvey007y/IdealSqlTracer <br/>
      2. Build and Run IdealSqlTracer<br/>
      3. A series of dialog boxes will appear that allow you to specify the server, database, username, password, and so on.<br/>
      <img src="http://www.idealautomate.com/images/DomainName.PNG" border="0" alt="Windows Domain Name for Active Directory" /><br/>
      4. Once the basic info is entered, a red dialog box is displayed telling you the trace has started. That dialog tells you that you 
         need to perform the action on the website that you want to trace. After the action on the website is done, click the okay button in the red dialog box to end the trace and have the formatted sql appear in notepad.<br/>

     IdealSqlTracer allows you to filter out the noise in SQL Profiler so that you can get just the SQL that is
     generated behind the scenes when you are performing some action on a web page or a desktop application. 
     The application comes with filtering to just get the generated sql for the database that you specify, 
     but you can add a lot more filters in the source code in the app.
          
     If you just want some of the generated sql on the page, you can temporarily 
     add the following to lines to your code where you want to start selecting the generated sql:
     
        con = new SqlConnection("Server=yourserver;Initial Catalog=yourdatabase;Integrated Security=SSPI");
        SqlCommand cmd = new SqlCommand();
        cmd.CommandText = "select top 1 name from sysobjects where name = 'START_TRACE'";
        cmd.Connection = con;
        string strStartTrace = cmd.ExecuteScalar();
        con.Close();

        
     Then, you temporarily add the following line at the end of where you 
     want to stop selecting the generated sql:
        
        con1 = new SqlConnection("Server=yourserver;Initial Catalog=yourdatabase;Integrated Security=SSPI");
        SqlCommand cmd1 = new SqlCommand();
        cmd1.CommandTex1t = "select top 1 name from sysobjects where name = 'END_TRACE'";
        cmd1.Connection1 = con1;
        string strEndTrace = cmd1.ExecuteScalar();
        con1.Close();<br/>
        
     When IdealSqlTracer finds the phrase START_TRACE in a sql query, it will restart the trace at that point.
     Similarly when IdealSqlTracer encounters sql that contains the phrase END_TRACE, it ends the trace prematurely.
     This allows you to use the above two code snippets to wrap around a piece of your code to help you in isolating
     what sql is getting generated.
     

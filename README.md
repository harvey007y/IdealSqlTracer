# IdealSqlTracer
IdealSqlTracer is a nice, free, open source alternative to SQL Profiler. The advantage of IdealSqlTracer is that it takes all of the sql generated behind the scenes in a desktop application or web page, and it formats it to make it easily readable. IdealSqlTracer takes this beautifully formatted sql, and puts it into notepad. This allows you to easily cut-n-paste the sql in notepad, and run it directly in Sql Server Management Studio (SSMS). The advantage of doing this is that it makes it possible for you to see exactly what is going on in your application or website.<br/>

  Steps to use:<br/>
      1. Get Latest source for IdealSqlTracer at https://github.com/harvey007y/IdealSqlTracer <br/>
      2. Build and Run IdealSqlTracer<br/>
      3. A series of dialog boxes will appear that allow you to specify the server, database, username, password, and so on. Here are screenshots of the dialog boxes:<br/>
      <center><img src="http://www.idealautomate.com/images/DomainName.PNG" border="0" alt="Windows Domain Name for Active Directory" /></center><br/>
            <center><img src="http://www.idealautomate.com/images/SelectServer.PNG" border="0" alt="Select Server that sql server runs on" /></center><br/>
                        <center><img src="http://www.idealautomate.com/images/Database.PNG" border="0" alt="Select Server that sql server runs on" /></center><br/>4. Once the basic info is entered, a red dialog box is displayed telling you the trace has started. That dialog tells you that you need to perform the action on the website that you want to trace. After the action on the website is done, click the okay button in the red dialog box to end the trace and have the formatted sql appear in notepad.  The next screenshot shows the redBox dialog that pops up when I am trying to trace what is going on behind a desktop app called IdealAutomate. After the redbox appears, I hit save in the IdealAutomate application to cause some sql to be generated. <br/>
          <center><img src="http://www.idealautomate.com/images/RedBox2.PNG" border="0" alt="RedBox" /></center><br/>
          After the save completes in IdealAutomate, I hit okay in the redbox dialog, to see the following formatted sql in notepad that was used in the save:
           <center><img src="http://www.idealautomate.com/images/Notepad.PNG" border="2" alt="Notepad" /></center><br/>
           I can cut-n-paste this sql from notepad into SSMS to run it in realtime so that I can identify where any problems might be.

          
     
     

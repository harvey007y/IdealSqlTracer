# IdealSqlTracer
 <p align="center">
<strong>IdealSqlTracer has now been integrated into  <a href="https://github.com/harvey007y/IdealAutomate" style="color:white;font-weight:bold;text-decoration:none;">IdealAutomate</a> Repository. Please visit  <a href="https://github.com/harvey007y/IdealAutomate"  style="color:white;font-weight:bold;text-decoration:none;">IdealAutomate</a> to get the latest integrated solution.</strong>
 </p>
 <p align="center">
 <a href="//pluralsight.pxf.io/c/1194222/424552/7490" target="_blank" style="color:white;font-weight:bold;text-decoration:none;">Save 15% On Pluralsight - Annual Subscription Only $299</a>
<br />
<a target="_blank" href="//pluralsight.pxf.io/c/1194222/424552/7490"><img src="http://a.impactradius-go.com/display-ad/7490-431393" border="0" height="300"/></a> <a target="_blank" href="http://payscale.com"><img src="http://www.payscale.com/images/llb/payscale_banner_120x240.gif" border="0" height="300" /></a> <a target="_blank" href="//pluralsight.pxf.io/c/1194222/424552/7490"><img src="http://idealprogrammer.com/wp-photos/SoftwareDeveloper300.png" border="0" height="300" /></a>
 </p>
                             <br /><br />
IdealSqlTracer is a simple, free, open source alternative to SQL Profiler. The advantage of IdealSqlTracer is that it takes all of the sql generated behind the scenes in a desktop application or web page, and it formats it to make it easily readable. IdealSqlTracer takes this beautifully formatted sql, and puts it into notepad. This allows you to easily cut-n-paste the sql in notepad, and run it directly in Sql Server Management Studio (SSMS). The advantage of doing this is that it makes it possible for you to see exactly what is going on in your application or website. IdealSqlTracer utilizes sp_trace_create, sp_trace_filter, sp_trace_setstatus sql procs to create these custom traces for you.<br/>

[![IdealSqlTracer Overview Video](http://www.idealautomate.com/images/IdealSqlTracer.PNG)](https://www.youtube.com/watch?v=oek38x27tzc)]
<br/>
  Steps to use:<br/>
      1. Get Latest source for IdealSqlTracer at https://github.com/harvey007y/IdealSqlTracer <br/>
      2. Build and Run IdealSqlTracer<br/>
      3. A series of dialog boxes will appear that allow you to specify the server, database, username, password, and so on. Here are screenshots of the dialog boxes:<br/>
      <center><img src="http://www.idealautomate.com/images/DomainName.PNG" border="0" alt="Windows Domain Name for Active Directory" /></center><br/>
            <center><img src="http://www.idealautomate.com/images/SelectServer.PNG" border="0" alt="Select Server that sql server runs on" /></center><br/>
                        <center><img src="http://www.idealautomate.com/images/Database.PNG" border="0" alt="Select Server that sql server runs on" /></center><br/>
                        <center><img src="http://www.idealautomate.com/images/Filters.PNG" border="0" alt="Select Server that sql server runs on" /></center><br/>
                        4. Once the basic info is entered, a green dialog box is displayed telling you the trace has started. That dialog tells you that you need to perform the action on the website that you want to trace. After the action on the website is done, click the okay button in the green dialog box to end the trace and have the formatted sql appear in notepad.  The next screenshot shows the green dialog box that pops up when I am trying to trace what is going on behind a desktop app called IdealAutomate. After the green dialog box appears, I hit save in the IdealAutomate application to cause some sql to be generated. <br/><br/>
          <center><img src="http://www.idealautomate.com/images/GreenBox.PNG" border="0" alt="GreenBox" /></center><br/>
          After the save completes in IdealAutomate, I hit okay in the green dialog box, to see the following formatted sql in notepad that was used in the save:
           <center><img src="http://www.idealautomate.com/images/Notepad.PNG" border="2" alt="Notepad" /></center><br/>
           I can cut-n-paste this sql from notepad into SSMS to run it in realtime so that I can identify where any problems might be.

          
     
     

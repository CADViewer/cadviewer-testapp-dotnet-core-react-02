using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json.Serialization;
using System.Web;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
//using Newtonsoft.Json.Linq;
using EASendMail;
using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json;
//using System.Net;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;






namespace cadviewer.Controllers
{

    [ApiController]
    [Route("[controller]")]

    public class CADViewerController : Controller
    {

        private static readonly JsonSerializerSettings _options 
            = new() { NullValueHandling = NullValueHandling.Ignore };




        //Microsoft.AspNetCore.Hosting.IWebHostEnvironment.

        private readonly IConfiguration _config;
        //private readonly IHostingEnvironment _env;
        private readonly IWebHostEnvironment _env;

        public CADViewerController(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }


        // Test

        
        [HttpGet("HelloWorld1")]
        public JsonResult HelloWorld1()
        {
            return Json("Hello World1 json!");
        }



        [HttpGet("helloworld")]
        public string HelloWorld()
        {
            return ("Hello World!");
        }


        [HttpGet("HelloWorld2")]
        public string HelloWorld2()
        {
            return ("Hello World2!");
        }




        // get Drawing

        [HttpGet("getfile")]
        public string GetFile(string remainOnServer, string fileTag, string Type)
        {

            fileTag = fileTag.Trim('/');
            string fileType = Type;
            byte[] bytes;

            string fileLocation = _config.GetValue<string>("CADViewer:fileLocation");
            string localPath = fileLocation + fileTag + "." + fileType;

            //context.Response.Write("Hello "+localPath+"  "+remainOnServer+"  ");
            try
            {
                using (FileStream fsSource = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                {

                    // Read the source file into a byte array.
                    bytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;

                }
            }
            catch (FileNotFoundException ioEx)
            {
                Console.WriteLine(ioEx.Message);
                return ("getFile error: "+ioEx);
            }

            UTF8Encoding temp = new UTF8Encoding(true);

            if (remainOnServer == "0")
            {
                System.IO.File.Delete(localPath);
            }

            
            if (fileType == "svg")
            {
                //HttpContext.Response.Headers.Add("Content-Type", "image/svg+xml");
            }
            else
                if (fileType == "svgz")
            {
                HttpContext.Response.Headers.Add("Content-Encoding", "gzip");
                HttpContext.Response.Headers.Add("Content-Type", "image/svg+xml");
            }
            else
                HttpContext.Response.Headers.Add("Content-Type", "text/plain");

            
            return (temp.GetString(bytes));

        }


        // callApiConversion   - main control for conversions to SVG and PDF
        [HttpPost("callApiConversion")]
        public JsonResult callApiConversion([FromBody] object request)
        {

            try
            {


                string myRequest = "";
                
                if (request != null)
                    myRequest = request.ToString();


                Trace.WriteLine("callApiConversion REQUEST:" + myRequest + "XXXX");
                

                //JObject myCADViewerRequestObject = JObject.Parse(myCADViewerRequestString);
                //myCADViewerRequestObject = JObject.Parse(myCADViewerRequestString);


                // context.Response.ContentType = "text/plain";
                // context.Response.AddHeader("Access-Control-Allow-Origin", "*");

                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");

                string fileLocation = _config.GetValue<string>("CADViewer:fileLocation");
                string fileLocationUrl = _config.GetValue<string>("CADViewer:fileLocationUrl");
                string converterLocation = _config.GetValue<string>("CADViewer:converterLocation");
                string ax2020_executable = _config.GetValue<string>("CADViewer:ax2020_executable");
                string licenseLocation = _config.GetValue<string>("CADViewer:licenseLocation");
                string xpathLocation = _config.GetValue<string>("CADViewer:xpathLocation");
                string callbackMethod = _config.GetValue<string>("CADViewer:callbackMethod");
                bool cvjs_debug = _config.GetValue<bool>("CADViewer:cvjs_debug");
    
                string[] myoutput = new String[1];
                string absFilePath = "";

                bool cvjs_svgz_compress = _config.GetValue<bool>("CADViewer:cvjs_svgz_compress");
         
                

                Trace.WriteLine("callApiConversion cvjs_debug:"+cvjs_debug+"  YYY");

                if (cvjs_debug == true)
                {

                    string wwwPath = _env.WebRootPath;
                    string contentPath = _env.ContentRootPath;
                    string path = Path.Combine(_env.WebRootPath, "temp_debug");

                    Trace.WriteLine("callApiConversion path:" + path + "  contentPath:  "+ contentPath+ "  YYY");

                    
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    absFilePath = Path.Combine(path, "callApiConversionLog.txt");

                    Trace.WriteLine("callApiConversion absFilePath:" + absFilePath + "  YYY");

                }


                myRequest = DecodeUrlString(myRequest);


                Trace.WriteLine("callApiConversion myrequest decoded:" + myRequest + "  YYY");

                string contentLocation = myRequest.Substring(myRequest.IndexOf("contentLocation") + 18);
                contentLocation = contentLocation.Substring(0, contentLocation.IndexOf('\"'));

                string parameters = myRequest.Substring((myRequest.IndexOf("parameters") + 12));
                parameters = parameters.Substring(0, parameters.IndexOf(']'));

                string countString = parameters;

                int paramCount = 0;
                while ((countString.IndexOf("paramName") > -1))
                {
                    countString = countString.Substring((countString.IndexOf("paramName") + 11));
                    paramCount++;
                }

                Trace.WriteLine("callApiConversion myrequest paramCount:" + paramCount + "  YYY");


                myoutput[0] = "total parameters: " + paramCount;
                System.IO.File.AppendAllLines(absFilePath, myoutput);


                string[] param_name = new string[paramCount];
                string[] param_value = new string[paramCount];

                paramCount = 0;

                while ((parameters.IndexOf("paramName") > -1))
                {

                    string string1 = parameters.Substring(parameters.IndexOf("paramName") + 12);

                    myoutput[0] = paramCount + "," + string1;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);

                    string1 = string1.Substring(0, string1.IndexOf('\"'));
                    param_name[paramCount] = string1;


                    try
                    {

                        string1 = parameters.Substring(parameters.IndexOf("paramValue") + 13);

                        string1 = string1.Substring(0, string1.IndexOf('\"'));

                        param_value[paramCount] = string1;
                        parameters = parameters.Substring(parameters.IndexOf("paramValue") + 13);

                        //            context.Response.Write("RUN "+paramCount+" name="+ param_name[paramCount] + " value=" + param_value[paramCount]);
                    }
                    catch (Exception e)
                    {

                        Trace.WriteLine("e:" + e);


                        param_value[paramCount] = "";
                    }

                    paramCount++;
                }

                // determine temporary filename
                Random randomGenerator = new Random();
                int randomInt = randomGenerator.Next(1000000);
                string tempFileName = "F" + randomInt;

                string writeTemp = contentLocation;  // already decoded.... !!!!

                string writeFile = fileLocation + tempFileName + "." + writeTemp.Substring(writeTemp.LastIndexOf(".") + 1);

                //context.Response.Write("4 TEST  HELLO!  "+writeFile);

                int localFlag = 0;

                Trace.WriteLine("callApiConversion writeFile:" + writeFile + "  YYY");

                myoutput[0] = "WriteFile: " + writeFile;
                System.IO.File.AppendAllLines(absFilePath, myoutput);



                if (contentLocation.IndexOf("http") == 0)
                {  // URL

                    if (contentLocation.IndexOf(ServerUrl) == 0)    // we are on same server, so OK
                    {
                        contentLocation = ServerLocation + contentLocation.Substring(ServerUrl.Length);
                        localFlag = 1;
                        // System.IO.File.Copy(contentLocation, writeFile, true);
                    }
                    else
                    {
                        using (WebClient wc = new WebClient())
                        {
                            wc.DownloadFile(contentLocation, writeFile);
                        }
                    }

                    string cleaned = "";
                    string pattern = @"[^\u0000-\u0200]+";
                    //            string pattern = @"[^\u0000-\u007F]+";
                    string replacement = " ";

                    Regex rgx = new Regex(pattern);
                    cleaned = rgx.Replace(contentLocation, replacement);

                    string contentNoUnicode = cleaned;

                    myoutput[0] = "XXX " + contentNoUnicode;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);


                    if (contentNoUnicode.Length != contentLocation.Length)
                    {
                        // if Unicode, we cancel the server operation, but copy to local temporart file

                        // everything seems to work removing this
                        //    localFlag = 0;
                        // System.IO.File.Copy(contentLocation, writeFile, true);

                    }



                }
                else
                { // Flat file

                    if (contentLocation.IndexOf("./") > -1)     // a relative path is made absolute based on current folder
                    {

                        string tmpPrintFolder = _env.WebRootPath + "/" + contentLocation.Substring(contentLocation.IndexOf("./") + 2);
 
//                        string tmpPrintFolder = HttpContext.Current.Server.MapPath("~\\" + contentLocation.Substring(contentLocation.IndexOf("./") + 2));

                        contentLocation = tmpPrintFolder;

                        // context.Response.Write("5 TEST  HELLO!  "+contentLocation+"  "+writeFile);
                    }

                    if (contentLocation.IndexOf("../") > -1)     // a relative path is made absolute based on current folder
                    {
//                        string tmpPrintFolder = HttpContext.Current.Server.MapPath("~\\" + contentLocation.Substring(contentLocation.IndexOf("../") + 3));
                        string tmpPrintFolder = _env.WebRootPath + "/" + contentLocation.Substring(contentLocation.IndexOf("../") + 3);

                        contentLocation = tmpPrintFolder;

                        // context.Response.Write("5 TEST  HELLO!  "+contentLocation+"  "+writeFile);
                    }

                    localFlag = 1;


                    string cleaned = "";
                    string pattern = @"[^\u0000-\u0200]+";
                    //            string pattern = @"[^\u0000-\u007F]+";
                    string replacement = " ";

                    Regex rgx = new Regex(pattern);
                    cleaned = rgx.Replace(contentLocation, replacement);

                    string contentNoUnicode = cleaned;

                    myoutput[0] = "YYY " + contentNoUnicode;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);



                    if (contentNoUnicode.Length != contentLocation.Length)
                    {

                        // if Unicode, we cancel the server operation
                        // localFlag = 0;
                        // System.IO.File.Copy(contentLocation, writeFile, true);

                    }

                }


                // new 2019-04-01
                if (localFlag == 1) { writeFile = contentLocation; }


                Boolean linux = false;   // we not need the windows batch mode any more

                // let us build a command line
                //String commandLine = converterLocation+ax2020_executable+" -i="+writeFile+" -o="+ fileLocation + tempFileName + ".svg -f=svg -model";


                // let us determine the output format

                string outputFormat = "svg";

                for (int i = 0; i < paramCount; i++)
                {
                    if (param_name[i].IndexOf("f") == 0 && param_name[i].Length == 1)
                    {
                        outputFormat = param_value[i];

       
                        // 20.05.52a
                        if ((cvjs_svgz_compress == true) && (outputFormat == "svg"))
                        {
                            outputFormat = "svgz";
                            param_value[i] = "svgz";
                        }


                    }
                }



                //	if (cvjs_debug){		
                //		stringContent = outputFormat;
                //		contentInBytes	= stringContent.getBytes();
                //		fileOut.write(contentInBytes);
                //	}



                string[] envp = new String[1];
                //		string[] str_arr = new String[4+paramCount+1];   // include trace
                string[] str_arr = new String[4 + paramCount];

                str_arr[0] = converterLocation + ax2020_executable;

                var fileName = "";
                var pdfpath = "";


                if (!linux)
                {   // !linux

                    //str_arr[1] =  "-i=\""+writeFile+"\"";
                    //str_arr[2] =  "-o=\""+ fileLocation + tempFileName + "."+outputFormat+"\"";

                    str_arr[3] = "\"-lpath=" + licenseLocation + "\"";

                    // we change for batch call
                    str_arr[1] = "\"-i=\"" + writeFile + "\"\"";
                    str_arr[2] = "\"-o=\"" + fileLocation + tempFileName + "." + outputFormat + "\"\"";

                    str_arr[0] = str_arr[0].Replace(@" ", @"%20");
                    str_arr[1] = str_arr[1].Replace(@" ", @"%20");
                    str_arr[2] = str_arr[2].Replace(@" ", @"%20");
                    str_arr[3] = str_arr[3].Replace(@" ", @"%20");

                    if (outputFormat.ToLower().IndexOf("pdf") > -1)
                    {

                        if (contentLocation.LastIndexOf("\\") > -1)
                            fileName = contentLocation.Substring(contentLocation.LastIndexOf("\\") + 1);
                        if (contentLocation.LastIndexOf("/") > -1)
                            fileName = contentLocation.Substring(contentLocation.LastIndexOf("/") + 1);

                        fileName = fileName.Substring(0, fileName.LastIndexOf("."));
                        pdfpath = fileLocation + "pdf\\" + randomInt;
                        Directory.CreateDirectory(pdfpath);

                        str_arr[2] = "\"-o=\"" + pdfpath + "\\" + fileName + "." + outputFormat + "\"\"";
                        //                str_arr[2] =  "-o=\""+ pdfpath +"\\"+ fileName + "." + outputFormat+"\"";
                    }

                }
                else
                {

                }

                for (int i = 0; i < paramCount; i++)
                {

                    if (param_value[i].Length == 0)
                    {
                        str_arr[4 + i] = "-" + param_name[i];

                    }
                    else
                    {
                        // layout names
                        if (param_value[i] is string)
                        {

                            param_value[i] = param_value[i].Replace('+', ' ');  // we change all url decoding  + to " ".  white spaces

                            // layouts=\\u1234    with Unicode Chinese names
                            param_value[i] = param_value[i].Replace(@"\\u", @"\u"); ;  // we change all \\u  to \u

                            //str_arr[4+i] = "-"+param_name[i]+"="+"\""+param_value[i]+"\"";


                            param_value[i] = param_value[i].Replace(@" ", @"%20");  // we change all url decoding  " "  to "%20".  white spaces

                        }

                        // we change for batch call
                        str_arr[4 + i] = "\"-" + param_name[i] + "=\"" + param_value[i] + "\"\"";
                        //               str_arr[4+i] = "\"\"-"+param_name[i]+"="+param_value[i]+"\"\"";

                    }
                }


                int exitCode = -1;



                Trace.WriteLine("callApiConversion before call to conversion: YYY");

                try
                {
                    {

                        string arguments = "";
                        for (int i = 1; i < paramCount + 4; i++)
                        {
                            if (i == 1)
                                arguments = str_arr[i];
                            else
                                arguments = arguments + " " + str_arr[i];
                        }

                        // move all this processing
                        arguments = str_arr[0] + " " + arguments;    //NOTE , no .bat processing

                        myoutput[0] = arguments;
                        System.IO.File.AppendAllLines(absFilePath, myoutput);

                        //context.Response.Write("arguments = " +arguments);
                        if (cvjs_debug == true)
                        {
                            //                    myoutput[0] = str_arr[0]+"  "+arguments;
                            myoutput[0] = "New Arguments:  " + arguments;
                            System.IO.File.AppendAllLines(absFilePath, myoutput);

                            myoutput[0] = "The command:  " + converterLocation + "\\run_ax2020.bat";
                            //                    myoutput[0] = "New The command:  "+str_arr[0]+" "+arguments;
                            System.IO.File.AppendAllLines(absFilePath, myoutput);

                        }

                        exitCode = 0;
                        ProcessStartInfo ProcessInfo;

                        ProcessInfo = new ProcessStartInfo(converterLocation + "\\run_ax2020.bat", arguments);
                        //              ProcessInfo = new ProcessStartInfo(str_arr[0], arguments);

                        ProcessInfo.CreateNoWindow = true;
                        ProcessInfo.UseShellExecute = false;    // false -> true
                        ProcessInfo.WorkingDirectory = converterLocation;
                        // *** Redirect the output ***
                        ProcessInfo.RedirectStandardError = true;
                        ProcessInfo.RedirectStandardOutput = true;

                        Process myProcess;

                        myProcess = Process.Start(ProcessInfo);

                        // *** Read the streams ***
                        string output = myProcess.StandardOutput.ReadToEnd();
                        string error = myProcess.StandardError.ReadToEnd();

                        myProcess.WaitForExit();

                        exitCode = myProcess.ExitCode;

                        myoutput[0] = "output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output);
                        System.IO.File.AppendAllLines(absFilePath, myoutput);

                        myoutput[0] = "error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error);
                        System.IO.File.AppendAllLines(absFilePath, myoutput);

                        myoutput[0] = "ExitCode: " + exitCode.ToString();
                        System.IO.File.AppendAllLines(absFilePath, myoutput);

                        myProcess.Close();


                    }
                }
                catch (Exception e)
                {

                    Trace.WriteLine("callApiConversion e: "+e.Message);
//                    Console.WriteLine(e.Message);
                }


                myoutput[0] = "Conversion done ";
                System.IO.File.AppendAllLines(absFilePath, myoutput);



                while (contentLocation.IndexOf("\\") > -1)
                {
                    contentLocation = contentLocation.Substring(0, contentLocation.IndexOf('\\')) + "/" + contentLocation.Substring(contentLocation.IndexOf('\\') + 1);
                }

                myoutput[0] = "Content location: " + contentLocation + " output format: " + outputFormat + "XX";
                System.IO.File.AppendAllLines(absFilePath, myoutput);


                // compose callback message

                if (outputFormat.ToLower().IndexOf("svg") > -1)  // BOTH svg + svgz   -> return outputFormat !!! note!
                    {

                    string CVJSresponse = "{\"completedAction\":\"svg_creation\",\"errorCode\":\"E" + exitCode + "\",\"converter\":\"AutoXchange AX2017\",\"version\":\"V1.00\",\"userLabel\":\"fromCADViewerJS\",\"contentLocation\":\"" + contentLocation + "\",\"contentResponse\":\"stream\",\"contentStreamData\":\"" + callbackMethod + "?remainOnServer=0&fileTag=" + tempFileName + "&Type=" + outputFormat + "\"}";


                    myoutput[0] = "SVG response: " + CVJSresponse;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);

                    // send callback message and terminate
                    //context.Response.Write(CVJSresponse);


                    Trace.WriteLine("callApiConversion SVG CVJSresponse:"+CVJSresponse+"QQQQQQ");


                    return Json(CVJSresponse);
                }
                else
                {
                    if (outputFormat.ToLower().IndexOf("pdf") > -1)
                    {

                        var pdfpathurl = "pdf/" + randomInt + "/";

                        string CVJSresponse = "{\"completedAction\":\"pdf_creation\",\"errorCode\":\"E" + exitCode + "\",\"converter\":\"AutoXchange AX2019\",\"version\":\"V1.00\",\"userLabel\":\"fromCADViewerJS\",\"contentLocation\":\"" + contentLocation + "\",\"contentResponse\":\"file\",\"contentStreamData\":\"" + fileLocationUrl + pdfpathurl + fileName + "." + outputFormat + "\"}";

                        //                string CVJSresponse = "{\"completedAction\":\"pdf_creation\",\"errorCode\":\"E"+exitCode+"\",\"converter\":\"AutoXchange AX2019\",\"version\":\"V1.00\",\"userLabel\":\"fromCADViewerJS\",\"contentLocation\":\""+contentLocation+"\",\"contentResponse\":\"file\",\"contentStreamData\":\""+fileLocationUrl+tempFileName+"."+outputFormat+"\"}";
                        //				String CVJSresponse = "{\"completedAction\":\"svg_creation\",\"errorCode\":\"E"+exitCode+"\",\"converter\":\"AutoXchange AX2017\",\"version\":\"V1.00\",\"userLabel\":\"fromCADViewerJS\",\"contentLocation\":\""+contentLocation+"\",\"contentResponse\":\"stream\",\"contentStreamData\":\""+callbackMethod+"?remainOnServer=0&fileTag="+tempFileName+"&Type=svg\"}";



                        myoutput[0] = "PDF response: " + CVJSresponse;
                        System.IO.File.AppendAllLines(absFilePath, myoutput);



                        // delete the temp originating file
                        if (localFlag == 0) System.IO.File.Delete(writeFile);


                        // send callback message and terminate
                        //context.Response.Write(CVJSresponse);
                        Trace.WriteLine("callApiConversion PDF CVJSresponse:" + CVJSresponse + "QQQQQQ");
                        return Json(CVJSresponse);

                    }

                }

                return Json("callApiConversion: error");

            }
            catch (Exception Ex)
            {
                return Json("callApiConversion: " + Ex);
            }

        }

        // 7.0.47   // LOADFILE   - loading of custom menu icon, etc.  same content at LoadFilePost  , change to LoadFileGetLog.txt

        [HttpGet("loadfile")]
        public FileContentResult LoadFileGet(string file, string listtype, string loadtype)
        {

            Trace.WriteLine("LoadFileGet:" + file + "XX" + listtype + "YY loadtype" +loadtype + "ZZ");

            // copy content from LoadFilePost

            bool cvjs_debug = false;
            string[] myoutput = new String[1];
            string absFilePath = "";

            try
            {

                Trace.WriteLine("LoadFileGet:" + file + "XXXX" + listtype + "YYYY loadtype" + loadtype);

                string filePath = DecodeUrlString(file);
                filePath = filePath.Trim('/');

                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");


                cvjs_debug = _config.GetValue<bool>("CADViewer:cvjs_debug");

                cvjs_debug = false;


                if (cvjs_debug == true)
                {

                    string wwwPath = _env.WebRootPath;
                    string contentPath = _env.ContentRootPath;
                    string path = Path.Combine(_env.WebRootPath, "temp_debug");


                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    absFilePath = Path.Combine(path, "LoadFileGetLog.txt");
                }



                if (cvjs_debug == true)
                {
                    myoutput[0] = "LoadFile:" + filePath;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);
                }


                /*
                string ServerLocation = AppSettings.Instance.Get<string>("CADViewer:ServerLocation"); 
                string ServerUrl = AppSettings.Instance.Get<string>("CADViewer:ServerUrl"); 
                */

                if (loadtype != null)
                {
                    loadtype = loadtype.Trim('/');
                    if (loadtype.IndexOf("serverfolder") == 0)
                    {

                        if (filePath.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            filePath = ServerLocation + filePath;

                        if (cvjs_debug == true)
                        {
                            myoutput[0] = "loadtype:" + loadtype + "  updated filePath " + filePath;
                            System.IO.File.AppendAllLines(absFilePath, myoutput);
                        }

                    }


                    if (loadtype.IndexOf("menufile") == 0)
                    {

                        if (filePath.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            filePath = ServerLocation + filePath;
                    }


                    Trace.WriteLine("LoadFileGet: after menufile:" + filePath);


                }


                if (filePath.IndexOf(ServerUrl) == 0)
                {

                    filePath = ServerLocation + filePath.Substring(ServerUrl.Length);

                }


                if (cvjs_debug == true)
                {
                    myoutput[0] = "after ServerUrl match check: filePath: " + filePath;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);
                }



                string localPath = "";

                if (System.IO.File.Exists(filePath))
                {
                    localPath = new Uri(filePath).LocalPath;
                }
                else
                {

                    if (cvjs_debug == true)
                    {
                        myoutput[0] = "Error: file does not exist ";
                        System.IO.File.AppendAllLines(absFilePath, myoutput);
                    }

                    // return ("file does not exist");

                    byte[] bytes = Encoding.ASCII.GetBytes("file does not exist");

                    return new FileContentResult(bytes, "image/jpeg");


                }

                using (FileStream fsSource = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                {

                    // Read the source file into a byte array.
                    byte[] bytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;

                    UTF8Encoding temp = new UTF8Encoding(true);

                    //context.Response.Write(temp.GetString(bytes));

                    Trace.WriteLine("LoadFileGet: before return localPath="+ localPath+" temp stringbytes" + temp.GetString(bytes));

                    // do not make Json(temp.GetString(bytes));

                    HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS, PUT, PATCH, DELET");
                    HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "X-Requested-With,content-type");
                    HttpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");



                    var content_type = "text/plain";

                    if (localPath.IndexOf(".rw") != -1)
                        content_type = "text/plain";
                    else
                    if (localPath.IndexOf(".png") != -1)
                        content_type = "image/png";
                    else
                    if (localPath.IndexOf(".jpg") != -1)
                        content_type = "image/jpeg";
                    else
                    if (localPath.IndexOf(".jpeg") != -1)
                        content_type = "image/jpeg";
                    else
                    if (localPath.IndexOf(".gif") != -1)
                        content_type = "image/gif";
                    else
                    if (localPath.IndexOf(".json") != -1)
                        content_type = "application/json";
                    else
                    if (localPath.IndexOf(".svg") != -1)
                        content_type = "image/svg+xml";

//                    return new FileContentResult(temp.GetString(bytes), "image/jpeg");
                    return new FileContentResult(bytes, content_type);

                    //return File(temp.GetString(bytes),"image/svg+xml");
                }

            }
            catch (FileNotFoundException ioEx)
            {


                if (cvjs_debug == true)
                {
                    myoutput[0] = "FileNotFoundException:" + ioEx;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);
                }


                byte[] bytes = Encoding.ASCII.GetBytes("LoadFileGet: " + ioEx);

                return new FileContentResult(bytes, "text/plain");

                //return ("LoadFileGet: " + ioEx);
            }


        }





        // LOADFILE   - loading of content to populate CADViewer interface and other stuff such as redlines
        [HttpPost("Loadfile")]
        public JsonResult LoadFilePost([FromBody] object filelisttypeloadtype)
        {

            bool cvjs_debug = false;
            string[] myoutput = new String[1];
            string absFilePath = "";
            bool myflag = false;

            try
            {

                var myRequest = "";

                if (filelisttypeloadtype != null)
                    myRequest = filelisttypeloadtype.ToString();


                string file = myRequest.Substring(myRequest.IndexOf("file") + 7);
                file = file.Substring(0, file.IndexOf('\"'));

                string listtype = myRequest.Substring(myRequest.IndexOf("listtype") + 11);
                listtype = listtype.Substring(0, listtype.IndexOf('\"'));

                string loadtype = myRequest.Substring(myRequest.IndexOf("loadtype") + 11);
                loadtype = loadtype.Substring(0, loadtype.IndexOf('\"'));


                Trace.WriteLine("LoadFilePost:" + file.ToString() + "XXXX" + listtype.ToString() + "YYYY loadtype" + loadtype.ToString());



                if (myflag)
                    return Json("This is loadfile post response " + file + " " + listtype + " " + loadtype);


                string filePath = DecodeUrlString(file.ToString());
                filePath = filePath.Trim('/');

                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");

            
                cvjs_debug = _config.GetValue<bool>("CADViewer:cvjs_debug");

                cvjs_debug = false;    // force false
   

                if (cvjs_debug == true)
                {

                    string wwwPath = _env.WebRootPath;
                    string contentPath = _env.ContentRootPath;
                    string path = Path.Combine(_env.WebRootPath, "temp_debug");


                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    absFilePath = Path.Combine(path, "LoadFileLog.txt");
                }



                if (cvjs_debug == true)
                {
                    myoutput[0] = "LoadFile:" + filePath;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);
                }

          
                /*
                string ServerLocation = AppSettings.Instance.Get<string>("CADViewer:ServerLocation"); 
                string ServerUrl = AppSettings.Instance.Get<string>("CADViewer:ServerUrl"); 
                */

                if (loadtype != null)
                {
                    var myloadtype = loadtype.ToString().Trim('/');
                    
                    if (myloadtype.IndexOf("serverfolder") == 0)
                    {

                        if (filePath.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            filePath = ServerLocation + filePath;


                            if (cvjs_debug == true)
                            {
                                myoutput[0] = "loadtype:" + myloadtype + "  updated filePath " + filePath;
                                System.IO.File.AppendAllLines(absFilePath, myoutput);
                            }


                    }


                    if (myloadtype.IndexOf("menufile") == 0)
                    {


                        if (filePath.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            filePath = ServerLocation + filePath;

                    }


                    Trace.WriteLine("LoadFile: after menufile:"+filePath );


                }


                if (filePath.IndexOf(ServerUrl) == 0)
                {

                    filePath = ServerLocation + filePath.Substring(ServerUrl.Length);

                }


                if (cvjs_debug == true)
                {
                    myoutput[0] = "after ServerUrl match check: filePath: " + filePath;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);
                }



                string localPath = "";

                if (System.IO.File.Exists(filePath)){       
                    localPath = new Uri(filePath).LocalPath;
                }
                else
                {

                    if (cvjs_debug == true)
                    {
                        myoutput[0] = "Error: file does not exist ";
                        System.IO.File.AppendAllLines(absFilePath, myoutput);
                    }

                    return Json("file does not exist");

                }

                using (FileStream fsSource = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                {

                    // Read the source file into a byte array.
                    byte[] bytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;

                    UTF8Encoding temp = new UTF8Encoding(true);

                    //context.Response.Write(temp.GetString(bytes));

                    return Json(temp.GetString(bytes));
                }

            }
            catch (FileNotFoundException ioEx)
            {


                if (cvjs_debug == true)
                {
                    myoutput[0] = "FileNotFoundException:"+ ioEx;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);
                }


                return Json("LoadFile: "+ioEx);
            }

        }



        [HttpPost("loadredline")]
        public JsonResult LoadRedline([FromBody] string file, string listtype)
        {

            try
            {

                Trace.WriteLine("HELLO  LoadRedline:" + file + "XXXX");


                string filePath = DecodeUrlString(file);
                filePath = filePath.Trim('/');

                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");

                /*
                string ServerLocation = AppSettings.Instance.Get<string>("CADViewer:ServerLocation"); 
                string ServerUrl = AppSettings.Instance.Get<string>("CADViewer:ServerUrl");  
                */

                if (listtype != null)
                {
                    string loadtype = listtype.Trim('/');
                    if (loadtype.IndexOf("serverfolder") == 0)
                    {

                        if (filePath.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            filePath = ServerLocation + filePath;
                    }

                }






                if (filePath.IndexOf(ServerUrl) == 0)
                {

                    filePath = ServerLocation + filePath.Substring(ServerUrl.Length);

                }

                string localPath = "";

                if (System.IO.File.Exists(filePath))
                {
                    localPath = new Uri(filePath).LocalPath;
                }
                else
                {
                    return Json("file does not exist");
                }

                using (FileStream fsSource = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                {

                    // Read the source file into a byte array.
                    byte[] bytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;

                    UTF8Encoding temp = new UTF8Encoding(true);

                    //context.Response.Write(temp.GetString(bytes));

                    return Json(temp.GetString(bytes));
                }

            }
            catch (FileNotFoundException ioEx)
            {
                return Json("LoadFile: " + ioEx);
            }

        }


        // SAVEFILE   - loading of content to populate CADViewer interface and other stuff such as redlines
        [HttpPost("savefile")]
        public JsonResult SaveFile([FromBody] string file, string file_content, string custom_content, string listtype)
        {

            try
            {
              
                string filePath = DecodeUrlString(file);
                filePath = filePath.Trim('/');

                string fileContent = file_content;
                string customContent = custom_content;
                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");


                bool cvjs_debug = _config.GetValue<bool>("CADViewer:cvjs_debug");
                string[] myoutput = new String[1];
                string absFilePath = "";

                if (cvjs_debug == true)
                {

                    string wwwPath = _env.WebRootPath;
                    string contentPath = _env.ContentRootPath;
                    string path = Path.Combine(_env.WebRootPath, "temp_debug");

           
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    absFilePath = Path.Combine(path, "SaveFileLog.txt");
                }

                if (cvjs_debug == true)
                {
                    myoutput[0] = "Save to:"+filePath;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);
                }


                if (listtype != null)
                {
                    string loadtype = listtype.Trim('/');
                    if (loadtype.IndexOf("serverfolder") == 0)
                    {

                        if (filePath.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            filePath = ServerLocation + filePath;
                    }


                    if (cvjs_debug == true)
                    {
                        myoutput[0] = "loadtype:" + loadtype+ "  updated filePath "+filePath;
                        System.IO.File.AppendAllLines(absFilePath, myoutput);
                    }


                }


                if (filePath.IndexOf(ServerUrl) == 0)
                {
                    filePath = ServerLocation + filePath.Substring(ServerUrl.Length);
                }

                string folder = filePath.Substring(0, filePath.LastIndexOf("/"));


                if (cvjs_debug == true)
                {
                    myoutput[0] = "after filePath check:" + filePath;
                    System.IO.File.AppendAllLines(absFilePath, myoutput);
                }



                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string localPath = filePath;

                try
                {
                    localPath = new Uri(filePath).LocalPath;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);


                    if (cvjs_debug == true)
                    {
                        myoutput[0] = "LocalPath Error:" + e.Message;
                        System.IO.File.AppendAllLines(absFilePath, myoutput);
                    }


                }


                try
                {
                    System.IO.File.WriteAllText(localPath, fileContent);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);


                    if (cvjs_debug == true)
                    {
                        myoutput[0] = "WriteAllText Error:" + e.Message;
                        System.IO.File.AppendAllLines(absFilePath, myoutput);
                    }


                }


                return Json("Success");
        
            }
            catch (FileNotFoundException ioEx)
            {
                return Json("SaveFile: " + ioEx);
            }

        }



        [HttpPost("saveredline")]
        public JsonResult SaveRedline([FromBody] object file_content_listtype_custom)
        {

            try
            {
                // 7.0.51
                //string file, string file_content, string custom_content, string listtype
                var myRequest = "";

                if (file_content_listtype_custom != null)
                    myRequest = file_content_listtype_custom.ToString();

                Trace.WriteLine("SaveRedline:" + myRequest + "XXXX");

                string file = myRequest.Substring(myRequest.IndexOf("file") + 7);
                file = file.Substring(0, file.IndexOf('\"'));

                string listtype = myRequest.Substring(myRequest.IndexOf("listtype") + 11);
                listtype = listtype.Substring(0, listtype.IndexOf('\"'));

                string file_content = myRequest.Substring(myRequest.IndexOf("file_content") + 15);
                file_content = file_content.Substring(0, file_content.IndexOf("custom_content") - 3);

                var version7 = false;

                Trace.WriteLine("SaveRedline file_content:" + file_content + "XXXX");


                if (file_content.IndexOf("RedlineObjects") > 0)
                    version7 = true;

                JObject json = null;
                // JObject json = JObject.Parse(file_content);   // Newtonsoft.Json.JsonReaderException: 'Invalid property identifier character: \. Path '', line 1, position 1.'
                version7 = false;   // defer for now


                // JObject json = JsonConvert.SerializeObject(file_content_listtype_custom.file_content);


                string custom_content = myRequest.Substring(myRequest.IndexOf("custom_content") + 17);
                custom_content = custom_content.Substring(0, custom_content.IndexOf('\"'));

                Trace.WriteLine("HELLO  SaveRedline:" + file + "XXXX");

                string filePath = DecodeUrlString(file);
                filePath = filePath.Trim('/');
                string fileContent = file_content;
                string customContent = custom_content;
                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");


                if (listtype != null)
                {
                    string loadtype = listtype.Trim('/');
                    if (loadtype.IndexOf("serverfolder") == 0)
                    {

                        if (filePath.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            filePath = ServerLocation + filePath;
                    }

                }



                if (filePath.IndexOf(ServerUrl) == 0)
                {
                    filePath = ServerLocation + filePath.Substring(ServerUrl.Length);
                }

                string folder = filePath.Substring(0, filePath.LastIndexOf("/"));

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string localPath = filePath;

                try
                {
                    localPath = new Uri(filePath).LocalPath;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


                try
                {

                    if (version7)
                    {
                        var jsonString = JsonConvert.SerializeObject(json, _options);
                        System.IO.File.WriteAllText(localPath, jsonString);

                    }
                    else
                        System.IO.File.WriteAllText(localPath, fileContent);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


                return Json("Success");

            }
            catch (FileNotFoundException ioEx)
            {
                return Json("SaveFile: " + ioEx);
            }

        }



        [HttpPost("appendfile")]
        public JsonResult AppendFile([FromBody] string file, string file_content, string custom_content, string listtype)
        {

            try
            {
                Trace.WriteLine("HELLO  AppenFile:" + file + "XXXX");

                string filePath = DecodeUrlString(file);
                filePath = filePath.Trim('/');
                string fileContent = file_content;
                string customContent = custom_content;
                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");


                if (listtype != null)
                {
                    string loadtype = listtype.Trim('/');
                    if (loadtype.IndexOf("serverfolder") == 0)
                    {

                        if (filePath.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            filePath = ServerLocation + filePath;
                    }

                }



                if (filePath.IndexOf(ServerUrl) == 0)
                {
                    filePath = ServerLocation + filePath.Substring(ServerUrl.Length);
                }

                string folder = filePath.Substring(0, filePath.LastIndexOf("/"));

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string localPath = filePath;

                try
                {
                    localPath = new Uri(filePath).LocalPath;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


                try
                {
                    System.IO.File.AppendAllText(localPath, fileContent);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


                return Json("Success");

            }
            catch (FileNotFoundException ioEx)
            {
                return Json("SaveFile: " + ioEx);
            }

        }


        [HttpPost("ReturnPDFParams")]
        public JsonResult ReturnPDFParams()
        {

            string fileLocation = _config.GetValue<string>("CADViewer:fileLocation");
            string fileLocationUrl = _config.GetValue<string>("CADViewer:fileLocationUrl");

            string returnString = fileLocation + "|" + fileLocationUrl;

            return Json(returnString);
        }



        [HttpPost("ListDirectoryContent")]
        public JsonResult ListDirectoryContent([FromBody] object directorylisttype)
        {


            try {
                // 7.0.50

                var myRequest = "";


                if (directorylisttype != null)
                    myRequest = directorylisttype.ToString();


                string directory = myRequest.Substring(myRequest.IndexOf("directory") + 12);
                directory = directory.Substring(0, directory.IndexOf('\"'));

                string listtype = myRequest.Substring(myRequest.IndexOf("listtype") + 11);
                listtype = listtype.Substring(0, listtype.IndexOf('\"'));



                string filePath = directory.Trim('/');
  
                string returnString = filePath;


                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");


                if (listtype != null)
                {
                    string loadtype = listtype.Trim('/');
                    if (loadtype.IndexOf("serverfolder") == 0)
                    {

                        if (filePath.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            filePath = ServerLocation + filePath;
                    }

                }



                if (filePath.IndexOf(ServerUrl) == 0)
                {
                    filePath = ServerLocation + filePath.Substring(ServerUrl.Length);
                }



                string[] fileArray = Directory.GetFiles(filePath);

                if (fileArray.Length == 0)
                {
                    returnString = returnString + "The directory is empty";
                }
                else
                {
                    for (var i = 0; i < fileArray.Length; i++)
                    {

                        if (!(fileArray[i].IndexOf(".rw") > 0))
                            returnString = returnString + "<br>" + fileArray[i].Substring(fileArray[i].LastIndexOf("\\") + 1); ;
                    }
                }

                return Json(returnString);



            }
            catch (Exception Ex)
            {
                return Json("ListDirectoryContent: "+Ex);
            }






        }

        

        private static string DecodeUrlString(string url)
        {
            string newUrl;
            while ((newUrl = Uri.UnescapeDataString(url)) != url)
                url = newUrl;
            return newUrl;
        }




        [HttpPost("MakeSinglepagePDF")]
        public JsonResult MakeSinglepagePDF([FromBody] string fileName_0, string rotation_0, string page_format_0)
        {

            Trace.WriteLine("MakeSinglePage REQUEST:" + fileName_0+ " "+ rotation_0 +" "+ page_format_0+ "XXXX");
            try
            {

                string fileName = fileName_0;
                string rotation = rotation_0;
                string pageformat = page_format_0;

                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");

                string fileLocation = _config.GetValue<string>("CADViewer:fileLocation");
                string fileLocationUrl = _config.GetValue<string>("CADViewer:fileLocationUrl");
                string converterLocation = _config.GetValue<string>("CADViewer:converterLocation");
                string ax2020_executable = _config.GetValue<string>("CADViewer:ax2020_executable");
                string licenseLocation = _config.GetValue<string>("CADViewer:licenseLocation");
                string xpathLocation = _config.GetValue<string>("CADViewer:xpathLocation");
                string callbackMethod = _config.GetValue<string>("CADViewer:callbackMethod");
                bool cvjs_debug = _config.GetValue<bool>("CADViewer:cvjs_debug");

                string[] myoutput = new String[1];
                string absFilePath = "";


                if (cvjs_debug == true)
                {     
                    string wwwPath = _env.WebRootPath;
                    string contentPath = _env.ContentRootPath;
                    string path = Path.Combine(_env.WebRootPath, "temp_debug");
                    Trace.WriteLine("makesinglepagepdf path:" + path + "  contentPath:  " + contentPath + "  YYY");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    absFilePath = Path.Combine(path, "MakeSinglePagePDF.txt");
                    Trace.WriteLine("callApiConversion absFilePath:" + absFilePath + "  YYY");
                }

                String base64_file = fileLocation + "/" + fileName + "_base64.png";

                String base64_content = System.IO.File.ReadAllText(base64_file);
                //strip out bas64


                //        myoutput[0] = " base64: "+base64_content+" XXX ";
                //        File.AppendAllLines(absFilePath, myoutput);


                //string imageResult = Regex.Replace(base64_content, "^data:image\/[a-zA-Z]+;base64,", string.empty);

                String[] substrings = base64_content.Split(',');

                string header = substrings[0];
                string imgData = substrings[1];


                myoutput[0] = " header: " + header + " XXX " + ServerLocation + fileName;
                System.IO.File.AppendAllLines(absFilePath, myoutput);

                byte[] bytes = Convert.FromBase64String(imgData);

                // save image
                System.IO.File.WriteAllBytes(fileLocation + fileName + ".png", bytes);

                // convert image
                String arguments = "-i=" + fileLocation + fileName + ".png -o=" + fileLocation + fileName + ".pdf -f=pdf -model -" + rotation + " -" + pageformat;

                myoutput[0] = " arguments: " + arguments;
                System.IO.File.AppendAllLines(absFilePath, myoutput);


                myoutput[0] = " arguments: " + arguments + " XXX ";
                System.IO.File.AppendAllLines(absFilePath, myoutput);


                ProcessStartInfo ProcessInfo;

                ProcessInfo = new ProcessStartInfo(converterLocation + ax2020_executable, arguments);
                ProcessInfo.CreateNoWindow = true;
                ProcessInfo.UseShellExecute = false;
                ProcessInfo.WorkingDirectory = converterLocation;
                // *** Redirect the output ***
                ProcessInfo.RedirectStandardError = true;
                ProcessInfo.RedirectStandardOutput = true;

                Process myProcess;

                myProcess = Process.Start(ProcessInfo);

                // *** Read the streams ***
                string output = myProcess.StandardOutput.ReadToEnd();
                string error = myProcess.StandardError.ReadToEnd();

                myProcess.WaitForExit();

                int exitCode = myProcess.ExitCode;

                myoutput[0] = "output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output);
                System.IO.File.AppendAllLines(absFilePath, myoutput);

                myoutput[0] = "error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error);
                System.IO.File.AppendAllLines(absFilePath, myoutput);

                myoutput[0] = "ExitCode: " + exitCode.ToString();
                System.IO.File.AppendAllLines(absFilePath, myoutput);

                myProcess.Close();

                return Json(fileName + ".pdf");
            }
            catch (Exception e)
            {

                return Json("error:" + e);

            }

        }


    
        [HttpPost("MergeEmail")]
        public JsonResult MergeEmail([FromBody] string pdf_file, string pdf_file_name, string from_name, string from_mail, string cc_mail, string replyto, string to_mail, string mail_title, string listtype, string mail_message)
        {

            try
            {
                string MailServer = _config.GetValue<string>("CADViewer:MailServer");
                int MailServerPort = _config.GetValue<int>("CADViewer:MailServerPort");
                string MailUserName = _config.GetValue<string>("CADViewer:MailUserName");
                string MailPassword = _config.GetValue<string>("CADViewer:MailPassword");


                string ServerLocation = _config.GetValue<string>("CADViewer:ServerLocation");
                string ServerUrl = _config.GetValue<string>("CADViewer:ServerUrl");


                string ccmail = cc_mail;
                string thisreplyto = replyto;


                if (listtype != null)
                {
                    string loadtype = listtype.Trim('/');
                    if (loadtype.IndexOf("serverfolder") == 0)
                    {

                        if (pdf_file.IndexOf(ServerUrl) == 0)
                        {
                            //do nothing!! - handle below
                        }
                        else
                            pdf_file = ServerLocation + pdf_file;
                    }

                }



                string localPath = new Uri(pdf_file).LocalPath;
                byte[] bytes = new byte[1];  // dummy declaration

                try
                {
                    using (FileStream fsSource = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                    {
                        bytes = new byte[fsSource.Length];
                        // Read the source file into a byte array.
                        int numBytesToRead = (int)fsSource.Length;
                        int numBytesRead = 0;
                        while (numBytesToRead > 0)
                        {
                            // Read may return anything from 0 to numBytesToRead.
                            int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                            // Break when the end of the file is reached.
                            if (n == 0)
                                break;

                            numBytesRead += n;
                            numBytesToRead -= n;
                        }
                    }
                }
                catch (FileNotFoundException ioEx)
                {
                    return Json("failed to send email with the following error:" + ioEx);

                }

                // new toolkit   NuGet MailKit

                MimeMessage message = new MimeMessage();

                MailboxAddress from = new MailboxAddress(from_name, from_mail);
                message.From.Add(from);

                MailboxAddress to = new MailboxAddress(to_mail, to_mail);
                message.To.Add(to);

                message.Subject = mail_title;

                BodyBuilder bodyBuilder = new BodyBuilder();
                //bodyBuilder.HtmlBody = "<h1>Hello World!</h1>";
                bodyBuilder.TextBody = mail_message;
            
                var filename = pdf_file.Substring(pdf_file.LastIndexOf("/") + 1);

            
                bodyBuilder.Attachments.Add(filename, bytes);
                message.Body = bodyBuilder.ToMessageBody();

                MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect(MailServer, MailServerPort, true);
                client.Authenticate(MailUserName, MailPassword);

                client.Send(message);
                client.Disconnect(true);
                client.Dispose();

                return Json("email was sent successfully!");


                /*

                                SmtpMail oMail = new SmtpMail("TryIt");

                                // Set sender email address, please change it to yours
                                oMail.From = from_mail;
                                // Set recipient email address, please change it to yours
                                oMail.To = to_mail;

                                // Set email subject
                                oMail.Subject = mail_title;
                                // Set Html body
                                oMail.HtmlBody = mail_message;

                                // return Json("pdf_file:" + pdf_file);
                                // Add attachment from local disk
                                oMail.AddAttachment(pdf_file_name, bytes);

                                // Add attachment from local disk
                                //oMail.AddAttachment(pdf_file);


                                // Your SMTP server address
                                SmtpServer oServer = new SmtpServer(MailServer);

                                // User and password for ESMTP authentication
                                oServer.User = MailUserName;
                                oServer.Password = MailPassword;

                                // Most mordern SMTP servers require SSL/TLS connection now.
                                // ConnectTryTLS means if server supports SSL/TLS, SSL/TLS will be used automatically.
                                oServer.ConnectType = SmtpConnectType.ConnectTryTLS;

                                // If your SMTP server uses 587 port
                                // oServer.Port = 587;

                                // If your SMTP server requires SSL/TLS connection on 25/587/465 port
                                oServer.Port = MailServerPort;                          // 25 or 587 or 465
                                oServer.ConnectType = SmtpConnectType.ConnectSSLAuto;

                                //Console.WriteLine("start to send email with attachment ...");

                                SmtpClient oSmtp = new SmtpClient();
                                oSmtp.SendMail(oServer, oMail);

                                return Json("email was sent successfully!");
                 */

            }
            catch (Exception ep)
            {
                return Json("failed to send email with the following error:" + ep.Message);
            }

        }

        public string GetUniqID()
        {
            var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            double t = ts.TotalMilliseconds / 1000;

            int a = (int)Math.Floor(t);
            int b = (int)((t - Math.Floor(t)) * 1000000);

            return a.ToString("x8") + b.ToString("x5");
        }



        public string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        // NOTE-NOTE-NOTE      AUTOGENERATED CONTENT BELOW

        // GET: CADViewer
        public ActionResult Index()
        {
            return View();
        }

        // GET: CADViewer/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CADViewer/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CADViewer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: CADViewer/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: CADViewer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: CADViewer/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: CADViewer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

    
}



}
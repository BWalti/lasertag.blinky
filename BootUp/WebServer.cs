using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using nanoFramework.Runtime.Native;

namespace BootUp
{
    public class WebServer
    {
        private HttpListener _listener;
        private Thread _serverThread;

        public void Start()
        {
            if (_listener == null)
            {
                _listener = new HttpListener("http");
                _serverThread = new Thread(RunServer);
                _serverThread.Start();
            }
        }

        public void Stop()
        {
            if (_listener != null)
                _listener.Stop();
        }

        private void RunServer()
        {
            _listener.Start();

            while (_listener.IsListening)
            {
                var context = _listener.GetContext();
                if (context != null)
                    ProcessRequest(context);
            }

            _listener.Close();

            _listener = null;
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string responseString;
            string ssid = null;
            string password = null;
            var isApSet = false;

            switch (request.HttpMethod)
            {
                case "GET":
                    var url = request.RawUrl.Split('?');
                    if (url[0] == "/favicon.ico")
                    {
                        response.ContentType = "image/png";
                        var responseBytes = Resources.GetBytes(Resources.BinaryResources.favicon);
                        OutPutByteResponse(response, responseBytes);
                    }
                    else
                    {
                        response.ContentType = "text/html";
                        responseString = ReplaceMessage(Resources.GetString(Resources.StringResources.main), "");
                        OutPutResponse(response, responseString);
                    }

                    break;

                case "POST":
                    // Pick up POST parameters from Input Stream
                    var hashPars = ParseParamsFromStream(request.InputStream);
                    ssid = (string)hashPars["ssid"];
                    password = (string)hashPars["password"];

                    Debug.WriteLine($"Wireless parameters SSID:{ssid} PASSWORD:{password}");

                    var message = "<p>New settings saved.</p><p>Rebooting device to put into normal mode</p>";

                    responseString = CreateMainPage(message);

                    OutPutResponse(response, responseString);
                    isApSet = true;
                    break;
            }

            response.Close();

            if (isApSet && !string.IsNullOrEmpty(ssid) && !string.IsNullOrEmpty(password))
            {
                // Enable the Wireless station interface
                Wireless80211.Configure(ssid, password);

                // Disable the Soft AP
                WirelessAp.Disable();
                Thread.Sleep(200);
                Power.RebootDevice();
            }
        }

        private static string ReplaceMessage(string page, string message)
        {
            var index = page.IndexOf("{message}");
            if (index >= 0) return page.Substring(0, index) + message + page.Substring(index + 9);

            return page;
        }

        private static void OutPutResponse(HttpListenerResponse response, string responseString)
        {
            var responseBytes = Encoding.UTF8.GetBytes(responseString);
            OutPutByteResponse(response, responseBytes);
        }

        private static void OutPutByteResponse(HttpListenerResponse response, byte[] responseBytes)
        {
            response.ContentLength64 = responseBytes!.Length;
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        }

        private static Hashtable ParseParamsFromStream(Stream inputStream)
        {
            var buffer = new byte[inputStream.Length];
            inputStream.Read(buffer, 0, (int)inputStream.Length);

            return ParseParams(Encoding.UTF8.GetString(buffer, 0, buffer.Length));
        }

        private static Hashtable ParseParams(string rawParams)
        {
            var hash = new Hashtable();

            var parPairs = rawParams.Split('&');
            foreach (var pair in parPairs)
            {
                var nameValue = pair.Split('=');
                hash.Add(nameValue[0], nameValue[1]);
            }

            return hash;
        }

        private static string CreateMainPage(string message)
        {
            return $"<!DOCTYPE html><html>{GetCss()}<body>" +
                   "<h1>NanoFramework</h1>" +
                   "<form method='POST'>" +
                   "<fieldset><legend>Wireless configuration</legend>" +
                   "Ssid:</br><input type='input' name='ssid' value='' ></br>" +
                   "Password:</br><input type='password' name='password' value='' >" +
                   "<br><br>" +
                   "<input type='submit' value='Save'>" +
                   "</fieldset>" +
                   "<b>" + message + "</b>" +
                   "</form></body></html>";
        }

        private static string GetCss()
        {
            return
                "<head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><style>" +
                "*{box-sizing: border-box}" +
                "h1,legend {text-align:center;}" +
                "form {max-width: 250px;margin: 10px auto 0 auto;}" +
                "fieldset {border-radius: 5px;box-shadow: 3px 3px 15px hsl(0, 0%, 90%);font-size: large;}" +
                "input {width: 100%;padding: 4px;margin-bottom: 8px;border: 1px solid hsl(0, 0%, 50%);border-radius: 3px;font-size: medium;}" +
                "input[type=submit]:hover {cursor: pointer;background-color: hsl(0, 0%, 90%);transition: 0.5s;}" +
                " @media only screen and (max-width: 768px) { form {max-width: 100%;}} " +
                "</style><title>NanoFramework</title></head>";
        }
    }
}
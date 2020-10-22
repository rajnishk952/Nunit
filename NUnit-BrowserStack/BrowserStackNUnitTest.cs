using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Net;

namespace BrowserStack
{
  [TestFixture]
  public class BrowserStackNUnitTest
  {
    protected IWebDriver driver;
    protected string profile;
    protected string environment;
    private Local browserStackLocal;

    public BrowserStackNUnitTest(string profile, string environment)
    {
      this.profile = profile;
      this.environment = environment;
    }
    
    [SetUp]
    public void Init()
    {
      NameValueCollection caps = ConfigurationManager.GetSection("capabilities/" + profile) as NameValueCollection;
      NameValueCollection settings = ConfigurationManager.GetSection("environments/" + environment) as NameValueCollection;

      DesiredCapabilities capability = new DesiredCapabilities();

      foreach (string key in caps.AllKeys)
      {
        capability.SetCapability(key, caps[key]);
      }

      foreach (string key in settings.AllKeys)
      {
        capability.SetCapability(key, settings[key]);
      }

      String username = Environment.GetEnvironmentVariable("BROWSERSTACK_USERNAME");
      if(username == null)
      {
        username = ConfigurationManager.AppSettings.Get("user");
      }

      String accesskey = Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY");
      if (accesskey == null)
      {
        accesskey = ConfigurationManager.AppSettings.Get("key");
      }

      capability.SetCapability("browserstack.user", username);
      capability.SetCapability("browserstack.key", accesskey);
      Object local_cap = capability.GetCapability("browserstack.local");
            Console.WriteLine("Befor checking local");
            if (local_cap != null && local_cap.ToString().Equals("true"))
            {
                //capability.SetCapability("browserstack.localIdentifier", "DummyTest123");
                //capability.SetCapability("binarypath", "/Users/rajnish/Downloads/BrowserStackLocal");

                
                browserStackLocal = new Local();
                Console.WriteLine("Inside local");

                //Console.WriteLine(browserStackLocal.isRunning().ToString());
                List<KeyValuePair<string, string>> bsLocalArgs = new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("key", accesskey),
                new KeyValuePair<string, string>("binarypath", "path")
                };

                if (!browserStackLocal.isRunning())
                {
                    Console.WriteLine(browserStackLocal.isRunning());
                    browserStackLocal.start(bsLocalArgs);
                    Console.WriteLine(browserStackLocal.isRunning());
                }
             
            }

            driver = new RemoteWebDriver(new Uri("http://"+ ConfigurationManager.AppSettings.Get("server") +"/wd/hub/"), capability);

            //REST API call

            string reqString = "{\"status\":\"passed\", \"reason\":\"Test Passed\"}";
            string sessionId = ((RemoteWebDriver)driver).Capabilities.GetCapability("webdriver.remote.sessionid").ToString();
            byte[] requestData = System.Text.Encoding.UTF8.GetBytes(reqString);
            Uri myUri = new Uri(string.Format($"https://www.browserstack.com/automate/sessions/{sessionId}.json"));
            System.Net.WebRequest myWebRequest = System.Net.HttpWebRequest.Create(myUri);
            System.Net.HttpWebRequest myHttpWebRequest = (System.Net.HttpWebRequest)myWebRequest;
            myWebRequest.ContentType = "application/json";
            myWebRequest.Method = "PUT";
            myWebRequest.ContentLength = requestData.Length;
            using (System.IO.Stream st = myWebRequest.GetRequestStream()) st.Write(requestData, 0, requestData.Length);

            System.Net.NetworkCredential myNetworkCredential = new NetworkCredential("user", "key");
            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(myUri, "Basic", myNetworkCredential);
            myHttpWebRequest.PreAuthenticate = true;
            myHttpWebRequest.Credentials = myCredentialCache;

            myWebRequest.GetResponse().Close();
        }

    [TearDown]
    public void Cleanup()
    {
      driver.Quit();
            if (browserStackLocal != null)
            {
                browserStackLocal.stop();
            }
        }
  }
}

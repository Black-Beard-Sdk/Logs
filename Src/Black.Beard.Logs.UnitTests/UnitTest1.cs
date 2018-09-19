using Bb.Logs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Black.Beard.Logs.UnitTests
{
    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        public void TestLogMethod1()
        {

            string expectedLog = "log_" + Guid.NewGuid().ToString();
            string resultLog = string.Empty;

            void Log4NetTraceListener_Events(object sender, LogEventArg e)
            {
                resultLog = e.Message;
            }

            Log4NetTraceListener.Events += Log4NetTraceListener_Events;

            var log = Log4NetTraceListener.Initialize("log1");

            System.Diagnostics.Trace.WriteLine(expectedLog);

            Log4NetTraceListener.Events -= Log4NetTraceListener_Events;
            log.Dispose();

            Assert.AreEqual(expectedLog, resultLog);

        }


        [TestMethod]
        public void TestLogMethod2()
        {

            string expectedLog = "log_" + Guid.NewGuid().ToString();
            string resultLog = string.Empty;
            object prop1 = string.Empty;

            void Log4NetTraceListener_Events(object sender, LogEventArg e)
            {
                resultLog = e.Message;
                prop1 = e.Properties.FirstOrDefault(c => c.Key == "P1").Value;
            }

            Log4NetTraceListener.Events += Log4NetTraceListener_Events;

            var log = Log4NetTraceListener.Initialize("log1");

            System.Diagnostics.Trace.WriteLine(new { Message = expectedLog, P1 = "pp1" });

            Log4NetTraceListener.Events -= Log4NetTraceListener_Events;
            log.Dispose();

            Assert.AreEqual(expectedLog, resultLog);
            Assert.AreEqual(prop1, "pp1");

        }

        [TestMethod]
        public void TestThrow1()
        {

            var assembliesReferences = new string[] { "Black.Beard.Logs.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.dll", "Microsoft.VisualStudio.TestPlatform.ObjectModel.dll", "Black.Beard.Logs.UnitTests.dll" };
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(c => !c.IsDynamic).Select(c => new FileInfo(c.Location)).ToDictionary(c => c.Name);

            // assembly_content
            void Log4NetTraceListener_Events(object sender, LogEventArg e)
            {

                if (e.Message == "assembly_content")
                {
                    var j = e.Properties.Single(c => c.Key != "session");
                    Assert.AreEqual(true, assembliesReferences.Contains(j.Key));
                    var assembly = assemblies[j.Key];
                }
                else if (e.Message == "Paf")
                {
                    var i = e.Properties.Where(c => c.Key == "Exception").FirstOrDefault();
                    var a = Convert.FromBase64String(i.Value.ToString());

                }
                else if (e.Message == "Log initialized")
                {

                }
                else
                    Assert.Fail("not managed");

            }

            Log4NetTraceListener.Events += Log4NetTraceListener_Events;
            var log = Log4NetTraceListener.Initialize("log1");

            try
            {
                throw new System.Exception("Paf");
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex);
            }
            finally
            {
                Log4NetTraceListener.Events -= Log4NetTraceListener_Events;
                log.Dispose();
            }

        }


        [TestMethod]
        public void TestThrow2()
        {

            void Log4NetTraceListener_Events(object sender, LogEventArg e)
            {

            }

            Log4NetTraceListener.Events += Log4NetTraceListener_Events;
            var log = Log4NetTraceListener.Initialize("log1");

            try
            {
                TestSubThrow();
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex);
                throw;
            }
            finally
            {
                Log4NetTraceListener.Events -= Log4NetTraceListener_Events;
                log.Dispose();
            }

        }


        public void TestSubThrow()
        {
            try
            {
                throw new System.Exception("Paf");
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

    }
}

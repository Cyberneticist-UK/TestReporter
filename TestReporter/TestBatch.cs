using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TestReporter
{
    public delegate void StringDelegate(string data);
    /// <summary>
    /// This Class Can be used to create Test Reports for a batch collection of different test sets. You can write this out into a report in plain text.
    /// For an example of how to use this piece of code, use new TestBatch().GenerateExample(Filename) and it will be created for you!
    /// </summary>
    public class TestBatch
    {
        Hashtable CurrentTests = new Hashtable();
        int CompletedTests = 0;
        string filename = "CompletedTests.txt";
        bool BatchMode = false;
        int TotalTests = 0;

        #region Constructor

        /// <summary>
        /// You can Identify a test by name and access the subtests, start and end it. If you haven't created a test with this name
        /// before, it will generate it for you, otherwise it will 
        /// </summary>
        /// <param name="TestName">The test to access</param>
        /// <returns>The test set with that name</returns>
        public TestSet this[string TestName]
        {
            get
            {
                if (CurrentTests.ContainsKey(TestName) == false)
                    StartTest(TestName);
                return (TestSet)CurrentTests[TestName];
            }
        }
        
        /// <summary>
        /// Blank Constructor. Nothing to see here!
        /// </summary>
        public TestBatch()
        {

        }

        /// <summary>
        /// You can run TestBatch in batch mode, setting up all the named tests at the start, and once all these have run a file
        /// is generated with the results - default is CompletedTests.txt in the current directory
        /// </summary>
        /// <param name="TestBatches">A list of </param>
        /// <param name="filename">The output file</param>
        public TestBatch(string[] TestBatches, string filename = null)
        {
            // If Batchmode is set to true, set up all our tests:
            BatchMode = true;
            if (filename != null)
                this.filename = filename;
            foreach (string Test in TestBatches)
            {
                TestSet newTest = new TestSet()
                {
                    TestName = Test
                };
                newTest.Ended += NewTest_Ended;
                CurrentTests.Add(Test, newTest);
            }
            TotalTests = CurrentTests.Count;
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Write the text report directly into a file.
        /// </summary>
        /// <param name="filename">The file to put the data into. It will automatically overwrite any existing file</param>
        public void WriteToFile(string filename = null)
        {
            StreamWriter sw;
            if (filename != null)
                sw = new StreamWriter(filename);
            else
                sw = new StreamWriter(this.filename);
            string X = ToString();
            sw.Write(X);
            sw.Close();
        }

        /// <summary>
        /// Generate the report into a text format string
        /// </summary>
        /// <returns>The report in text format</returns>
        public override string ToString()
        {
            // First, make sure all the current tests are added to the completed ones:
            StringBuilder sb = new StringBuilder();
            foreach (TestSet item in CurrentTests.Values)
            {
                sb.Append(item.ToString());
            }
            return sb.ToString();
        }

        #endregion

        private bool StartTest(string Test)
        {
            if (CurrentTests.ContainsKey(Test) == false)
            {
                TestSet newTest = new TestSet()
                {
                    TestName = Test
                };
                newTest.Ended += NewTest_Ended;
                newTest.Start();
                CurrentTests.Add(Test, newTest);
                return true;
            }
            // Cannot add a test when there is one with the same name in progress!
            return false;
        }

        private void NewTest_Ended(string Test)
        {
            if (CurrentTests.ContainsKey(Test) == true)
            {
                CompletedTests++;
                //CurrentTests.Remove(Test);
            }
            CheckAllRun();
        }

        private bool EndTest(string Test)
        {
            if (CurrentTests.ContainsKey(Test) == true)
            {
                ((TestSet)CurrentTests[Test]).End();
                CompletedTests++;
                //CurrentTests.Remove(Test);
                CheckAllRun();
                return true;
            }
            // Cannot end a test when there isn't one!
            return false;
        }

        private void CheckAllRun()
        {
            // If all the set up tests have now been completed, and in batchmode, write out the file!
            if (BatchMode == true && CompletedTests == TotalTests)
            {
                WriteToFile();
            }
        }

        /// <summary>
        /// A Test set is a single set of subtests that are grouped together with a single name.
        /// When you start a test run, All tests are still run no matter if one fails or not.
        /// </summary>
        public class TestSet
        {
            public string TestName;
            public DateTime TestStart;
            public DateTime TestEnd;
            private List<TestSubItem> SubTests = new List<TestSubItem>();
            public event StringDelegate Ended;

            /// <summary>
            /// Log the date and time the test is starting
            /// </summary>
            public void Start()
            {
                TestStart = System.DateTime.Now;
            }

            /// <summary>
            /// Log the date and time the test finishes
            /// </summary>
            public void End()
            {
                TestEnd = System.DateTime.Now;
                Ended?.Invoke(TestName);
            }

            /// <summary>
            /// See if two items are equal or not.
            /// </summary>
            /// <param name="Description">What the test is for</param>
            /// <param name="Expected">What you are expecting the value to be</param>
            /// <param name="Actual">The value being tested</param>
            /// <param name="ErrorMessage">an optional error message added to the report if it fails</param>
            /// <returns>True if equal, false if not</returns>
            public bool AssertEqual(string Description, object Expected, object Actual, string ErrorMessage = null)
            {
                if (TestStart == null)
                    TestStart = System.DateTime.Now;
                if ((Expected == null && Actual == null) || (Expected != null && Expected.Equals(Actual)))
                {
                    string res = "";
                    if (Expected == null && Actual == null)
                        res = "Both were null";
                    else
                        res = Expected.ToString() + " equals " + Actual.ToString();
                    SubTests.Add(new TestSubItem()
                    {
                        Commenced = System.DateTime.Now,
                        TestNumber = SubTests.Count + 1,
                        Description = Description,
                        Result = res,
                        Passed = true
                    });
                    return true;
                }
                else
                {
                    string res = "";
                    if (Expected != null)
                    { res = Expected.ToString(); }
                    else
                    { res = "null"; }

                    res += " did not equal ";
                    if (Actual != null)
                    { res += Actual.ToString(); }
                    else
                    { res += "null"; }
                    if (ErrorMessage != null)
                        res += " Error: " + ErrorMessage;
                    SubTests.Add(new TestSubItem()
                    {
                        Commenced = System.DateTime.Now,
                        TestNumber = SubTests.Count + 1,
                        Description = Description,
                        Result = res,
                        Passed = false
                    });
                    return false;
                }
            }

            /// <summary>
            /// See if two items are not equal.
            /// </summary>
            /// <param name="Description">What the test is for</param>
            /// <param name="Expected">What you are expecting the value to be</param>
            /// <param name="Actual">The value being tested</param>
            /// <param name="ErrorMessage">an optional error message added to the report if it fails</param>
            /// <returns>True if not equal, false if equal</returns>
            public bool AssertNotEqual(string Description, object Expected, object Actual, string ErrorMessage = null)
            {
                if (TestStart == null)
                    TestStart = System.DateTime.Now;
                if ((Expected == null && Actual == null) || (Expected != null && Expected.Equals(Actual)))
                {
                    string res = "";
                    if (Expected == null && Actual == null)
                        res = "Both were null";
                    else
                        res = Expected.ToString() + " equals " + Actual.ToString();
                    if (ErrorMessage != null)
                        res += " Error: " + ErrorMessage;
                    SubTests.Add(new TestSubItem()
                    {
                        Commenced = System.DateTime.Now,
                        TestNumber = SubTests.Count + 1,
                        Description = Description,
                        Result = res,
                        Passed = false
                    });
                    return false;
                }
                else
                {
                    string res = "";
                    if (Expected != null)
                    { res = Expected.ToString(); }
                    else
                    { res = "null"; }

                    res += " did not equal ";
                    if (Actual != null)
                    { res += Actual.ToString(); }
                    else
                    { res += "null"; }

                    SubTests.Add(new TestSubItem()
                    {
                        Commenced = System.DateTime.Now,
                        TestNumber = SubTests.Count + 1,
                        Description = Description,
                        Result = res,
                        Passed = true
                    });
                    return true;
                }
            }

            /// <summary>
            /// A report on the test set that has run
            /// </summary>
            /// <returns>The report in text format</returns>
            public override string ToString()
            {
                string Start = "********************************************************************************\r\n" +
                    "Test Name:\t" + TestName + " \r\n" +
                    "Test Started:\t" + TestStart.ToString("dd-MM-yyyy HH:mm:ss.fff") + " \r\n" +
                    "Test Completed:\t" + TestEnd.ToString("dd-MM-yyyy HH:mm:ss.fff") + " \r\n" +
                    "Total (ms):\t" + new TimeSpan(TestEnd.Ticks - TestStart.Ticks).TotalMilliseconds.ToString() + " \r\n";

                string Passed = "";
                string Failed = "";
                foreach (TestSubItem a in SubTests)
                {
                    if (a.Passed)
                        Passed += a.TestNumber + "\t" + a.Commenced.ToString("dd-MM-yyyy HH:mm:ss.fff") + "\t" + a.Description + " (" + a.Result + ")\r\n";
                    else
                        Failed += a.TestNumber + "\t" + a.Commenced.ToString("dd-MM-yyyy HH:mm:ss.fff") + "\t" + a.Description + " (" + a.Result + ")\r\n";
                }

                if (Failed != "")
                {
                    Start += "Overall:\tFAILED\r\n\r\n";
                    Start += "Tests that Failed:\r\n";
                    Start += Failed + "\r\n";
                    Start += "Tests that Passed:\r\n";
                    Start += Passed + "\r\n";
                }
                else
                {
                    Start += "Overall:\tPASSED\r\n\r\n";
                    Start += Passed + "\r\n";
                }

                return Start;
            }

            private struct TestSubItem
            {
                public int TestNumber;
                public bool Passed;
                public DateTime Commenced;
                public string Description;
                public string Result;
            }
        }

        /// <summary>
        /// Shows an example of this system in action in a Test class with a single test method that calls multiple others!
        /// </summary>
        /// <param name="Filename">The file to save the example in</param>
        public void GenerateExample(string Filename)
        {
            StreamWriter sw = new StreamWriter(Filename);
            sw.Write(
            "    [TestClass]\r\n" +
            "    public class NodeContentTest\r\n" +
            "    {\r\n" +
            "	TestBatch TestSystem = new TestBatch(new string[] { \"Test1\", \"Test2\" }, \"MyTests.txt\");\r\n" +
            "\r\n" +
            "        [TestMethod]\r\n" +
            "        public void AllTests()\r\n" +
            "        {\r\n" +
            "            TestMethod1();\r\n" +
            "            TestMethod2();\r\n" +
            "        }\r\n" +
            "\r\n" +
            "        public void TestMethod1()\r\n" +
            "        {\r\n" +
            "            TestSystem[\"Test1\"].Start();\r\n" +
            "            int X = 12;\r\n" +
            "            int Y = X;\r\n" +
            "            TestSystem[\"Test1\"].AssertEqual(\"Check X equals Y\", X, Y, \"Not the same\");\r\n" +
            "            X = 14;\r\n" +
            "            TestSystem[\"Test1\"].AssertNotEqual(\"Check changing X does not change Y\", X, Y);\r\n" +
            "            TestSystem[\"Test1\"].End();\r\n" +
            "        }\r\n" +
            "\r\n" +
            "        public void TestMethod2()\r\n" +
            "        {\r\n" +
            "            TestSystem[\"Test2\"].Start();\r\n" +
            "            string X = \"Hello \";\r\n" +
            "            string Y = \"World\";\r\n" +
            "            TestSystem[\"Test2\"].AssertEqual(\"Check X equals Hello\", X, \"Hello\", \"Not the same\");\r\n" +
            "            X = X+Y;\r\n" +
            "            TestSystem[\"Test2\"].AssertEqual(\"Check Concatenation\", X, \"Hello World\");\r\n" +
            "            TestSystem[\"Test2\"].End();\r\n" +
            "        }\r\n" +
            "}");
            sw.Close();
        }
    }
    
    

    
}

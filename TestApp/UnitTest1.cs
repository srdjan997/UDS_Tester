using Microsoft.VisualStudio.TestTools.UnitTesting;
using UDSonCAN;
using System;
using System.Threading.Tasks;
using System.Threading;


namespace TestApp
{

    [TestClass]
    public class UnitTest1
    {
        enum Session : int
        {
            DEFAULT_SESSION = 0X01,
            PROGRAMMING_SESSION = 0X02,
            EXTENDET_SESSION = 0X03,
        }

        public const string READ_TEMPERATURE = "B006";
        public const string READ_VOLTAGE_SUPPLY = "0112";
        public const string READ_DATE_TIME = "010B";
        public const string READ_CPU_TEMPERATURE = "B00A";
        //////////////////////////////////////////////////////////////////////// 
        [TestMethod]
        public void ReadDataById_Test()
        {
            var test = new Program();
            Boolean res2 = test.TestCase3(test,READ_VOLTAGE_SUPPLY);            
            Assert.IsFalse(res2);
        }
        //////////////////////////////////////////////////////////////////////// 
        [TestMethod]
               
        public void ChangeSession_Test()
        {
            var test = new Program();                        
            Boolean res = test.TestCase1(test,(int)Session.DEFAULT_SESSION);            
            Assert.IsTrue(res);            
        }
        //////////////////////////////////////////////////////////////////////// 
        [TestMethod]
        public void Tester_Present_Test()
        {
            var test = new Program();
            Boolean res = test.TestCase4(test);       
            Assert.IsFalse(res);
        }
        //////////////////////////////////////////////////////////////////////// 
        [TestMethod]
        public void EcuReset_Test()
        {                   
            var test = new Program();
            Boolean res = test.TestCase2(test);          
            Assert.IsTrue(res);    
        }
        //////////////////////////////////////////////////////////////////////// 


    }
}

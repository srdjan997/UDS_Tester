using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using vxlapi_NET;
using System.Threading;
using System.Linq;
using System.Timers;
using System.Windows;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;



namespace UDSonCAN
{
    
    public class Program
    {
        /**********************Global Variables*********************************/
        //Timers::
        public System.Timers.Timer P2TIMER;
        public System.Timers.Timer TPRIMER;
        //Driver::
        private static XLDriver UDSDemo = new XLDriver();
        private static string appName = "UDS Client";
        //Driver configuration::
        private static XLClass.xl_driver_config driverConfig = new XLClass.xl_driver_config();
        private static XLClass.xl_ethernet_bus_params bus_Params = new XLClass.xl_ethernet_bus_params();
        XLDefine.XL_Status txStatus;
        XLClass.xl_event_collection xlEventCollection = new XLClass.xl_event_collection(1);
        XLClass.xl_event receivedEvent = new XLClass.xl_event();
        XLDefine.XL_Status xlStatus = XLDefine.XL_Status.XL_SUCCESS;
        // Variables required by XLDriver
        private static XLDefine.XL_HardwareType hwType = XLDefine.XL_HardwareType.XL_HWTYPE_NONE;
        private static uint hwIndex = 0;
        private static uint hwChannel = 0;
        private static int portHandle = -1;
        private static UInt64 accessMask = 0;
        private static UInt64 permissionMask = 0;
        private static UInt64 txMask = 0;
        private static UInt64 rxMask = 0;
        private static int txCi = -1;
        private static int rxCi = -1;
        private static EventWaitHandle xlEvWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, null);
        // RX thread
        public Thread rxThread;
        private static bool blockRxThread = false;
        public Thread LogThread;
        public Thread txThread;
        XLDefine.XL_Status status;
        public int ThreadMutex = 0;
        public string TRACE_DATA;
        public string DEFAULT_SESSION;
        public string READ_DATA_BY_ID;
        public string TESTER_PRESENT;
        //private static uint respid;
        //private static uint reqid;
        //private static uint resaddr;
        //private static uint reqaddr;
        public int FILLER = 0X55;
        public uint REQ_ID = 0X735;
        public uint RES_ID = 0X73D;
        public uint RTrigger = 0;
        public int DIDLOW;
        public int DIDHIGH;
        public int DTCLOW;
        public int DTCHIGH;
        public int DTC;
        delegate void Rxdelegate(string methodname);
        public const int P2 = 5000;
        public const int P2Ext = 5000;
        public const int S3Client = 2000;
        public const int S3Server = 2000;
        public uint TimerRate = 0;
        public Boolean response = false;
        public const string READ_TEMPERATURE = "B006";
        public const string READ_VOLTAGE_SUPPLY = "0112";
        public const string READ_DATE_TIME = "010B";
        public const string READ_CPU_TEMPERATURE = "B00A";
        //int TPLOCK = 0;

        /******************************End of Global variables***************/


        static void Main(string[] args)
        {
            Console.WriteLine("Hello to App UDS on CAN!!");
            Console.WriteLine("\n");
            var test = new Program();
            //////////////////////////////////////////////////////
            Console.WriteLine("\nSESSION TYPE: ");
            test.TestCase1(test,0X01);
            Thread.Sleep(1000);
            Console.WriteLine("\nECU RESET: ");
            //////////////////////////////////////////////////////
            test.TestCase2(test);            
            Console.WriteLine("\nREAD DATA BY ID: ");
            //////////////////////////////////////////////////////
            test.TestCase3(test,READ_VOLTAGE_SUPPLY);
            Thread.Sleep(1000);
            //////////////////////////////////////////////////////
            Console.WriteLine("\nTESTER PRESENT: ");
            test.TestCase4(test);
            Thread.Sleep(1000);
        }

        
        /////////////// Implementation of essential fucntions ////////////////
        
       
        public Boolean TestCase1(Program t1, int sub)
        {
            Boolean ress;          
            t1.INITLOG();
            Thread.Sleep(3000);
            t1.ChangeSession(0x10,sub);
            ress = PrintResult();            
            return ress;
        }

        public Boolean TestCase2(Program t1)
        {
            Boolean ress;
            t1.EcuReset(0x11);            
            ress = PrintResult();
            return ress;
        }

        public Boolean TestCase3(Program t1,string did)
        {
            Boolean ress;
            t1.ReadDataById(0x22,READ_VOLTAGE_SUPPLY);
            ress = PrintResult();
            return ress;
        }

        public Boolean TestCase4(Program t1)
        {
            Boolean ress;
            t1.TesterPresent(0x3E);
            ress = PrintResult();
            return ress;
        }

        public Boolean PrintResult()
        {            
            Thread.Sleep(2000);
            if (response == true)
            {
                Console.WriteLine("Communication with CAN is successfull!! ");                
            }
            else
            {
                Console.WriteLine("Some trouble in communication with CAN !!");                
                
            }
            return response;
        }
        public void PrintFunctionError()
        {
            Console.WriteLine("App is crashed!!");
        }

        public void INITLOG()
        {

            //starting app
            Console.WriteLine("UDS- Vector Client Started \n");
            Console.WriteLine("Vector XL Driver Version: " + typeof(XLDriver).Assembly.GetName().Version + "\n");
            //opening driver
            status = UDSDemo.XL_OpenDriver();
            Console.WriteLine("Opening vector CAN Driver.... \n");
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            status = UDSDemo.XL_GetDriverConfig(ref driverConfig);
            //getting config
            Console.WriteLine("Getting CAN Driver Config: \n");
            Console.WriteLine(status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //getting DLL info
            Console.WriteLine("Getting Vector DLL Version: ");
            Console.WriteLine(UDSDemo.VersionToString(driverConfig.dllVersion) + Environment.NewLine);
            //Getting channels...
            Console.WriteLine("Channels found: " + driverConfig.channelCount + Environment.NewLine);
            for (int i = 0; i < driverConfig.channelCount; i++)
            {
                Console.WriteLine("   Channel Name:" + driverConfig.channel[i].name );
                Console.WriteLine("   Channel Mask:" + driverConfig.channel[i].channelMask );
                Console.WriteLine("   Transceiver Name:" + driverConfig.channel[i].transceiverName );
                Console.WriteLine("   Serial Number:" + driverConfig.channel[i].serialNumber);
                Console.WriteLine("\n\n");
            }

            //Check config
            if ((UDSDemo.XL_GetApplConfig(appName, 0, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN) != XLDefine.XL_Status.XL_SUCCESS) ||
          (UDSDemo.XL_GetApplConfig(appName, 1, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN) != XLDefine.XL_Status.XL_SUCCESS))
            {
                //...create the item with two CAN channels
                UDSDemo.XL_SetApplConfig(appName, 0, XLDefine.XL_HardwareType.XL_HWTYPE_NONE, 0, 0, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
                UDSDemo.XL_SetApplConfig(appName, 1, XLDefine.XL_HardwareType.XL_HWTYPE_NONE, 0, 0, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
                //PrintAssignErrorAndPopupHwConf();
                ThreadMutex = 1;
            }
            // Request the user to assign channels until both CAN1 (Tx) and CAN2 (Rx) are assigned to usable channels
            if (!GetAppChannelAndTestIsOk(0, ref txMask, ref txCi) || !GetAppChannelAndTestIsOk(1, ref rxMask, ref rxCi))
            {
                ThreadMutex = 0;
            }
            //Printing application configuration on log screen
            //PrintConfig();
            //making masks
            accessMask = txMask | rxMask;
            permissionMask = accessMask;
            //opening port
            status = UDSDemo.XL_OpenPort(ref portHandle, appName, accessMask, ref permissionMask, 1024, XLDefine.XL_InterfaceVersion.XL_INTERFACE_VERSION, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
            Console.WriteLine("Open Port  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //chip state checking
            status = UDSDemo.XL_CanRequestChipState(portHandle, accessMask);
            Console.WriteLine("CAN Request Chip state  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //ON Bus
            status = UDSDemo.XL_ActivateChannel(portHandle, accessMask, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN, XLDefine.XL_AC_Flags.XL_ACTIVATE_NONE);
            Console.WriteLine("Activate Channel  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //can ids display
            Console.WriteLine("TESTER REQUEST CAN ID: 0x735" + Environment.NewLine);
            Console.WriteLine("ECU RESPONSE CAN ID: 0x73D" + Environment.NewLine);
            //giving info
            
            //putting notifications on can
            int tempInt = -1;
            status = UDSDemo.XL_SetNotification(portHandle, ref tempInt, 1);
            xlEvWaitHandle.SafeWaitHandle = new SafeWaitHandle(new IntPtr(tempInt), true);
            Console.WriteLine("Set Notification  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();

            if (TimerRate == 1) TimerRate = 0;
            else TimerRate = 20000;
            status = UDSDemo.XL_SetTimerRate(portHandle, TimerRate);
            Console.WriteLine( "setTimer  :" + status + Environment.NewLine);
            //resetting clock
            status = UDSDemo.XL_ResetClock(portHandle);
            Console.WriteLine("Reset Clock  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //TPLOCK = 1;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nDevice Connected " + driverConfig.channel[0].name + " and " + driverConfig.channel[1].name + " active\n");
            Console.ForegroundColor = ConsoleColor.White;
            //starting rx thread
            Console.WriteLine("Starting Receive Thread........" + Environment.NewLine);
            rxThread = new Thread(new ThreadStart(RXHANDLER));
            rxThread.Start();
            Console.WriteLine("Is main thread is alive" +
                            " ? : {0}", rxThread.IsAlive);
        }

        public void CONNECT()
        {
            //TPLOCK = 1;
            REQ_ID = 0X735;
            RES_ID = 0X73D;
            status = UDSDemo.XL_ActivateChannel(portHandle, accessMask, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN, XLDefine.XL_AC_Flags.XL_ACTIVATE_NONE);
            Console.WriteLine("Activate Channel  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" Device Connected " + driverConfig.channel[0].name + " and " + driverConfig.channel[1].name + " active");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("TESTER REQUEST CAN ID: 0x735" + Environment.NewLine);
            Console.WriteLine("ECU RESPONSE CAN ID: 0x73D" + Environment.NewLine);
            Console.WriteLine("Starting Receive Thread........" + Environment.NewLine);
            rxThread = new Thread(new ThreadStart(RXHANDLER));
            rxThread.Start();
        }

        public void DISCONNECT()
        {
            //TPLOCK = 0;
            status = UDSDemo.XL_DeactivateChannel(portHandle, accessMask);
            Console.WriteLine("Deactivate Channel  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Device status: Device disconnected ");
            Console.ForegroundColor = ConsoleColor.White;
            rxThread.Abort(); // CHECK

        }

        public void RXHANDLER()
        {
            //Boolean response = false;
            // Create new object containing received data 
            XLClass.xl_event receivedEvent = new XLClass.xl_event();
            // Result of XL Driver function calls
            XLDefine.XL_Status xlStatus = XLDefine.XL_Status.XL_SUCCESS;
            // Note: this thread will be destroyed by MAIN
            while (true)
            {
                // Wait for hardware events
                if (xlEvWaitHandle.WaitOne(5000))
                {
                    // ...init xlStatus first
                    xlStatus = XLDefine.XL_Status.XL_SUCCESS;
                    // afterwards: while hw queue is not empty...
                    
                    while (xlStatus != XLDefine.XL_Status.XL_ERR_QUEUE_IS_EMPTY)
                    {
                        // ...block RX thread to generate RX-Queue overflows
                        while (blockRxThread) { Thread.Sleep(1000); }
                        // ...receive data from hardware.
                        xlStatus = UDSDemo.XL_Receive(portHandle, ref receivedEvent);
                        //  If receiving succeed....
                        if (xlStatus == XLDefine.XL_Status.XL_SUCCESS)
                        {
                           
                            if ((receivedEvent.flags & XLDefine.XL_MessageFlags.XL_EVENT_FLAG_OVERRUN) != 0)
                            {

                            }
                            // ...and data is a Rx msg...
                            if (receivedEvent.tag == XLDefine.XL_EventTags.XL_RECEIVE_MSG)
                            {
                                if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_OVERRUN) != 0)
                                {

                                }
                                // ...check various flags
                                if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME)
                                    == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME)
                                {

                                    Console.WriteLine("Error frame" + Environment.NewLine);



                                }
                                else if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME)
                                    == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME)
                                {

                                    Console.WriteLine("Remote frame" + Environment.NewLine);

                                }
                                else if ((receivedEvent.tagData.can_Msg.id == RES_ID) && (receivedEvent.chanIndex == 2))
                                {
                                    RTrigger = 1;
                                   
                                    switch (receivedEvent.tagData.can_Msg.data[1])
                                    {
                                        case 0X7F:                                     //for negative responses

                                            Console.WriteLine("Negative Response Received!" + Environment.NewLine);    // works fine

                                            TRACE_DATA = string.Format(" {0:X2} {1} {2:X2} {3:X2} {4:X2} {5:X2} {6:X2} {7:X2} {8:X2} {9:X2} ", receivedEvent.tagData.can_Msg.id, receivedEvent.tagData.can_Msg.dlc,
                                            receivedEvent.tagData.can_Msg.data[0], receivedEvent.tagData.can_Msg.data[1], receivedEvent.tagData.can_Msg.data[2], receivedEvent.tagData.can_Msg.data[3],
                                            receivedEvent.tagData.can_Msg.data[4], receivedEvent.tagData.can_Msg.data[5], receivedEvent.tagData.can_Msg.data[6], receivedEvent.tagData.can_Msg.data[7]);
                                            Console.ForegroundColor = ConsoleColor.DarkRed;
                                            Console.WriteLine("RX: " + xlStatus + Environment.NewLine);
                                            Console.WriteLine("RX Data: " + TRACE_DATA + Environment.NewLine);
                                            Console.ForegroundColor = ConsoleColor.White;
                                            response = false;

                                            break;
                                        default:                                       //for all other responses                      

                                            Console.WriteLine("Response Received, Positive Response" + Environment.NewLine);    // works fine
                                            TRACE_DATA = string.Format(" {0:X2} {1} {2:X2} {3:X2} {4:X2} {5:X2} {6:X2} {7:X2} {8:X2} {9:X2} ", receivedEvent.tagData.can_Msg.id, receivedEvent.tagData.can_Msg.dlc,
                                            receivedEvent.tagData.can_Msg.data[0], receivedEvent.tagData.can_Msg.data[1], receivedEvent.tagData.can_Msg.data[2], receivedEvent.tagData.can_Msg.data[3],
                                            receivedEvent.tagData.can_Msg.data[4], receivedEvent.tagData.can_Msg.data[5], receivedEvent.tagData.can_Msg.data[6], receivedEvent.tagData.can_Msg.data[7]);

                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine("RX: " + xlStatus + Environment.NewLine);
                                            Console.WriteLine("RX Data: " + TRACE_DATA + Environment.NewLine);
                                            Console.ForegroundColor = ConsoleColor.White;
                                            response = true;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
        }
        ////////////////// END OF RXHANDLER //////////////////////////

        ////////////////// START OF TXHANDLER ////////////////////////
        public void ChangeSession(int SESSION, int SUB_FN)
        {
            
            switch (SUB_FN)
            {
                case 0X01:

                    TXBuffFill(REQ_ID, 8, 0X02, 0X10, 0X01, FILLER, FILLER, FILLER, FILLER, FILLER);
                    txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                    TRACER(1, 0);
                    break;

                case 0X02:

                    TXBuffFill(REQ_ID, 8, 0X02, 0X10, 0X02, FILLER, FILLER, FILLER, FILLER, FILLER);
                    txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                    TRACER(1, 0);
                    break;

                case 0X03:

                    TXBuffFill(REQ_ID, 8, 0X02, 0X10, 0X03, FILLER, FILLER, FILLER, FILLER, FILLER);
                    txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                    TRACER(1, 0);
                    break;


            }
            
        }

        public void EcuReset(int SESSION)
        {
            
            if (SESSION == 0X11)
            {
                TXBuffFill(REQ_ID, 8, 0X02, 0X11, 0X02, FILLER, FILLER, FILLER, FILLER, FILLER);
                txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                TRACER(1, 0);
            } else Console.WriteLine("Error occured!!");

            

        }

        public void TesterPresent(int SESSION)
        {
            
            if (SESSION == 0X3E)
            {
                TXBuffFill(REQ_ID, 8, 0X02, 0X3E, 0x00, FILLER, FILLER, FILLER, FILLER, FILLER);
                txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                TRACER(1, 0);
            }
            else Console.WriteLine("Error occured!!");
            Thread.Sleep(1000);
           
        }
            public void ReadDataById(int SESSION, string dids)
            {
                           
                
                if (String.IsNullOrEmpty(dids))
                {
                    Console.WriteLine("DID Cannot be empty!");
                }
                else
                {
                    int DID;

                    DID = Int32.Parse(dids, System.Globalization.NumberStyles.HexNumber);
                    if ((DID <= 0xFFFF) && (DID > 0X0000))
                    {
                        DIDHIGH = (byte)((DID >> 8) & 0XFF);
                        DIDLOW = (byte)(DID & 0XFF);
                        TXBuffFill(REQ_ID, 8, 0X03, 0X22, DIDHIGH, DIDLOW, FILLER, FILLER, FILLER, FILLER);
                        txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                        TRACER(1, 0);
                    }
                    else
                    {
                        Console.WriteLine("DID is not valid!", "DID Error");
                    }
                }
                

            } 


        ////////////////// END OF TXHANDLER //////////////////////////
             

        /////////////////////////////////////////////////////////////////////////
        ///
        public void TXBuffFill(uint id, ushort dlc, int PCI, int SID_RQ, int DATA_A, int DATA_B, int DATA_C, int DATA_D, int DATA_E, int DATA_F)
        {
            xlEventCollection.xlEvent[0].tagData.can_Msg.id = id;
            xlEventCollection.xlEvent[0].tagData.can_Msg.dlc = dlc;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[0] = (byte)PCI;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[1] = (byte)SID_RQ;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[2] = (byte)DATA_A;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[3] = (byte)DATA_B;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[4] = (byte)DATA_C;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[5] = (byte)DATA_D;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[6] = (byte)DATA_E;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[7] = (byte)DATA_F;
            xlEventCollection.xlEvent[0].tag = XLDefine.XL_EventTags.XL_TRANSMIT_MSG;

            TRACE_DATA = string.Format(" {0:X2} {1} {2:X2} {3:X2} {4:X2} {5:X2} {6:X2} {7:X2} {8:X2} {9:X2} ", xlEventCollection.xlEvent[0].tagData.can_Msg.id, xlEventCollection.xlEvent[0].tagData.can_Msg.dlc,
                xlEventCollection.xlEvent[0].tagData.can_Msg.data[0], xlEventCollection.xlEvent[0].tagData.can_Msg.data[1], xlEventCollection.xlEvent[0].tagData.can_Msg.data[2], xlEventCollection.xlEvent[0].tagData.can_Msg.data[3],
                xlEventCollection.xlEvent[0].tagData.can_Msg.data[4], xlEventCollection.xlEvent[0].tagData.can_Msg.data[5], xlEventCollection.xlEvent[0].tagData.can_Msg.data[6], xlEventCollection.xlEvent[0].tagData.can_Msg.data[7]);
        }
        /////////////////////////////////////////////////////////////////////////
        ///

        public void TRACER(int type, int direction)
        {
            switch (direction)    // type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
            {
                case 0:
                    switch (type)
                    {
                        case 1:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("TX: " + txStatus + Environment.NewLine);
                            Console.WriteLine("TX Data: " + TRACE_DATA + Environment.NewLine);
                            break;
                    }
                    break;
                case 1:
                    switch (type)
                    {
                        case 1:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("RX: " + xlStatus + Environment.NewLine);
                            Console.WriteLine("RX Data: " + TRACE_DATA + Environment.NewLine);

                            break;
                        case 2:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("RX: " + Environment.NewLine);//xlStatus + Environment.NewLine;
                            Console.WriteLine("RX Data: " + Environment.NewLine);//TRACE_DATA + Environment.NewLine;
                            break;
                    }
                    break;
            }



        }
        /////////////////////////////////////////////////////////////////////////
        ///
        public bool GetAppChannelAndTestIsOk(uint appChIdx, ref UInt64 chMask, ref int chIdx)
        {
            XLDefine.XL_Status status = UDSDemo.XL_GetApplConfig(appName, appChIdx, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
            {
                Console.WriteLine("XL_Get application Configuration:" + status + Environment.NewLine);
            }

            chMask = UDSDemo.XL_GetChannelMask(hwType, (int)hwIndex, (int)hwChannel);
            chIdx = UDSDemo.XL_GetChannelIndex(hwType, (int)hwIndex, (int)hwChannel);
            if (chIdx < 0 || chIdx >= driverConfig.channelCount)
            {
                // the (hwType, hwIndex, hwChannel) triplet stored in the application configuration does not refer to any available channel.
                return false;
            }

            // test if CAN is available on this channel
            return (driverConfig.channel[chIdx].channelBusCapabilities & XLDefine.XL_BusCapabilities.XL_BUS_ACTIVE_CAP_CAN) != 0;
            /////////////////////////////////////////////////////////////////////////
            ///


        }
    }
}

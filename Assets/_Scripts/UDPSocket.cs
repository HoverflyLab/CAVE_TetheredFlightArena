//https://forum.unity.com/threads/simple-udp-implementation-send-read-via-mono-c.15900/

/*
    -----------------------
    UDP-Receive (send to)
    -----------------------
     > receive
     127.0.0.1 : 8051
   
     send
     nc -u 127.0.0.1 8051
*/

using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NaughtyAttributes;
 
namespace TetheredFlight
{
    //This script receives packets from DeepLabCut-Live using a UDP Socket
    //These Packets are then sent to the DataProcessor Script where the packet is disassembled.
    //This script is also used to switch between Latency and Packet tests. 

    //Packet Test prints to console how many packets were recieved over a 10 second period.
    //Latency Test activates a square in the bottom left corner of display 4 and switches it from Black to White based on DLC-live input.
    //Tests should not be run at the same time or during trials, they should be done independently.
    public class UDPSocket : MonoBehaviour 
    {
        #region Variables
        public static UDPSocket Instance = null;
        private Thread receiveThread = null;
        private UdpClient client;
        private int port; // define > init
        private string lastReceivedUDPPacket="";

        [SerializeField, ReadOnly] private bool Active = false;
        [SerializeField] private bool verbose = false;
        [SerializeField] private bool isPacketTest = false;
        [SerializeField] private bool isLatencyTest = false;
        [SerializeField] private bool isFrameTest = false;

        private DataProcessor dataProcessor = null;
        private float startTime = default;
        private int counter = 0;
        private bool isClosedLoop = false;
        #endregion
    
        private static void Main()
        {
            UDPSocket receiveObj=new UDPSocket();
            receiveObj.init();
    
            string text="";
            do
            {
                text = Console.ReadLine();
            }
            while(!text.Equals("exit"));
        }

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.LogError("Error: There can only be one UDP Socket");
                Destroy(this.gameObject);
            }
            
            //If the Trial Manager exists put it in charge of Activating and Deactivating the socket
            if(TrialManager.Instance != null)
            {
                Active = false;
                TrialManager.Instance.PreStimulusStarted += Become_Active;
                TrialManager.Instance.PostStimulusComplete += Become_Inactive;
                TrialManager.Instance.CloseLoop += CloseLoop;
                TrialManager.Instance.OpenLoop += OpenLoop;
            }
        }

        public void Start()
        {     
            dataProcessor = DataProcessor.Instance;
            init();

            if (isLatencyTest == true)
            {
                if(LatencyTester.Instance != null)
                {
                    LatencyTester.Instance.isLatencyTest = true;
                    LatencyTester.Instance.ActivateCanvas();
                }
                else
                {
                    Debug.LogError("Error: Latency Tester does not exist");
                    isLatencyTest = false;
                }
            }
            else if(isFrameTest == true) 
            {
                if (LatencyTester.Instance != null)
                {
                    LatencyTester.Instance.isLatencyTest = false; //set to frame test
                    LatencyTester.Instance.ActivateCanvas();
                }
                else
                {
                    Debug.LogError("Error: Latency Tester does not exist");
                    isFrameTest = false;
                }
            }
        }
    
        // OnGUI
        void OnGUI()
        {
            if(verbose == true)
            {
                Rect rectObj=new Rect(40,10,200,400);
                GUIStyle style = new GUIStyle();
                    style.alignment = TextAnchor.UpperLeft;
                    GUI.Box(rectObj,"# UDPReceive\n127.0.0.1 "+port+" #\n"
                        + "shell> nc -u 127.0.0.1 : "+port+" \n"
                        + "\nLast Packet: \n"+ lastReceivedUDPPacket
                    ,style);
            }
        }

        private void init()
        {
            port = 8051;

            if (verbose) { print("Creating RecieveData Thread"); }
            receiveThread = new Thread(
            new ThreadStart(Receive_Data));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        void FixedUpdate()
        {
            if(isPacketTest == true && Active == true && isClosedLoop == true)
            {
                if(verbose) { print("Start Frame count"); }
                
                //tests for packets per second
                if(Time.time >= startTime + 10f)
                {
                    print("Packets received per second = " + (counter / 10f));
                    TetheredAnimalAvatarController.Instance.Print_Stats();
                    Become_Inactive();
                } 
            }
        }

        // receive thread
        private void Receive_Data()
        {
            client = new UdpClient(port);
            while (true)
            {
                try
                {
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = client.Receive(ref anyIP);
                    string text = Encoding.UTF8.GetString(data);

                    if(Active)
                    {
                        //count packet and send it through to the dataProcessor
                        counter++;
                        lastReceivedUDPPacket = text;
                        if(verbose) { print("Packet Recieved"); }
                        if(isLatencyTest == true && LatencyTester.Instance != null)
                        {
                            LatencyTester.Instance.Get_Latest_UDP_Packet(lastReceivedUDPPacket);
                        }
                        else if(isFrameTest == true && LatencyTester.Instance != null)
                        {
                            LatencyTester.Instance.Get_Latest_UDP_Packet(lastReceivedUDPPacket);
                        }
                        else
                        {
                            dataProcessor.Get_Latest_UDP_Packet(lastReceivedUDPPacket);
                        }       
                    }
                }
                catch (ThreadAbortException err)
                {
                    print("Thread safely aborted with message : " + err);
                }
            }
        }

        //Called via action in TrialManager
        public void Become_Active()
        {
            if(verbose) { print("Thread Activated"); }
            Active = true;
        }

        //Called via action in TrialManager
        public void Become_Inactive()
        {
            if (verbose) { print("Thread Deactivated"); }
            Active = false;
        }

        //Called via action in TrialManager
        private void CloseLoop()
        {
            if(verbose) { print("Loop Closed"); }
            startTime = Time.time;
            isClosedLoop = true;
        }

        //Called via action in TrialManger
        private void OpenLoop()
        {
            if(verbose) { print("Loop Opened"); }
            isClosedLoop = false;
        }

        public bool isLatencyTestActive()
        {
            return isLatencyTest;
        }

        private void OnDestroy()
        {
            if (receiveThread != null)
            {
                if (verbose) { print("Abort thread"); }
                receiveThread.Abort();
            }

            if (UIManager.Instance != null)
            {
                TrialManager.Instance.PreStimulusStarted -= Become_Active;
                TrialManager.Instance.PostStimulusComplete -= Become_Inactive;
                TrialManager.Instance.CloseLoop -= CloseLoop;
                TrialManager.Instance.OpenLoop -= OpenLoop;
            }
        }
    }
}

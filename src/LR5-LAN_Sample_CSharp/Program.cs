using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LR5_LAN_Sample_CSharp
{
    class Program
    {
        private static Socket sock = null;

        // product category
        public static ushort PNS_PRODUCT_ID = 0x4142;

        // PNS command identifier
        private static readonly byte PNS_RUN_CONTROL_COMMAND = 0x53;        // operation control command
        private static readonly byte PNS_CLEAR_COMMAND = 0x43;              // clear command
        private static readonly byte PNS_GET_DATA_COMMAND = 0x47;           // get status command

        // response data for PNS command
        private static readonly byte PNS_ACK = 0x06;                        // normal response
        private static readonly byte PNS_NAK = 0x15;                        // abnormal response

        // LED unit for motion control command
        private static readonly byte PNS_RUN_CONTROL_LED_OFF = 0x00;         	 // light off
        private static readonly byte PNS_RUN_CONTROL_LED_ON = 0x01;	             // light on
        private static readonly byte PNS_RUN_CONTROL_LED_BLINKING_SLOW = 0x02;	 // blinking(slow)
        private static readonly byte PNS_RUN_CONTROL_LED_BLINKING_MEDIUM = 0x03; // blinking(medium)
        private static readonly byte PNS_RUN_CONTROL_LED_BLINKING_HIGH = 0x04;	 // blinking(high)
        private static readonly byte PNS_RUN_CONTROL_LED_FLASHING_SINGLE = 0x05; // flashing single
        private static readonly byte PNS_RUN_CONTROL_LED_FLASHING_DOUBLE = 0x06; // flashing double
        private static readonly byte PNS_RUN_CONTROL_LED_FLASHING_TRIPLE = 0x07; // flashing triple
        private static readonly byte PNS_RUN_CONTROL_LED_NO_CHANGE = 0x09;   	 // no change

        // buzzer for motion control command
        private static readonly byte PNS_RUN_CONTROL_BUZZER_STOP = 0x00;	 // stop
        private static readonly byte PNS_RUN_CONTROL_BUZZER_RING = 0x01;	 // ring
        private static readonly byte PNS_RUN_CONTROL_BUZZER_NO_CHANGE = 0x09;	 // no change

        // operation control data structure
        public class PNS_RUN_CONTROL_DATA
        {
            // LED Red pattern
            public byte ledRedPattern = 0;

            // LED Amber pattern
            public byte ledAmberPattern = 0;

            // LED Green pattern
            public byte ledGreenPattern = 0;

            // LED Blue pattern
            public byte ledBluePattern = 0;

            // LED White pattern
            public byte ledWhitePattern = 0;

            // buzzer mode
            public byte buzzerMode = 0;
        };

        // status data of operation control
        public class PNS_STATUS_DATA
        {
            // LED Pattern 1 to 5
            public byte[] ledPattern = new byte[5];

            // buzzer mode
            public byte buzzer = 0;
        };


        /// <summary>
        /// Main Function
        /// </summary>
        static void Main()
        {
            int ret;

            // Connect to LR5-LAN
            ret = SocketOpen("192.168.10.1", 10000);
            if (ret == -1)
                return;

            // Get the command identifier specified by the command line argument
            string commandId = "";
            string[] cmds = System.Environment.GetCommandLineArgs();
            if (cmds.Length > 1)
                commandId = cmds[1];

            switch (commandId)
            {
                case "S":
                    {
                        // operation control command
                        if (cmds.Length >= 8)
                        {
                            PNS_RUN_CONTROL_DATA runControlData = new PNS_RUN_CONTROL_DATA()
                            {
                                ledRedPattern = byte.Parse(cmds[2]),
                                ledAmberPattern = byte.Parse(cmds[3]),
                                ledGreenPattern = byte.Parse(cmds[4]),
                                ledBluePattern = byte.Parse(cmds[5]),
                                ledWhitePattern = byte.Parse(cmds[6]),
                                buzzerMode = byte.Parse(cmds[7])
                            };
                            PNS_RunControlCommand(runControlData);
                        }

                        break;
                    }

                case "C":
                    {
                        // clear command
                        PNS_ClearCommand();
                        break;
                    }

                case "G":
                    {
                        // get status command
                        PNS_STATUS_DATA statusData = new PNS_STATUS_DATA();
                        ret = PNS_GetDataCommand(out statusData);
                        if (ret == 0)
                        {
                            // Display acquired data
                            Console.WriteLine("Response data for status acquisition command");
                            // LED Red pattern
                            Console.WriteLine("LED Red pattern : " + statusData.ledPattern[0].ToString());
                            // LED Amber pattern
                            Console.WriteLine("LED Amber pattern : " + statusData.ledPattern[1].ToString());
                            // LED Green pattern
                            Console.WriteLine("LED Green pattern : " + statusData.ledPattern[2].ToString());
                            // LED Blue pattern
                            Console.WriteLine("LED Blue pattern : " + statusData.ledPattern[3].ToString());
                            // LED White pattern
                            Console.WriteLine("LED White pattern : " + statusData.ledPattern[4].ToString());
                            // buzzer mode
                            Console.WriteLine("Buzzer Mode : " + statusData.buzzer.ToString());
                        }

                        break;
                    }

            }

            // Close the socket
            SocketClose();
        }

        /// <summary>
        /// Connect to LR5-LAN
        /// </summary>
        /// <param name="ip">IP address</param>
        /// <param name="port">port number</param>
        /// <returns>success: 0, failure: non-zero</returns>
        public static int SocketOpen(string ip, int port)
        {
            try
            {
                // Set the IP address and port
                IPAddress ipAddress = IPAddress.Parse(ip);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a socket
                sock = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (sock == null)
                {
                    Console.Write("failed to create socket");
                    return -1;
                }

                // Connect to LR5-LAN
                sock.Connect(remoteEP);
            }
            catch(Exception ex)
            {
                Console.Write(ex.Message);
                sock.Close();
                sock.Dispose();
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Close the socket.
        /// </summary>
        public static void SocketClose()
        {
            if (sock != null)
            {
                // Close the socket.
                sock.Shutdown(SocketShutdown.Both);
                sock.Close();
                sock.Dispose();
            }

        }

        /// <summary>
        /// Send command
        /// </summary>
        /// <param name="sendData">send data</param>
        /// <param name="recvData">received data</param>
        /// <returns>success: 0, failure: non-zero</returns>
        public static int SendCommand(byte[] sendData, out byte[] recvData)
        {
            int ret;
            recvData = null;

            try
            {
                if (sock == null)
                {
                    Console.Write("socket is not");
                    return -1;
                }

                // Send
                ret = sock.Send(sendData);
                if (ret < 0)
                {
                    Console.Write("failed to send");
                    return -1;
                }

                // Receive response data
                byte[] bytes = new byte[1024];
                int recvSize = sock.Receive(bytes);
                if (recvSize < 0)
                {
                    Console.Write("failed to recv");
                    return -1;
                }
                recvData = new byte[recvSize];
                Array.Copy(bytes, recvData, recvSize);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Send operation control command for PNS command
        /// Each color of the LED unit and the buzzer can be controlled by the pattern specified in the data area
        /// Operates with the color and buzzer set in the signal light mode
        /// </summary>
        /// <param name="runControlData">
        /// Red/amber/green/blue/white LED unit operation patterns, buzzer mode
        /// Pattern of LED unit  (off: 0, on: 1, blinking(slow): 2, blinking(medium): 3, blinking(high): 4, flashing single: 5, flashing double: 6, flashing triple: 7, no change: 9)
        /// Pattern of buzzer  (stop: 0, ring: 1, no change: 9)
        /// </param>
        /// <returns>success: 0, failure: non-zero</returns>
        public static int PNS_RunControlCommand(PNS_RUN_CONTROL_DATA runControlData)
        {
            int ret;

            try
            {
                byte[] sendData = { };

                // Product Category (AB)
                sendData = sendData.Concat(BitConverter.GetBytes(PNS_PRODUCT_ID).Reverse()).ToArray();

                // Command Identifier(S)
                sendData = sendData.Concat(new byte[] { PNS_RUN_CONTROL_COMMAND }).ToArray();

                // Empty(0)
                sendData = sendData.Concat(new byte[] { 0 }).ToArray();

                // data size, data area
                byte[] data = {
                    runControlData.ledRedPattern,     // LED Red pattern
                    runControlData.ledAmberPattern,     // LED Amber pattern
                    runControlData.ledGreenPattern,     // LED Green pattern
                    runControlData.ledBluePattern,     // LED Blue pattern
                    runControlData.ledWhitePattern,     // LED White pattern
                    runControlData.buzzerMode       // Buzzer mode
                };
                sendData = sendData.Concat(BitConverter.GetBytes((ushort)data.Length).Reverse()).ToArray();
                sendData = sendData.Concat(data).ToArray();

                // Send PNS command
                byte[] recvData;
                ret = SendCommand(sendData, out recvData);
                if (ret != 0)
                {
                    Console.Write("failed to send data");
                    return -1;
                }

                // check the response data
                if (recvData[0] == PNS_NAK)
                {
                    // receive abnormal response
                    Console.Write("negative acknowledge");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Send clear command for PNS command
        /// Turn off the LED unit and stop the buzzer
        /// </summary>
        /// <returns>success: 0, failure: non-zero</returns>
        public static int PNS_ClearCommand()
        {
            int ret;

            try
            {
                byte[] sendData = { };

                // Product Category (AB)
                sendData = sendData.Concat(BitConverter.GetBytes(PNS_PRODUCT_ID).Reverse()).ToArray();

                // Command identifier (C)
                sendData = sendData.Concat(new byte[] { PNS_CLEAR_COMMAND }).ToArray();

                // Empty (0)
                sendData = sendData.Concat(new byte[] { 0 }).ToArray();

                // Data size
                sendData = sendData.Concat(BitConverter.GetBytes((ushort)0)).ToArray();

                // Send PNS command
                byte[] recvData;
                ret = SendCommand(sendData, out recvData);
                if (ret != 0)
                {
                    Console.Write("failed to send data");
                    return -1;
                }

                // check the response data
                if (recvData[0] == PNS_NAK)
                {
                    // receive abnormal response
                    Console.Write("negative acknowledge");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Send status acquisition command for PNS command
        /// LED unit and buzzer status can be acquired
        /// </summary>
        /// <param name="statusData">Received data of status acquisition command (status of LED unit and buzzer)</param>
        /// <returns>Success: 0, failure: non-zero</returns>
        public static int PNS_GetDataCommand(out PNS_STATUS_DATA statusData)
        {
            int ret;
            statusData = new PNS_STATUS_DATA();

            try
            {
                byte[] sendData = { };

                // Product Category (AB)
                sendData = sendData.Concat(BitConverter.GetBytes(PNS_PRODUCT_ID).Reverse()).ToArray();

                // Command identifier (G)
                sendData = sendData.Concat(new byte[] { PNS_GET_DATA_COMMAND }).ToArray();

                // Empty (0)
                sendData = sendData.Concat(new byte[] { 0 }).ToArray();

                // Data size
                sendData = sendData.Concat(BitConverter.GetBytes((short)0).Reverse()).ToArray();

                // Send PNS command
                byte[] recvData;
                ret = SendCommand(sendData, out recvData);
                if (ret != 0)
                {
                    Console.Write("failed to send data");
                    return -1;
                }

                // check the response data
                if (recvData[0] == PNS_NAK)
                {
                    // receive abnormal response
                    Console.Write("negative acknowledge");
                    return -1;
                }

                // LED Pattern 1～5
                statusData.ledPattern = new byte[5];
                Array.Copy(recvData, statusData.ledPattern, statusData.ledPattern.Length);

                // Buzzer Mode
                statusData.buzzer = recvData[5];

            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return -1;
            }

            return 0;
        }

    }
}

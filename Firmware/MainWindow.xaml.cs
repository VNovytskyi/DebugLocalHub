using System;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static EventWaitHandle allowRefreshComPorts = new ManualResetEvent(initialState: true);
        private static EventWaitHandle allowRefreshCurrentComPortState = new ManualResetEvent(initialState: false);

        private static Thread RefreshCurrentComPortState = null;
        private static Thread RefreshComPortsList = null;
        private static Thread ConnectToComPort = null;
        private static Thread TransiverData = null;
        
        private static SerialPort serialPort = null;


        /*
         * Default values for comboBoxes   
        */
        readonly private static string[] baudRateValues = { "9600", "115200" };
        readonly private static string[] dataBitsValues = { "5", "6", "7", "8", "9" };
        readonly private static string[] parityValues = { "None", "Odd", "Even", "Mark", "Space" };
        readonly private static string[] stopBitsValues = { "1", "2", "1.5" };
        readonly private static string[] portCValues = { "Digital Input", "Analog Input" };
        readonly private static string[] portBFrequencyValues = { "Low", "Medium", "High" };

        private static int oldComPortsAmount = -1;

        private static bool firstDisplayPortList = false;

        public MainWindow()
        {
            InitializeComponent();

            InitBaudRateValuesComboBox();
            InitDataBitsValuesComboBox();
            InitParityValuesComboBox();
            InitStopBitsValuesComboBox();
            InitPortCModeValuesComboBox();
            InitportBFrequency();
            
            InitPortAIndicators();
            InitPortBIndicators();
            InitPortCIndicators();

            serialPort = new SerialPort();

            RefreshCurrentComPortState = new Thread(RefreshCurrentComPortStateFunc);
            RefreshCurrentComPortState.Start();

            RefreshComPortsList = new Thread(RefreshComPortsListFunc);
            RefreshComPortsList.Start();
        }


        /*
         * Function for updating state of COM-port
         */
        private void RefreshCurrentComPortStateFunc()
        {
            while(true)
            {
                allowRefreshCurrentComPortState.WaitOne();

                bool portState = true;
                Dispatcher.Invoke(() => portState = serialPort.IsOpen);

                if (!portState)
                    StopConnection();

                Thread.Sleep(100);
            }
        }

        /*
         * 
         */
        private void RefreshComPortsListFunc()
        {
            while(true)
            {
                allowRefreshComPorts.WaitOne();

                string[] portNames = SerialPort.GetPortNames();
                int currentComPortsAmount = portNames.Length;

                if (oldComPortsAmount != currentComPortsAmount)
                {
                    Dispatcher.Invoke(() =>
                    {
                        PortsComboBox.Items.Clear();
                        foreach (string port in portNames)
                            PortsComboBox.Items.Add(port);
                    });

                    if(firstDisplayPortList)
                        firstDisplayPortList = true;

                    if (currentComPortsAmount > 0)
                    {
                        EnableSettings();
                        setDefaultSettings();
                    }
                    else
                    {
                        DisableSettings();
                        setEmptySettings();
                    }

                    if (currentComPortsAmount == 1 && getAutoConnectStatus())
                    {
                        InitConnection();
                    }
                }

                oldComPortsAmount = currentComPortsAmount;

                Thread.Sleep(100);
            }
        }

        private bool getAutoConnectStatus()
        {
            bool a = false;
            Dispatcher.Invoke(() => a = AutoConnectToPortCheckBox.IsChecked == true);
            return a;
        }

        private void EnableSettings()
        {
            Dispatcher.Invoke(() =>
            {
                PortsComboBox.IsEnabled = true;
                BaudRateValuesComboBox.IsEnabled = true;
                DataBitsValuesComboBox.IsEnabled = true;
                ParityValuesComboBox.IsEnabled = true;
                StopBitsValuesComboBox.IsEnabled = true;
                ConnectToComPortButton.IsEnabled = true;
                DisconnectToComPortButton.IsEnabled = false;
            });
        }

        private void setDefaultSettings()
        {
            Dispatcher.Invoke(() =>
            {
                PortsComboBox.SelectedIndex = 0;
                BaudRateValuesComboBox.SelectedItem = "9600";
                DataBitsValuesComboBox.SelectedItem = "8";
                ParityValuesComboBox.SelectedItem = "None";
                StopBitsValuesComboBox.SelectedItem = "1";
            });
        }

        private void DisableSettings()
        {
            Dispatcher.Invoke(() =>
            {
                PortsComboBox.IsEnabled = false;
                BaudRateValuesComboBox.IsEnabled = false;
                DataBitsValuesComboBox.IsEnabled = false;
                ParityValuesComboBox.IsEnabled = false;
                StopBitsValuesComboBox.IsEnabled = false;
                ConnectToComPortButton.IsEnabled = false;
                DisconnectToComPortButton.IsEnabled = true;
                ConnectToComPortButton.IsEnabled = false;
                DisconnectToComPortButton.IsEnabled = false;
            });
        }

        private void setEmptySettings()
        {
            Dispatcher.Invoke(() =>
            {
                BaudRateValuesComboBox.SelectedIndex = -1;
                DataBitsValuesComboBox.SelectedIndex = -1;
                ParityValuesComboBox.SelectedIndex = -1;
                StopBitsValuesComboBox.SelectedIndex = -1;
            });
        }

        private void InitStopBitsValuesComboBox()
        {
            foreach (string stopBitsValue in stopBitsValues)
                StopBitsValuesComboBox.Items.Add(stopBitsValue);

            StopBitsValuesComboBox.SelectedItem = "1";
        }

        private void InitParityValuesComboBox()
        {
            foreach (string parityValue in parityValues)
                ParityValuesComboBox.Items.Add(parityValue);

            ParityValuesComboBox.SelectedItem = "None";
        }

        private void InitDataBitsValuesComboBox()
        {
            foreach (string dataBitsValue in dataBitsValues)
                DataBitsValuesComboBox.Items.Add(dataBitsValue);

            DataBitsValuesComboBox.SelectedItem = "8";
        }

        private void InitBaudRateValuesComboBox()
        { 
             foreach (string baudRateValue in baudRateValues)
                BaudRateValuesComboBox.Items.Add(baudRateValue);

            BaudRateValuesComboBox.SelectedItem = "115200";
        }

        private void InitPortCModeValuesComboBox()
        {
            string str = "None";
            C0_ComboBox.Items.Add(str);
            C1_ComboBox.Items.Add(str);
            C2_ComboBox.Items.Add(str);
            C3_ComboBox.Items.Add(str);
            C4_ComboBox.Items.Add(str);
            C5_ComboBox.Items.Add(str);
            C6_ComboBox.Items.Add(str);
            C7_ComboBox.Items.Add(str);
            C8_ComboBox.Items.Add(str);
            C9_ComboBox.Items.Add(str);

            C0_ComboBox.SelectedItem = str;
            C1_ComboBox.SelectedItem = str;
            C2_ComboBox.SelectedItem = str;
            C3_ComboBox.SelectedItem = str;
            C4_ComboBox.SelectedItem = str;
            C5_ComboBox.SelectedItem = str;
            C6_ComboBox.SelectedItem = str;
            C7_ComboBox.SelectedItem = str;
            C8_ComboBox.SelectedItem = str;
            C9_ComboBox.SelectedItem = str;

            foreach (string portCValue in portCValues)
            {
                C0_ComboBox.Items.Add(portCValue);
                C1_ComboBox.Items.Add(portCValue);
                C2_ComboBox.Items.Add(portCValue);
                C3_ComboBox.Items.Add(portCValue);
                C4_ComboBox.Items.Add(portCValue);
                C5_ComboBox.Items.Add(portCValue);
                C6_ComboBox.Items.Add(portCValue);
                C7_ComboBox.Items.Add(portCValue);
                C8_ComboBox.Items.Add(portCValue);
                C9_ComboBox.Items.Add(portCValue);
            }

            C0_ComboBox.Items.Add("CAN_RX");
            C1_ComboBox.Items.Add("CAN_TX");

            C2_ComboBox.Items.Add("UART_RX");
            C3_ComboBox.Items.Add("UART_TX");

            C4_ComboBox.Items.Add("I2C_SDA");
            C5_ComboBox.Items.Add("I2C_SCL");

            C6_ComboBox.Items.Add("SPI_MOSI");
            C7_ComboBox.Items.Add("SPI_MISO");
            C8_ComboBox.Items.Add("SPI_SCK");
            C9_ComboBox.Items.Add("SPI_CS");


            C0_ComboBox.IsEnabled = false;
            C1_ComboBox.IsEnabled = false;
            C2_ComboBox.IsEnabled = false;
            C3_ComboBox.IsEnabled = false;
            C4_ComboBox.IsEnabled = false;
            C5_ComboBox.IsEnabled = false;
            C6_ComboBox.IsEnabled = false;
            C7_ComboBox.IsEnabled = false;
            C8_ComboBox.IsEnabled = false;
            C9_ComboBox.IsEnabled = false;
        }

        private void InitportBFrequency()
        {
            foreach(string portBFrequencyValue in portBFrequencyValues)
            {
                B0_Frequency.Items.Add(portBFrequencyValue);
                B1_Frequency.Items.Add(portBFrequencyValue);
                B2_Frequency.Items.Add(portBFrequencyValue);
                B3_Frequency.Items.Add(portBFrequencyValue);
                B4_Frequency.Items.Add(portBFrequencyValue);
                B5_Frequency.Items.Add(portBFrequencyValue);
            }

            B0_Frequency.SelectedItem = "Medium";
            B1_Frequency.SelectedItem = "Medium";
            B2_Frequency.SelectedItem = "Medium";
            B3_Frequency.SelectedItem = "Medium";
            B4_Frequency.SelectedItem = "Medium";
            B5_Frequency.SelectedItem = "Medium";

            B0_Frequency.IsEnabled = false;
            B1_Frequency.IsEnabled = false;
            B2_Frequency.IsEnabled = false;
            B3_Frequency.IsEnabled = false;
            B4_Frequency.IsEnabled = false;
            B5_Frequency.IsEnabled = false;
        }

        private void InitPortAIndicators()
        {
            A0_Indicator.Fill = Brushes.Gray;
            A1_Indicator.Fill = Brushes.Gray;
            A2_Indicator.Fill = Brushes.Gray;
            A3_Indicator.Fill = Brushes.Gray;
            A4_Indicator.Fill = Brushes.Gray;
            A5_Indicator.Fill = Brushes.Gray;
            A6_Indicator.Fill = Brushes.Gray;
            A7_Indicator.Fill = Brushes.Gray;
            A8_Indicator.Fill = Brushes.Gray;
            A9_Indicator.Fill = Brushes.Gray;
            A10_Indicator.Fill = Brushes.Gray;
            A11_Indicator.Fill = Brushes.Gray;
            A12_Indicator.Fill = Brushes.Gray;
            A13_Indicator.Fill = Brushes.Gray;
            A14_Indicator.Fill = Brushes.Gray;
            A15_Indicator.Fill = Brushes.Gray;
        }

        private void InitPortBIndicators()
        {
            B0_Indicator.Fill = Brushes.Gray;
            B1_Indicator.Fill = Brushes.Gray;
            B2_Indicator.Fill = Brushes.Gray;
            B3_Indicator.Fill = Brushes.Gray;
            B4_Indicator.Fill = Brushes.Gray;
            B5_Indicator.Fill = Brushes.Gray;
        }

        private void InitPortCIndicators()
        {
            C0_Indicator.Fill = Brushes.Gray;
            C1_Indicator.Fill = Brushes.Gray;
            C2_Indicator.Fill = Brushes.Gray;
            C3_Indicator.Fill = Brushes.Gray;
            C4_Indicator.Fill = Brushes.Gray;
            C5_Indicator.Fill = Brushes.Gray;
            C6_Indicator.Fill = Brushes.Gray;
            C7_Indicator.Fill = Brushes.Gray;
            C8_Indicator.Fill = Brushes.Gray;
            C9_Indicator.Fill = Brushes.Gray;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                RefreshComPortsList.Abort();
                RefreshCurrentComPortState.Abort();
                CloseComPort();
            }
            catch
            {

            }       
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            InitConnection();
        }

        private void InitConnection()
        {
            string str = string.Empty;

            Dispatcher.Invoke(()=>
            {
                str = "Try connect to " + PortsComboBox.SelectedItem + " (" +
                              BaudRateValuesComboBox.SelectedItem + ", " +
                              DataBitsValuesComboBox.SelectedItem + ", " +
                              ParityValuesComboBox.SelectedItem + ", " +
                              StopBitsValuesComboBox.SelectedItem + ")";
                
                LogText.Text += str + '\n';
            });

            allowRefreshComPorts.Reset();
            DisableSettings();
            ConnectToComPort = new Thread(ConnectToComPortFunc);
            ConnectToComPort.Start();
        }

        private void ConnectToComPortFunc()
        {
            Thread.Sleep(100);

            string pn = "";
            int br = 0, db = 0, rt = 0, wt = 0;
            Parity p = Parity.None;
            StopBits sb = StopBits.One;

            Dispatcher.Invoke(()=> 
            {
                pn = PortsComboBox.SelectedItem.ToString();
                br = Convert.ToInt32(BaudRateValuesComboBox.SelectedItem);
                db = Convert.ToInt32(DataBitsValuesComboBox.SelectedItem);
                p = (Parity)Convert.ToInt32(ParityValuesComboBox.SelectedIndex);
                sb = (StopBits)Convert.ToInt32(StopBitsValuesComboBox.SelectedIndex + 1);
            });

           
            try
            {
                serialPort.PortName = pn;
                serialPort.BaudRate = br;
                serialPort.DataBits = db;
                serialPort.Parity = p;
                serialPort.StopBits = sb;
                serialPort.ReadTimeout = 1000;
                serialPort.WriteTimeout = 1000;

                serialPort.Open();
            }
            catch (Exception e)
            {
                displayLog("[ ERROR ] Connect to COM port. Stop connecting...");
                StopConnection();
                return;
            }

            allowRefreshCurrentComPortState.Set();

            Dispatcher.Invoke(() => DisconnectToComPortButton.IsEnabled = true );

            displayLog("[ OK ] Connect to COM port.");

            serialPort.DataReceived += new SerialDataReceivedEventHandler(ReceiverDataFunc);
        }

        private void DisconnectToComPortButton_Click(object sender, RoutedEventArgs e)
        {
            displayLog("Completion work with COM port...");
            StopConnection();
        }

        private void StopConnection()
        {
            allowRefreshCurrentComPortState.Reset();
            CloseComPort();
            EnableSettings();

            allowRefreshComPorts.Set();
        }

        private void CloseComPort()
        {
            try
            {
                serialPort.Close();
            }
            catch (Exception ex)
            {
                displayLog("[ ERROR ] Close COM port.");
                displayLog(ex.Message);
                return;
            }

            displayLog("[ OK ] Close COM port.");

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void ReceiverDataFunc(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string str = string.Empty;
           
            try
            {
                str = serialPort.ReadLine();
            }
            catch(TimeoutException ex)
            {
                displayLog("[ WARNING ] Timeout exception when read line from COM port.");
            }
            catch(Exception ex)
            {
                displayLog("[ ERROR ] Read line from COM port.\n");
                displayLog(ex.Message);
            }

            Dispatcher.Invoke(() =>
            {
                if (!String.IsNullOrEmpty(str) && DisplayReceiverDataCheckBox.IsChecked == true)
                {
                    if (DisplaySenderCheckBox.IsChecked == true)
                        str = "[ Port ] " + str;
                        
                    str = str.Trim(new Char[] { '\n', '\r' });
                }     
            });
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
        }

        private void SendDataTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Button_Click_1(null, null);
            }
        }

        private void PortA_ClickHandler(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;

            Brush color = checkbox.IsChecked == true ? Brushes.Red : Brushes.Gray;
            switch(checkbox.Content)
            {
                case "A0":  A0_Indicator.Fill  = color; break;
                case "A1":  A1_Indicator.Fill  = color; break;
                case "A2":  A2_Indicator.Fill  = color; break;
                case "A3":  A3_Indicator.Fill  = color; break;
                case "A4":  A4_Indicator.Fill  = color; break;
                case "A5":  A5_Indicator.Fill  = color; break;
                case "A6":  A6_Indicator.Fill  = color; break;
                case "A7":  A7_Indicator.Fill  = color; break;
                case "A8":  A8_Indicator.Fill  = color; break;
                case "A9":  A9_Indicator.Fill  = color; break;
                case "A10": A10_Indicator.Fill = color; break;
                case "A11": A11_Indicator.Fill = color; break;
                case "A12": A12_Indicator.Fill = color; break;
                case "A13": A13_Indicator.Fill = color; break;
                case "A14": A14_Indicator.Fill = color; break;
                case "A15": A15_Indicator.Fill = color; break;
            }

            byte portNum = Byte.Parse(((string)checkbox.Content).Remove(0, 1));
            byte cmd = (byte)(0x03 + 2 * portNum);

            if (checkbox.IsChecked == false)
                ++cmd;

            byte[] arr = new byte[2] {cmd, (byte)'\n'};

            try
            {
                serialPort.Write(arr, 0, 2);
            }
            catch (Exception ex)
            {
                displayLog("[ ERROR ] Send arr to COM port.");
                displayLog(ex.Message);
            }

            //TransiverData = new Thread(new ParameterizedThreadStart(TransiverArrayFunc));
            //TransiverData.Start(arr);

            displayLog("Send arr: [ " + String.Join(", ", arr) + " ]");
        }

        private void TransiverArrayFunc(object arr)
        {
            byte[] array = arr as byte[];

            try
            {
                serialPort.Write(array, 0, array.Length);
            }
            catch (Exception e)
            {
                //Dispatcher.Invoke(() => testLabel.Content = e.Message);
               // displayLog();
                return;
            }
        }

        

        private void cp_SelectedColorChanged_1(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (cp.SelectedColor.HasValue)
            {
                Color C = cp.SelectedColor.Value;
               
                int Red = C.R * 257;
                int Green = C.G * 257;
                int Blue = C.B * 257;

                B0.Value = Red;
                B1.Value = Green;
                B2.Value = Blue;

                byte[] arr = new byte[] { 0x23, (byte)(Red >> 8), (byte)(Red & 0xff), 0x25, (byte)(Green >> 8), (byte)(Green & 0xff), 0x27, (byte)(Blue >> 8), (byte)(Blue & 0xff), (byte)'\n' };

                if(RGB_MaxSpeed_CheckBox.IsChecked == false)
                {
                    try
                    {
                        serialPort.Write(arr, 0, arr.Length);
                    }
                    catch(Exception ex)
                    {
                        displayLog("[ ERROR ] Send data to serial port.");
                        displayLog(ex.Message);
                        return;
                    }

                    displayLog("Send arr: [ " + String.Join(", ", arr) + " ]");
                }
                else
                {
                    serialPort.Write(arr, 0, arr.Length);
                }
            }

        }

        private void PortB_Handler(object sender, RoutedEventArgs e)
        {
            Slider slider = sender as Slider;

            int pwmValue = (int)Math.Round(slider.Value);
            
            byte portNum = Byte.Parse(((string)slider.Name).Remove(0, 1));
            byte cmd = (byte)(0x23 + 2 * portNum);
            byte[] arr;


            if (pwmValue == 0)
                arr = new byte[] { ++cmd, (byte)'\n' };
            else
                arr = new byte[] { cmd, (byte)(pwmValue >> 8), (byte)(pwmValue & 0xff), (byte)'\n' };

            displayLog("Send arr: [ " + String.Join(", ", arr) + " ]");

            TransiverData = new Thread(new ParameterizedThreadStart(TransiverArrayFunc));
            TransiverData.Start(arr);

            float value = ((float)pwmValue / 656) / 100;
            

            LinearGradientBrush fiveColorLGB = new LinearGradientBrush();
            
            fiveColorLGB.StartPoint = new Point(0.5, 0);
            fiveColorLGB.EndPoint = new Point(0.5, 1);

            GradientStop blackGS = new GradientStop();
            blackGS.Color = Colors.Gray;
            blackGS.Offset = 1.0 - value;
            fiveColorLGB.GradientStops.Add(blackGS);

            GradientStop redGS = new GradientStop();
            redGS.Color = Colors.Red;
            redGS.Offset = 1.0 - value;
            fiveColorLGB.GradientStops.Add(redGS);


            switch(slider.Name)
            {
                case "B0": B0_Indicator.Fill = fiveColorLGB; break;
                case "B1": B1_Indicator.Fill = fiveColorLGB; break;
                case "B2": B2_Indicator.Fill = fiveColorLGB; break;
                case "B3": B3_Indicator.Fill = fiveColorLGB; break;
                case "B4": B4_Indicator.Fill = fiveColorLGB; break;
                case "B5": B5_Indicator.Fill = fiveColorLGB; break;
            }
            
        }

        private void PortC_ClickHandler(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;

            switch (checkBox.Content)
            {
                case "CAN":
                    if(checkBox.IsChecked == true)
                    {
                        C0_Indicator.Fill = Brushes.Yellow;
                        C1_Indicator.Fill = Brushes.Blue;

                        C0_ComboBox.SelectedItem = "CAN_RX";
                        C1_ComboBox.SelectedItem = "CAN_TX";
                    }
                    else
                    {
                        C0_Indicator.Fill = Brushes.Gray;
                        C1_Indicator.Fill = Brushes.Gray;

                        C0_ComboBox.SelectedItem = "None";
                        C1_ComboBox.SelectedItem = "None";
                    }
                    
                    break;

                case "UART":
                    if (checkBox.IsChecked == true)
                    {
                        C2_Indicator.Fill = Brushes.Yellow;
                        C3_Indicator.Fill = Brushes.Blue;

                        C2_ComboBox.SelectedItem = "UART_RX";
                        C3_ComboBox.SelectedItem = "UART_TX";
                    }
                    else
                    {
                        C2_Indicator.Fill = Brushes.Gray;
                        C3_Indicator.Fill = Brushes.Gray;

                        C2_ComboBox.SelectedItem = "None";
                        C3_ComboBox.SelectedItem = "None";
                    }
                    break;
                    
                case "I2C":
                    if (checkBox.IsChecked == true)
                    {
                        C4_Indicator.Fill = Brushes.Fuchsia;
                        C5_Indicator.Fill = Brushes.Green;

                        C4_ComboBox.SelectedItem = "I2C_SDA";
                        C5_ComboBox.SelectedItem = "I2C_SCL";
                    }
                    else
                    {
                        C4_Indicator.Fill = Brushes.Gray;
                        C5_Indicator.Fill = Brushes.Gray;

                        C4_ComboBox.SelectedItem = "None";
                        C5_ComboBox.SelectedItem = "None";
                    }
                    break;
                    
                case "SPI":
                    if (checkBox.IsChecked == true)
                    {
                        C6_Indicator.Fill = Brushes.Fuchsia;
                        C7_Indicator.Fill = Brushes.Green;
                        C8_Indicator.Fill = Brushes.Orange;
                        C9_Indicator.Fill = Brushes.Brown;
                        
                        C6_ComboBox.SelectedItem = "SPI_MOSI";
                        C7_ComboBox.SelectedItem = "SPI_MISO";
                        C8_ComboBox.SelectedItem = "SPI_SCK";
                        C9_ComboBox.SelectedItem = "SPI_CS";
                    }
                    else
                    {
                        C6_Indicator.Fill = Brushes.Gray;
                        C7_Indicator.Fill = Brushes.Gray;
                        C8_Indicator.Fill = Brushes.Gray;
                        C9_Indicator.Fill = Brushes.Gray;

                        C6_ComboBox.SelectedItem = "None";
                        C7_ComboBox.SelectedItem = "None";
                        C8_ComboBox.SelectedItem = "None";
                        C9_ComboBox.SelectedItem = "None";
                    }
                    break;
            }
        }

        private void displayLog(string str)
        {
            Dispatcher.Invoke(() => {

                LogText.Text += str + '\n';
                LogText.ScrollToEnd();
            });
        }
    }
}

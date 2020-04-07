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

        readonly private static string[] baudRateValues = { "9600", "115200"};
        readonly private static string[] dataBitsValues = { "5", "6", "7", "8", "9"};
        readonly private static string[] parityValues = { "None", "Odd", "Even", "Mark", "Space"};
        readonly private static string[] stopBitsValues = { "1", "2", "1.5"};
        readonly private static string[] EndOfLineValues = {"None", "NL", "CR", "NL&CR"};

        private static int oldComPortsAmount = -1;

        private static bool firstDisplayPortList = false;

        public MainWindow()
        {
            InitializeComponent();

            InitBaudRateValuesComboBox();
            InitDataBitsValuesComboBox();
            InitParityValuesComboBox();
            InitStopBitsValuesComboBox();

            serialPort = new SerialPort();

            RefreshCurrentComPortState = new Thread(RefreshCurrentComPortStateFunc);
            RefreshCurrentComPortState.Start();

            RefreshComPortsList = new Thread(RefreshComPortsListFunc);
            RefreshComPortsList.Start();
        }



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
                BaudRateValuesComboBox.SelectedItem = "115200";
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

            Dispatcher.Invoke(()=> str = "Try connect to " + PortsComboBox.SelectedItem + " (" +
                              BaudRateValuesComboBox.SelectedItem + ", " +
                              DataBitsValuesComboBox.SelectedItem + ", " +
                              ParityValuesComboBox.SelectedItem + ", " +
                              StopBitsValuesComboBox.SelectedItem + ")");

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
                StopConnection();
                return;
            }

            allowRefreshCurrentComPortState.Set();

            Dispatcher.Invoke(() =>
            {
                DisconnectToComPortButton.IsEnabled = true;
               
            });

            //Connection successful
            serialPort.DataReceived += new SerialDataReceivedEventHandler(ReceiverDataFunc);
        }

        private void DisconnectToComPortButton_Click(object sender, RoutedEventArgs e)
        {
            StopConnection();
            //Connection is closed
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
            catch (Exception e)
            {
               //e.Message
            }
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
            catch(Exception ex)
            {
                testLabel.Content = ex.Message;
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

            byte portNum = Byte.Parse(((string)checkbox.Content).Remove(0, 1));
            byte cmd = (byte)(0x03 + 2 * portNum);

            if (checkbox.IsChecked == false)
                ++cmd;

            byte[] arr = new byte[2] {cmd, (byte)'\n'};
            
            testLabel.Content = "Send arr: [ " + String.Join(", ", arr) + " ]";

            TransiverData = new Thread(new ParameterizedThreadStart(TransiverArrayFunc));
            TransiverData.Start(arr);
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
                Dispatcher.Invoke(() => testLabel.Content = e.Message);
                return;
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
                arr = new byte[] {++cmd, (byte)'\n' };
            else
                arr = new byte[] {cmd, (byte)(pwmValue >> 8), (byte)(pwmValue & 0xff), (byte)'\n' };
          
            testLabel.Content = "Send arr: [ " + String.Join(", ", arr) + " ]";
            
            TransiverData = new Thread(new ParameterizedThreadStart(TransiverArrayFunc));
            TransiverData.Start(arr);
        }

        private void cp_SelectedColorChanged_1(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (cp.SelectedColor.HasValue)
            {
                Color C = cp.SelectedColor.Value;
               
                int Red = C.R * 255;
                int Green = C.G * 255;
                int Blue = C.B * 255;
                
                byte[] arr = new byte[] { 0x23, (byte)(Red >> 8), (byte)(Red & 0xff), 0x25, (byte)(Green >> 8), (byte)(Green & 0xff), 0x27, (byte)(Blue >> 8), (byte)(Blue & 0xff), (byte)'\n' };

                //Максимум скорости
                serialPort.Write(arr, 0, arr.Length);
            }

        }
    }
}

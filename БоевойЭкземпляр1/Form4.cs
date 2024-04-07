using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.IO;

namespace БоевойЭкземпляр1
{
  

    public partial class Form4 : Form
    {
        class ProcessInfo //Класс, определяющий форму сканирования процессов
        {
            public int Port { get; set; }
            public string ProcessName { get; set; }
            public int ProcessId { get; set; }
            public string FilePath { get; set; }
            public IPAddress DestinationIP { get; set; }
            public string Organization { get; set; }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MibTcpRowOwnerPid //Структура для определения TCP-пакета
        {
            public uint State;
            public uint LocalAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] LocalPort;
            public uint RemoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] RemotePort;
            public uint OwningPid;

            public int LocalPortValue
            {
                get { return BitConverter.ToUInt16(new byte[2] { LocalPort[1], LocalPort[0] }, 0); }
            }
        }

        [DllImport("iphlpapi.dll", SetLastError = true)] //Системный файлы Windows для помощи сканированию
        public static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwSize, bool sort, int ipVersion, int tblClass, int reserved);

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int IphlpapiSendARP(uint destIp, uint srcIP, byte[] macAddr, ref uint physicalAddrLen);
        static async Task Main(Form4 form)
        {
 
            int AF_INET = 2; // IPv4
            int TCP_TABLE_OWNER_PID_ALL = 5;
            int bufferSize = 0;
            IntPtr tcpTable = IntPtr.Zero;

            List<string> FilesToScan = new List<string>();

            GetExtendedTcpTable(tcpTable, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
            tcpTable = Marshal.AllocHGlobal(bufferSize);

            GetExtendedTcpTable(tcpTable, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);

            int rowNum = Marshal.ReadInt32(tcpTable);
            IntPtr rowPtr = IntPtr.Add(tcpTable, 4);

            var processTasks = new List<Task<ProcessInfo>>();

            for (int i = 0; i < rowNum; i++)
            {
                MibTcpRowOwnerPid tcpRow = (MibTcpRowOwnerPid)Marshal.PtrToStructure(rowPtr, typeof(MibTcpRowOwnerPid));

                processTasks.Add(GetProcessInfoAsync(tcpRow, FilesToScan));

                rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf(tcpRow));
            }

            var processResults = new List<ProcessInfo>();

            foreach (var task in processTasks)
            {
                ProcessInfo result = await task;
                processResults.Add(result);
            }

            Marshal.FreeHGlobal(tcpTable);
            foreach (var result in processResults)
            {
                if (result.Organization != "N/A" && result.FilePath != "N/A")
                {
                    form.listBox1.Items.Add($"Порт: {result.Port}, Имя процесса: {result.ProcessName}, " +
                                  $"Идентификатор процесса: {result.ProcessId}, Путь к файлу процесса: {result.FilePath}, " +
                                  $"IP адрес назначения: {result.DestinationIP}, Организация: {result.Organization}");
                }
                else if (result.Organization == "N/A" && result.FilePath != "N/A")
                {
                    form.listBox1.Items.Add($"Порт: {result.Port}, Имя процесса: {result.ProcessName}, " +
                                  $"Идентификатор процесса: {result.ProcessId}, Путь к файлу процесса: {result.FilePath}, " +
                                  $"IP адрес назначения: {result.DestinationIP}");
                }
                else if (result.Organization != "N/A" && result.FilePath == "N/A")
                {
                    form.listBox1.Items.Add($"Порт: {result.Port}, Имя процесса: {result.ProcessName}, " +
                                  $"Идентификатор процесса: {result.ProcessId}, " +
                                  $"IP адрес назначения: {result.DestinationIP}, Организация: {result.Organization}");
                }
                else
                {
                    form.listBox1.Items.Add($"Порт: {result.Port}, Имя процесса: {result.ProcessName}, " +
                                  $"Идентификатор процесса: {result.ProcessId}, " +
                                  $"IP адрес назначения: {result.DestinationIP}");
                }
            }
        }
        static async Task<ProcessInfo> GetProcessInfoAsync(MibTcpRowOwnerPid tcpRow, List<string> FilesToScan) //Определение процессов
        {
            ProcessInfo processInfo = new ProcessInfo();
            processInfo.Port = tcpRow.LocalPortValue;
            processInfo.ProcessId = (int)tcpRow.OwningPid;

            try
            {
                var process = System.Diagnostics.Process.GetProcessById((int)tcpRow.OwningPid);
                processInfo.ProcessName = process.ProcessName;
                processInfo.FilePath = await Task.Run(() => GetProcessFilePath(process));
                processInfo.DestinationIP = new IPAddress(tcpRow.RemoteAddr);

                if (processInfo.DestinationIP != null)
                {
                    processInfo.Organization = await GetOrganizationInfo(processInfo.DestinationIP);
                }
                else
                {
                    processInfo.Organization = "N/A";
                }
            }
            catch (Exception)
            {
                processInfo.ProcessName = "N/A";
                processInfo.FilePath = "N/A";
                processInfo.Organization = "N/A";
            }

            if (processInfo.FilePath != "N/A")
            {
                FilesToScan.Add(processInfo.FilePath);
            }

            return processInfo;
        }

        static string GetProcessFilePath(System.Diagnostics.Process process)
        {
            string filePath = "N/A";
            try
            {
                filePath = process.MainModule.FileName;
            }
            catch (Exception) { }
            return filePath;
        }

        static async Task<string> GetOrganizationInfo(IPAddress ip)
        {
            try
            {
                string hostName = Dns.GetHostEntry(ip).HostName;
                IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
                string orgName = hostEntry.HostName;
                return orgName;
            }
            catch (Exception)
            {
                return "N/A";
            }
        }
        public Form4()
        {
            InitializeComponent();
        }
        private void button3_Click_1(object sender, EventArgs e)
        {
            Hide();
        }
       
        private async void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Инициирован запуск сканера процессов", "ВНИМАНИЕ", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Выводим окно предупреждения начала сканирования процессов
            await Main(this);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<string> dataList = new List<string>();

            // Пройдемся по элементам ListBox1 и добавим данные в список
            foreach (var item in listBox1.Items)
            {
                string data = (string)item;
                dataList.Add(data);
            }

            string filePath = "C:\\Program Files\\КИБ\\dynamic_res.json";

            // Запишем каждый элемент списка в формате JSON с новой строкой
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var data in dataList)
                {
                    string jsonData = JsonConvert.SerializeObject(data); // Сериализуем каждый элемент в JSON
                    writer.WriteLine(jsonData); // Записываем в файл с новой строкой
                }
            }

            MessageBox.Show("Сканирование завершено. Путь к файлу отчета: C:\\Program Files\\КИБ\\dynamic_res.json", "ИНФОРМАЦИЯ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    
}

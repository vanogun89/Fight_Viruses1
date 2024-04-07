using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace БоевойЭкземпляр1
{

    public partial class Form1 : Form
    {
        static async Task put_IP(Form1 form)
        {
            // Получаем информацию о локальном узле
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // Фильтрация IPv4 адресов
                {
                    string localIP = ip.ToString();

                    string[] ipParts = localIP.Split('.');
                    string subnet = ipParts[0] + "." + ipParts[1] + "." + ipParts[2] + ".";

                    var tasks = new Task[254];
                    int taskIndex = 0;

                    for (int i = 1; i < 255; i++)
                    {
                        string targetIP = subnet + i.ToString();
                        tasks[taskIndex++] = CheckNodeAvailabilityAsync(targetIP, form);
                    }

                    await Task.WhenAll(tasks);
                }
            }
        }

        static async Task CheckNodeAvailabilityAsync(string targetIP, Form1 form)
        {
            using (Ping ping = new Ping())
            {
                PingReply reply = await ping.SendPingAsync(targetIP, 100); // Асинхронный пинг узла

                if (reply.Status == IPStatus.Success)
                {
                    form.listBox1.Items.Add($"Узел {targetIP} доступен.");
                }
            }
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form4 fr4 = new Form4();
            fr4.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 fr2 = new Form2();
            fr2.Show();
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            await put_IP(this);
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            while (true)
            {
                listBox1.Items.Clear();
                await put_IP(this);
                foreach (var itemL1 in listBox1.Items)
                {
                    string currentString = itemL1.ToString();

                    bool stringFound = false;

                    // Перебираем элементы listBox2 и проверяем наличие текущей строки
                    foreach (var itemL2 in listBox2.Items)
                    {
                        if (currentString == itemL2.ToString())
                        {
                            stringFound = true;
                            break;
                        }
                    }

                    if (!stringFound)
                    {
                        MessageBox.Show("Обнаружена угроза: сетевая разведка", "ВНИМАНИЕ", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Выводим окно обнаружения угрозы
                    }
                }
                await Task.Delay(3000);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            foreach (var itemL1 in listBox1.Items)
            {
                string currentString = itemL1.ToString();

                bool stringFound = false;

                // Перебираем элементы listBox2 и проверяем наличие текущей строки
                foreach (var itemL2 in listBox2.Items)
                {
                    if (currentString == itemL2.ToString())
                    {
                        stringFound = true;
                        break;
                    }
                }

                if (!stringFound)
                {
                    listBox2.Items.Add(currentString); // Добавляем строку в listBox2, если не найдена
                }
            }
        }

       
    }
}
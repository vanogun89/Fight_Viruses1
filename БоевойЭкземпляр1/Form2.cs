using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YaraSharp;

namespace БоевойЭкземпляр1
{
    public partial class Form2 : Form
    {
        public class ScanResult //Класс, определяющий форму результатов сканирования
        {
            public string FilePath { get; set; }
            public string Match { get; set; }
        }
        static async Task Main(Form2 form)
        {
            YSInstance instance = new YSInstance();
            Dictionary<string, object> externals = new Dictionary<string, object>()
        {
            { "filename", string.Empty },
            { "filepath", string.Empty },
            { "extension", string.Empty }
        };
            if (form.textBox1.Text.ToString() == "")
            {
                MessageBox.Show("Ошибка сканирования: неверно указан путь", "ВНИМАНИЕ", MessageBoxButtons.OK, MessageBoxIcon.Error); // Выводим окно предупреждения начала сканирования процессов
            }
            else if (form.textBox1.Text.ToString() != "")
            {
                string directoryToScan = form.textBox1.Text.ToString(); // Путь к каталогу для сканирования
                List<string> ruleFilenames = Directory.GetFiles(@"C:\Program Files\КИБ\all", "*.yara", SearchOption.AllDirectories).ToList();
                List<ScanResult> results = new List<ScanResult>();

                using (YSContext context = new YSContext())
                {
                    using (YSCompiler compiler = instance.CompileFromFiles(ruleFilenames, externals))
                    {
                        YSRules rules = compiler.GetRules();
                        YSReport errors = compiler.GetErrors();
                        YSReport warnings = compiler.GetWarnings();

                        await RecursivelyScanDirectoryAsync(directoryToScan, instance, rules, results, form);

                        // Сохраняем результаты сканирования в JSON файл
                        string json = JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(@"C:\Program Files\КИБ\scan_res.json", json);
                        MessageBox.Show("Сканирование завершено. Путь к файлу отчета: C:\\Program Files\\КИБ\\scan_res.json", "ИНФОРМАЦИЯ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        static async Task RecursivelyScanDirectoryAsync(string directory, YSInstance instance, YSRules rules, List<ScanResult> results, Form2 form)
        {
            try                                                                                 //Рекурсивное сканирование файлов системы
            {
                IEnumerable<string> files = Directory.EnumerateFiles(directory);
                var tasks = new List<Task>();

                foreach (string file in files)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            List<YSMatches> matches = await Task.Run(() => instance.ScanFile(file, rules, new Dictionary<string, object>()
                    {
                        { "filename", Path.GetFileName(file) },
                        { "filepath", Path.GetFullPath(file) },
                        { "extension", Path.GetExtension(file) }
                    }, 0));

                            if (matches.Any())
                            {
                                foreach (YSMatches match in matches)
                                {
                                    Console.WriteLine(match + "\n");
                                    ScanResult scanResult = new ScanResult()
                                    {
                                        FilePath = Path.GetFullPath(file),
                                        Match = match.Rule.Identifier.ToString()
                                    };

                                    form.Invoke((MethodInvoker)delegate {
                                        form.listBox2.Items.Add($"{Path.GetFullPath(file)} - содержит угрозу: {scanResult}");
                                    });

                                    results.Add(scanResult);
                                }
                            }
                            else
                            {
                                form.Invoke((MethodInvoker)delegate {
                                    form.listBox1.Items.Add($"{Path.GetFullPath(file)} - нет совпадений");
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            form.Invoke((MethodInvoker)delegate
                            {
                                form.listBox2.Items.Add($"Произошла ошибка при сканировании файла: {file} - файл требуется проверить динамическим ядром - сканером процессов");
                            });
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                IEnumerable<string> directories = Directory.EnumerateDirectories(directory);

                foreach (string subdirectory in directories)
                {
                    await RecursivelyScanDirectoryAsync(subdirectory, instance, rules, results, form);
                }
            }
            catch (Exception ex)
            {
                form.listBox2.Items.Add($"Произошла ошибка при сканировании директории: {directory}. {ex.Message}");
            }
        }
        public Form2()
        {
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await Main(this);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChromeHtmlToPdfLib;
using ChromeHtmlToPdfLib.Settings;
using Xunit;

namespace ChromeHtmlToPdfLibStandard.UnitTest
{
    public class ConverterTest
    {

        private readonly string _htmlFileContent;
        private readonly string _xmlFileContent;
        private readonly string _textFileContent;
        
        public ConverterTest()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var testFileDir = Path.Combine(baseDir, "TestFiles");

            _htmlFileContent = File.ReadAllText(Path.Combine(testFileDir, "Test.html"), Encoding.Default);
            _xmlFileContent = File.ReadAllText(Path.Combine(testFileDir, "Test.xml"), Encoding.Default);
            _textFileContent = File.ReadAllText(Path.Combine(testFileDir, "Test.txt"), Encoding.Default);

        }
        
        [Fact]
        public void TestHtml()
        {
            var infile = Guid.NewGuid() + ".html";
            var outfile = Guid.NewGuid() + ".pdf";
            infile = Path.Combine(Path.GetTempPath(), infile);
            outfile = Path.Combine(Path.GetTempPath(), outfile);
            File.WriteAllText(infile, _htmlFileContent);

            Convert(infile, outfile);
        }
        
        [Fact]
        public void TestText()
        {
            var infile = Guid.NewGuid() + ".txt";
            var outfile = Guid.NewGuid() + ".pdf";
            infile = Path.Combine(Path.GetTempPath(), infile);
            outfile = Path.Combine(Path.GetTempPath(), outfile);
            File.WriteAllText(infile, _textFileContent);

            Convert(infile, outfile);
        }
        
        [Fact]
        public void TestXml()
        {
            var infile = Guid.NewGuid() + ".xml";
            var outfile = Guid.NewGuid() + ".pdf";
            infile = Path.Combine(Path.GetTempPath(), infile);
            outfile = Path.Combine(Path.GetTempPath(), outfile);
            File.WriteAllText(infile, _xmlFileContent);

            Convert(infile, outfile);
        }

        [Fact]
        public void TreadingStressTest()
        {
            using(var chrome = new ChromeProcess())
            {
                chrome.EnsureRunning();
                var infile = Guid.NewGuid() + ".xml";
                infile = Path.Combine(Path.GetTempPath(), infile);
                File.WriteAllText(infile, _xmlFileContent);
                var tasks = new List<Task>();
                for (var i = 0; i < 100; i++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var outfile = Guid.NewGuid() + ".pdf";
                        outfile = Path.Combine(Path.GetTempPath(), outfile);
                        ConvertWithProcess(chrome, infile, outfile);
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
        }
        
        [Fact]
        public void StressTest()
        {
            using(var chrome = new ChromeProcess())
            {
                chrome.EnsureRunning();
                var infile = Guid.NewGuid() + ".xml";
                infile = Path.Combine(Path.GetTempPath(), infile);
                File.WriteAllText(infile, _xmlFileContent);
                for (var i = 0; i < 100; i++)
                {
                    var outfile = Guid.NewGuid() + ".pdf";
                    outfile = Path.Combine(Path.GetTempPath(), outfile);
                    ConvertWithProcess(chrome, infile, outfile);
                }
            }
        }

        private void ConvertWithProcess(ChromeProcess process, string infile, string outfile)
        {
            var pageSettings = new PageSettings();

            using (var converter = new Converter(process))
            {
                converter.ConvertToPdf(new ConvertUri(infile), outfile, pageSettings);
            }

            if (!File.Exists(outfile))
                throw new Exception($"HTML to PDF conversion failed; No result: {outfile}");
        }

        private void Convert(string infile, string outfile)
        {
            using (var chrome = new ChromeProcess())
            {
                chrome.EnsureRunning();
                ConvertWithProcess(chrome, infile, outfile);
            }
        }
    }
}
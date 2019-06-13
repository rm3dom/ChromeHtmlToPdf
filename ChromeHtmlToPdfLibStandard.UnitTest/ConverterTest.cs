using System;
using System.IO;
using System.Text;
using ChromeHtmlToPdfLib;
using ChromeHtmlToPdfLib.Settings;
using Xunit;

namespace ChromeHtmlToPdfLibStandard.UnitTest
{
    public class UnitTest1
    {

        private readonly string _htmlFileContent;
        private readonly string _xmlFileContent;
        private readonly string _textFileContent;
        
        public UnitTest1()
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

        private static void Convert(string infile, string outfile)
        {
            var pageSettings = new PageSettings();

            using (var converter = new ChromeHtmlToPdfLib.Converter())
            {
                converter.PreWrapExtensions.Add(new WrapExtension(".xml", false));
                converter.PreWrapExtensions.Add(new WrapExtension(".txt", false));
                converter.ConvertToPdf(new ConvertUri(infile), outfile, pageSettings);
            }

            if (!File.Exists(outfile))
                throw new Exception($"HTML to PDF conversion failed; No result: {outfile}");
        }
    }
}
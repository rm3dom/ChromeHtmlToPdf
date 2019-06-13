using System;
using System.IO;
using ChromeHtmlToPdfLib;
using ChromeHtmlToPdfLib.Settings;
using Xunit;

namespace ChromeHtmlToPdfLibStandard.UnitTest
{
    public class UnitTest1
    {
        [Fact]
        public void TestHtml()
        {
        }
        
        [Fact]
        public void TestText()
        {
        }
        
        [Fact]
        public void TestXml()
        {
        }

        private void Covert(string infile, string outfile)
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
namespace ChromeHtmlToPdfLib
{
    public class WrapExtension
    {
        public WrapExtension(string ext, bool wrap = true)
        {
            Extension = ext;
            Wrap = wrap;
        }

        public string Extension { get; }
        public bool Wrap { get; } = true;
    }
}
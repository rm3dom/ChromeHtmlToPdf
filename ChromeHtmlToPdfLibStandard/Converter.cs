//
// Converter.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2018 Magic-Sessions. (www.magic-sessions.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ChromeHtmlToPdfLib.Exceptions;
using ChromeHtmlToPdfLib.Helpers;
using ChromeHtmlToPdfLib.Settings;

// ReSharper disable UnusedMember.Global

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    ///     A converter class around Google Chrome headless to convert html to pdf
    /// </summary>
    public class Converter : IDisposable
    {
        /// <summary>
        ///     Handles the communication with Chrome dev tools, this will be null if chrome has not started yet.
        /// </summary>
        private readonly Browser _browser;


        private ChromeProcess _chromeProcess;

        /// <summary>
        ///     The directory used for temporary files
        /// </summary>
        private DirectoryInfo _currentTempDirectory;

        /// <summary>
        ///     Keeps track is we already disposed our resources
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///     When set then logging is written to this stream
        /// </summary>
        private Stream _logStream;

        /// <summary>
        ///     Used to add the extension of text based files that needed to be wrapped in an HTML PRE
        ///     tag so that they can be opened by Chrome
        /// </summary>
        private List<WrapExtension> _preWrapExtensions = new List<WrapExtension>();

        /// <summary>
        ///     When set then this folder is used for temporary files
        /// </summary>
        private string _tempDirectory;


        /// <summary>
        ///     An unique id that can be used to identify the logging of the converter when
        ///     calling the code from multiple threads and writing all the logging to the same file
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        ///     Used to add the extension of text based files that needed to be wrapped in an HTML PRE
        ///     tag so that they can be opened by Chrome
        /// </summary>
        /// <example>
        ///     <code>
        ///     var converter = new Converter()
        ///     converter.PreWrapExtensions.Add(".txt");
        ///     converter.PreWrapExtensions.Add(".log");
        ///     // etc ...
        ///     </code>
        /// </example>
        /// <remarks>
        ///     The extensions are used case insensitive
        /// </remarks>
        public List<WrapExtension> PreWrapExtensions
        {
            get => _preWrapExtensions;
            set => _preWrapExtensions = value;
        }

        /// <summary>
        ///     When set to <c>true</c> then images are resized to fix the given <see cref="PageSettings.PaperWidth" />
        /// </summary>
        public bool ImageResize { get; set; }

        /// <summary>
        ///     When set to <c>true</c> then images are automatically rotated following the orientation
        ///     set in the EXIF information
        /// </summary>
        public bool ImageRotate { get; set; }

        /// <summary>
        ///     The timeout in milliseconds before this application aborts the downloading
        ///     of images when the option <see cref="ImageResize" /> and/or <see cref="ImageRotate" />
        ///     is being used
        /// </summary>
        public int? ImageDownloadTimeout { get; set; }

        /// <summary>
        ///     When set then this directory is used to store temporary files.
        ///     For example files that are made in combination with <see cref="PreWrapExtensions" />
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">Raised when the given directory does not exists</exception>
        public string TempDirectory
        {
            get => _tempDirectory;
            set
            {
                if (!Directory.Exists(value))
                    throw new DirectoryNotFoundException($"The directory '{value}' does not exists");

                _tempDirectory = value;
            }
        }

        /// <summary>
        ///     Returns a reference to the temp directory
        /// </summary>
        private DirectoryInfo GetTempDirectory
        {
            get
            {
                _currentTempDirectory = _tempDirectory == null
                    ? new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                    : new DirectoryInfo(Path.Combine(_tempDirectory, Guid.NewGuid().ToString()));

                if (!_currentTempDirectory.Exists)
                    _currentTempDirectory.Create();

                return _currentTempDirectory;
            }
        }

        #region Dispose

        /// <summary>
        ///     Disposes the running <see cref="_chromeProcess" />
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_browser != null)
            {
                _browser.Close();
                _browser.Dispose();
            }
        }

        #endregion


        #region CheckIfOutputFolderExists

        /// <summary>
        ///     Checks if the path to the given <paramref name="outputFile" /> exists.
        ///     An <see cref="DirectoryNotFoundException" /> is thrown when the path is not valid
        /// </summary>
        /// <param name="outputFile"></param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private void CheckIfOutputFolderExists(string outputFile)
        {
            var directory = new FileInfo(outputFile).Directory;
            if (directory != null && !directory.Exists)
                throw new DirectoryNotFoundException($"The path '{directory.FullName}' does not exists");
        }

        #endregion

        #region CheckForPreWrap

        /// <summary>
        ///     Checks if <see cref="PreWrapExtensions" /> is set and if the extension
        ///     is inside this list. When in the list then the file is wrapped
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        private bool CheckForPreWrap(ConvertUri inputFile, out string outputFile)
        {
            outputFile = inputFile.LocalPath;

            if (PreWrapExtensions.Count == 0)
                return false;

            var ext = Path.GetExtension(inputFile.LocalPath);

            if (!PreWrapExtensions.Any(wrapExt =>
                wrapExt.Wrap && wrapExt.Extension.Equals(ext, StringComparison.InvariantCultureIgnoreCase)))
                return false;

            var preWrapper = new PreWrapper(GetTempDirectory);
            outputFile = preWrapper.WrapFile(inputFile.LocalPath, inputFile.Encoding);
            return true;
        }

        #endregion

        #region WriteToLog

        /// <summary>
        ///     Writes a line and linefeed to the <see cref="_logStream" />
        /// </summary>
        /// <param name="message">The message to write</param>
        private void WriteToLog(string message)
        {
            if (_logStream == null) return;

            try
            {
                var line = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") +
                           (InstanceId != null ? " - " + InstanceId : string.Empty) + " - " +
                           message + Environment.NewLine;
                var bytes = Encoding.UTF8.GetBytes(line);
                _logStream.Write(bytes, 0, bytes.Length);
                _logStream.Flush();
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
        }

        #endregion


        #region Constructor & Destructor

        /// <summary>
        ///     Creates this object and sets it's needed properties
        /// </summary>
        /// <param name="chromeExeFileName">
        ///     When set then this has to be tThe full path to the chrome executable.
        ///     When not set then then the converter tries to find Chrome.exe by first looking in the path
        ///     where this library exists. After that it tries to find it by looking into the registry
        /// </param>
        /// <param name="userProfile">
        ///     If set then this directory will be used to store a user profile.
        ///     Leave blank or set to <c>null</c> if you want to use the default Chrome user profile location
        /// </param>
        /// <param name="logStream">
        ///     When set then logging is written to this stream for all conversions. If
        ///     you want a separate log for each conversion then set the log stream on one of the ConvertToPdf" methods
        /// </param>
        /// <exception cref="FileNotFoundException">Raised when <see cref="chromeExeFileName" /> does not exists</exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     Raised when the <paramref name="userProfile" /> directory is given but
        ///     does not exists
        /// </exception>
        public Converter(ChromeProcess chrome, Stream logStream = null)
        {
            _preWrapExtensions = new List<WrapExtension>();
            _logStream = logStream;
            _browser = new Browser(chrome.InstanceHandle);
        }

        /// <summary>
        ///     Destructor
        /// </summary>
        ~Converter()
        {
            Dispose();
        }

        #endregion


        #region ConvertToPdf

        /// <summary>
        ///     Converts the given <paramref name="inputUri" /> to PDF
        /// </summary>
        /// <param name="inputUri">The webpage to convert</param>
        /// <param name="outputStream">The output stream</param>
        /// <param name="pageSettings">
        ///     <see cref="PageSettings" />
        /// </param>
        /// <param name="waitForWindowStatus">
        ///     Wait until the javascript window.status has this value before
        ///     rendering the PDF
        /// </param>
        /// <param name="waitForWindowsStatusTimeout"></param>
        /// <param name="conversionTimeout">
        ///     An conversion timeout in milliseconds, if the conversion fails
        ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
        /// </param>
        /// <param name="logStream">
        ///     When set then this will give a logging for each conversion. Use the log stream
        ///     option in the constructor if you want one log for all conversions
        /// </param>
        /// <exception cref="ConversionTimedOutException">
        ///     Raised when <see cref="conversionTimeout" /> is set and the
        ///     conversion fails to finish in this amount of time
        /// </exception>
        public void ConvertToPdf(ConvertUri inputUri,
            Stream outputStream,
            PageSettings pageSettings,
            string waitForWindowStatus = "",
            int waitForWindowsStatusTimeout = 60000,
            int? conversionTimeout = null,
            Stream logStream = null)
        {
            _logStream = logStream;

            if (inputUri.IsFile)
            {
                if (!File.Exists(inputUri.OriginalString))
                    throw new FileNotFoundException($"The file '{inputUri.OriginalString}' does not exists");

                var ext = Path.GetExtension(inputUri.OriginalString);

                switch (ext.ToLowerInvariant())
                {
                    case ".htm":
                    case ".html":
                        // This is ok
                        break;

                    default:
                        if (!PreWrapExtensions.Any(wrapExt =>
                            wrapExt.Extension.Equals(ext, StringComparison.InvariantCultureIgnoreCase)))
                            throw new ConversionException(
                                $"The file '{inputUri.OriginalString}' with extension '{ext}' is not valid. " +
                                "If this is a text based file then add the extension to the PreWrapExtensions");
                        break;
                }
            }

            try
            {
                if (inputUri.IsFile && CheckForPreWrap(inputUri, out var preWrapFile))
                {
                    inputUri = new ConvertUri(preWrapFile);
                }
                else if (ImageResize || ImageRotate)
                {
                    var imageHelper = new ImageHelper(GetTempDirectory, _logStream, _chromeProcess.WebProxy,
                            ImageDownloadTimeout)
                        {InstanceId = InstanceId};
                    if (!imageHelper.ValidateImages(inputUri, ImageResize, ImageRotate, pageSettings,
                        out var outputUri))
                        inputUri = outputUri;
                }

                CountdownTimer countdownTimer = null;

                if (conversionTimeout.HasValue)
                {
                    if (conversionTimeout <= 1)
                        throw new ArgumentOutOfRangeException(
                            $"The value for {nameof(countdownTimer)} has to be a value equal to 1 or greater");

                    WriteToLog($"Conversion timeout set to {conversionTimeout.Value} milliseconds");

                    countdownTimer = new CountdownTimer(conversionTimeout.Value);
                    countdownTimer.Start();
                }

                WriteToLog("Loading " + (inputUri.IsFile ? "file " + inputUri.OriginalString : "url " + inputUri));

                _browser.NavigateTo(inputUri, countdownTimer);

                if (!string.IsNullOrWhiteSpace(waitForWindowStatus))
                {
                    if (conversionTimeout.HasValue)
                    {
                        WriteToLog("Conversion timeout paused because we are waiting for a window.status");
                        countdownTimer.Stop();
                    }

                    WriteToLog(
                        $"Waiting for window.status '{waitForWindowStatus}' or a timeout of {waitForWindowsStatusTimeout} milliseconds");
                    var match = _browser.WaitForWindowStatus(waitForWindowStatus, waitForWindowsStatusTimeout);
                    WriteToLog(!match ? "Waiting timed out" : $"Window status equaled {waitForWindowStatus}");

                    if (conversionTimeout.HasValue)
                    {
                        WriteToLog("Conversion timeout started again because we are done waiting for a window.status");
                        countdownTimer.Start();
                    }
                }

                WriteToLog((inputUri.IsFile ? "File" : "Url") + " loaded");

                WriteToLog("Converting to PDF");

                using (var memoryStream = new MemoryStream(_browser.PrintToPdf(pageSettings, countdownTimer).Bytes))
                {
                    memoryStream.Position = 0;
                    memoryStream.CopyTo(outputStream);
                }

                WriteToLog("Converted");
            }
            catch (Exception exception)
            {
                WriteToLog($"Error: {ExceptionHelpers.GetInnerException(exception)}'");
                throw;
            }
            finally
            {
                if (_currentTempDirectory != null)
                {
                    _currentTempDirectory.Refresh();
                    if (_currentTempDirectory.Exists)
                    {
                        WriteToLog($"Deleting temporary folder '{_currentTempDirectory.FullName}'");
                        _currentTempDirectory.Delete(true);
                    }
                }
            }
        }

        /// <summary>
        ///     Converts the given <paramref name="inputUri" /> to PDF
        /// </summary>
        /// <param name="inputUri">The webpage to convert</param>
        /// <param name="outputFile">The output file</param>
        /// <param name="pageSettings">
        ///     <see cref="PageSettings" />
        /// </param>
        /// <param name="waitForWindowStatus">
        ///     Wait until the javascript window.status has this value before
        ///     rendering the PDF
        /// </param>
        /// <param name="waitForWindowsStatusTimeout"></param>
        /// <param name="conversionTimeout">
        ///     An conversion timeout in milliseconds, if the conversion fails
        ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
        /// </param>
        /// <param name="logStream">
        ///     When set then this will give a logging for each conversion. Use the log stream
        ///     option in the constructor if you want one log for all conversions
        /// </param>
        /// <exception cref="ConversionTimedOutException">
        ///     Raised when <see cref="conversionTimeout" /> is set and the
        ///     conversion fails to finish in this amount of time
        /// </exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void ConvertToPdf(ConvertUri inputUri,
            string outputFile,
            PageSettings pageSettings,
            string waitForWindowStatus = "",
            int waitForWindowsStatusTimeout = 60000,
            int? conversionTimeout = null,
            Stream logStream = null)
        {
            CheckIfOutputFolderExists(outputFile);
            using (var memoryStream = new MemoryStream())
            {
                ConvertToPdf(inputUri, memoryStream, pageSettings, waitForWindowStatus,
                    waitForWindowsStatusTimeout, conversionTimeout, logStream);

                using (var fileStream = File.Open(outputFile, FileMode.Create))
                {
                    memoryStream.Position = 0;
                    memoryStream.CopyTo(fileStream);
                }

                WriteToLog($"PDF written to output file '{outputFile}'");
            }
        }

        ///// <summary>
        /////     Converts the given <see paramref name="inputFile" /> to JPG
        ///// </summary>
        ///// <param name="inputFile">The input file to convert to PDF</param>
        ///// <param name="outputFile">The output file</param>
        ///// <param name="pageSettings"><see cref="PageSettings"/></param>
        ///// <returns>The filename with full path to the generated PNG</returns>
        ///// <exception cref="DirectoryNotFoundException"></exception>
        //public void ConvertToPng(string inputFile, string outputFile, PageSettings pageSettings)
        //{
        //    CheckIfOutputFolderExists(outputFile);
        //    _communicator.NavigateTo(new Uri("file://" + inputFile), TODO);
        //    SetDefaultArgument("--screenshot", Path.ChangeExtension(outputFile, ".png"));
        //}

        ///// <summary>
        /////     Converts the given <paramref name="inputUri" /> to JPG
        ///// </summary>
        ///// <param name="inputUri">The webpage to convert</param>
        ///// <param name="outputFile">The output file</param>
        ///// <returns>The filename with full path to the generated PNG</returns>
        ///// <exception cref="DirectoryNotFoundException"></exception>
        //public void ConvertToPng(Uri inputUri, string outputFile)
        //{
        //    CheckIfOutputFolderExists(outputFile);
        //    _communicator.NavigateTo(inputUri, TODO);
        //    SetDefaultArgument("--screenshot", Path.ChangeExtension(outputFile, ".png"));
        //}

        #endregion
    }
}
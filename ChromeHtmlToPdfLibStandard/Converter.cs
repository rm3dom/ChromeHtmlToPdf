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
using System.IO;
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

        private readonly ChromeProcess _chromeProcess;

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
        ///     When set then this folder is used for temporary files
        /// </summary>
        private string _tempDirectory;


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
            _chromeProcess.EnsureRunning();
            _chromeProcess = chrome;
            _logStream = logStream;
            _browser = new Browser(chrome.InstanceHandle);
        }


        /// <summary>
        ///     An unique id that can be used to identify the logging of the converter when
        ///     calling the code from multiple threads and writing all the logging to the same file
        /// </summary>
        public string InstanceId { get; set; }


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

        #region Dispose

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

        /// <summary>
        ///     Destructor
        /// </summary>
        ~Converter()
        {
            Dispose();
        }


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
                if (!File.Exists(inputUri.OriginalString))
                    throw new FileNotFoundException($"The file '{inputUri.OriginalString}' does not exists");

            try
            {
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

        #endregion
    }
}
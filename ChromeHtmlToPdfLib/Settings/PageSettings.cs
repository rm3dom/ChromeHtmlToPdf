﻿//
// PageSettings.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017 Magic-Sessions. (www.magic-sessions.com)
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using ChromeHtmlToPdfLib.Enums;

namespace ChromeHtmlToPdfLib.Settings
{
    #region PaperFormats
    #endregion

    /// <summary>
    /// The page settings to use when converting to PDF
    /// </summary>
    public class PageSettings
    {
        #region Properties
        /// <summary>
        ///     Paper orientation. Defaults to false.
        /// </summary>
        public bool Landscape { get; set; }

        /// <summary>
        ///     Display header and footer. Defaults to false.
        /// </summary>
        public bool DisplayHeaderFooter { get; set; }

        /// <summary>
        ///     Print background graphics. Defaults to false.
        /// </summary>
        public bool PrintBackground { get; set; }

        /// <summary>
        ///     Scale of the webpage rendering. Defaults to 1.
        /// </summary>
        public double Scale { get; set; }

        /// <summary>
        ///     Paper width in inches. Defaults to 8.5 inches.
        /// </summary>
        public double PaperWidth { get; set; }

        /// <summary>
        ///     Paper height in inches. Defaults to 11 inches.
        /// </summary>
        public double PaperHeight { get; set; }

        /// <summary>
        ///     Top margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        public double MarginTop { get; set; }

        /// <summary>
        ///     Bottom margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        public double MarginBottom { get; set; }

        /// <summary>
        ///     Left margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        public double MarginLeft { get; set; }

        /// <summary>
        ///     Right margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        public double MarginRight { get; set; }

        /// <summary>
        ///     Paper ranges to print, e.g., '1-5, 8, 11-13'. Defaults to the empty string, which means print all pages.
        /// </summary>
        public string PageRanges { get; set; }

        /// <summary>
        ///     Whether to silently ignore invalid but successfully parsed page ranges, such as '3-2'. Defaults to false.
        /// </summary>
        public bool IgnoreInvalidPageRanges { get; set; }
        #endregion

        #region PageSettings
        /// <summary>
        /// Makes this object and sets all the settings to it's default values
        /// </summary>
        /// <remarks>
        /// Default paper settings are set to <see cref="PaperFormats.A4"/>
        /// </remarks>
        public PageSettings()
        {
            ResetToDefaultSettings();
        }

        /// <summary>
        /// Makes this object and sets all the settings to it's default values
        /// </summary>
        /// <remarks>
        /// Default paper settings are set to <see cref="PaperFormats.A4"/>
        /// </remarks>
        /// <param name="paperFormat"></param>
        public PageSettings(PaperFormats paperFormat)
        {
            ResetToDefaultSettings();
            SetPaperFormat(paperFormat);
        }
        #endregion

        #region ResetToDefaultSettings
        /// <summary>
        /// Resets the <see cref="PageSettings"/> to it's default settings
        /// </summary>
        public void ResetToDefaultSettings()
        {
            Landscape = false;
            DisplayHeaderFooter = false;
            PrintBackground = false;
            Scale = 1.0;
            SetPaperFormat(PaperFormats.A4);
            MarginTop = 0.4;
            MarginBottom = 0.4;
            MarginLeft = 0.4;
            MarginRight = 0.4;
            PageRanges = string.Empty;
        }
        #endregion

        #region SetPaperFormat
        /// <summary>
        /// Set the given <paramref name="paperFormat"/>
        /// </summary>
        /// <param name="paperFormat"><see cref="PaperFormats"/></param>
        private void SetPaperFormat(PaperFormats paperFormat)
        {
            switch (paperFormat)
            {
                case PaperFormats.Letter:
                    PaperWidth = 8.5;
                    PaperHeight = 11;
                    break;
                case PaperFormats.Legal:
                    PaperWidth = 8.5;
                    PaperHeight = 14;
                    break;
                case PaperFormats.Tabloid:
                    PaperWidth = 11;
                    PaperHeight = 17;
                    break;
                case PaperFormats.Ledger:
                    PaperWidth = 17;
                    PaperHeight = 11;
                    break;
                case PaperFormats.A0:
                    PaperWidth = 33.1;
                    PaperHeight = 46.8;
                    break;
                case PaperFormats.A1:
                    PaperWidth = 23.4;
                    PaperHeight = 33.1;
                    break;
                case PaperFormats.A2:
                    PaperWidth = 16.5;
                    PaperHeight = 23.4;
                    break;
                case PaperFormats.A3:
                    PaperWidth = 11.7;
                    PaperHeight = 16.5;
                    break;
                case PaperFormats.A4:
                    PaperWidth = 8.27;
                    PaperHeight = 11.7;
                    break;
                case PaperFormats.A5:
                    PaperWidth = 5.83;
                    PaperHeight = 8.27;
                    break;
                case PaperFormats.A6:
                    PaperWidth = 4.13;
                    PaperHeight = 5.83;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paperFormat), paperFormat, null);
            }
        }
        #endregion
    }
}
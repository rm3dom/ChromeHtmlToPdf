﻿//
// Extensions.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChromeHtmlToPdfLib.Exceptions;

namespace ChromeHtmlToPdfLib.Helpers
{
    internal static class Extensions
    {
        #region Contains

        /// <summary>
        ///     Returns <c>true</c> when the list containts the given <paramref name="source" />
        /// </summary>
        /// <param name="source"></param>
        /// <param name="value">The value to check if it exists in the list</param>
        /// <param name="comparison">
        ///     <see cref="StringComparison" />
        /// </param>
        /// <returns></returns>
        public static bool Contains(this List<string> source, string value, StringComparison comparison)
        {
            return
                source != null &&
                !string.IsNullOrEmpty(value) &&
                source.Any(x => string.Compare(x, value, comparison) == 0);
        }

        #endregion

        #region Timeout

        /// <summary>
        ///     A timeout for a task
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task"></param>
        /// <param name="timeout">The timeout in millisecons</param>
        /// <returns></returns>
        public static async Task<TResult> Timeout<TResult>(this Task<TResult> task, int timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask != task) throw new TaskTimedOutException("The task timed out");
                timeoutCancellationTokenSource.Cancel();
                return await task;
            }
        }

        #endregion
    }
}
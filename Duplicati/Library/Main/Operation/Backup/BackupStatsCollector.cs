// Copyright (C) 2025, The Duplicati Team
// https://duplicati.com, hello@duplicati.com
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Duplicati.Library.Main.Operation.Backup
{
    /// <summary>
    /// Asynchronous interface that ensures all stat requests
    /// are performed in a sequential manner
    /// </summary>
    internal class BackupStatsCollector
    {
        private readonly BackupResults m_res;
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public BackupStatsCollector(BackupResults res)
        {
            m_res = res;
        }

        private async Task WithLock(Action action)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                action();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public Task AddOpenedFile(long size)
        {
            return WithLock(() =>
            {
                m_res.SizeOfOpenedFiles += size;
                m_res.OpenedFiles++;
            });
        }

        public Task AddTimestampChangedFile()
        {
            return WithLock(() =>
            {
                m_res.TimestampChangedFiles++;
            });
        }

        public Task AddAddedFile(long size)
        {
            return WithLock(() =>
            {
                m_res.SizeOfAddedFiles += size;
                m_res.AddedFiles++;
            });
        }

        public Task AddModifiedFile(long size)
        {
            return WithLock(() =>
            {
                m_res.SizeOfModifiedFiles += size;
                m_res.ModifiedFiles++;
            });
        }

        public Task AddExaminedFile(long size)
        {
            return WithLock(() =>
            {
                m_res.SizeOfExaminedFiles += size;
                m_res.ExaminedFiles++;
            });
        }
    }
}


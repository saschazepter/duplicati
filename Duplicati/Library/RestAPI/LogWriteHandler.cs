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
using System.Linq;
using Duplicati.Library.Logging;
using System.Collections.Generic;
using Duplicati.Library.Interface;
using Duplicati.Library.Main;

namespace Duplicati.Server
{
    /// <summary>
    /// Class that handles logging from the server, 
    /// and provides an entry point for the runner
    /// to redirect log output to a file
    /// </summary>
    public class LogWriteHandler : ILogDestination, IDisposable
    {
        /// <summary>
        /// The number of messages to keep when inactive
        /// </summary>
        private const int INACTIVE_SIZE = 30;
        /// <summary>
        /// The number of messages to keep when active
        /// </summary>
        private const int ACTIVE_SIZE = 5000;

        /// <summary>
        /// The context key used for conveying the backup ID
        /// </summary>
        public const string LOG_EXTRA_BACKUPID = "BackupID";
        /// <summary>
        /// The context key used for conveying the task ID
        /// </summary>
        public const string LOG_EXTRA_TASKID = "TaskID";

        /// <summary>
        /// Represents a single log event
        /// </summary>
        public struct LogEntry
        {
            /// <summary>
            /// A unique ID that sequentially increments
            /// </summary>
            private static long _id;

            /// <summary>
            /// The time the message was logged
            /// </summary>
            public readonly DateTime When;

            /// <summary>
            /// The ID assigned to the message
            /// </summary>
            public readonly long ID;

            /// <summary>
            /// The logged message
            /// </summary>
            public readonly string Message;

            /// <summary>
            /// The log tag
            /// </summary>
            public readonly string Tag;

            /// <summary>
            /// The message ID
            /// </summary>
            public readonly string MessageID;

            /// <summary>
            /// The message ID
            /// </summary>
            public readonly string ExceptionID;

            /// <summary>
            /// The message type
            /// </summary>
            public readonly LogMessageType Type;

            /// <summary>
            /// Exception data attached to the message
            /// </summary>
            public readonly Exception Exception;

            /// <summary>
            /// The backup ID, if any
            /// </summary>
            public readonly string BackupID;

            /// <summary>
            /// The task ID, if any
            /// </summary>
            public readonly string TaskID;

            /// <summary>
            /// Initializes a new instance of the <see cref="Duplicati.Server.LogWriteHandler+LogEntry"/> struct.
            /// </summary>
            /// <param name="entry">The log entry to store</param>
            public LogEntry(Duplicati.Library.Logging.LogEntry entry)
            {
                this.ID = System.Threading.Interlocked.Increment(ref _id);
                this.When = entry.When;
                this.Message = entry.FormattedMessage;
                this.Type = entry.Level;
                this.Exception = entry.Exception;
                this.Tag = entry.FilterTag;
                this.MessageID = entry.Id;
                this.BackupID = entry[LOG_EXTRA_BACKUPID];
                this.TaskID = entry[LOG_EXTRA_TASKID];

                if (entry.Exception == null)
                    this.ExceptionID = null;
                else if (entry.Exception is UserInformationException exception)
                    this.ExceptionID = exception.HelpID;
                else
                    this.ExceptionID = entry.Exception.GetType().FullName;

            }
        }

        /// <summary>
        /// Basic implementation of a ring-buffer
        /// </summary>
        private class RingBuffer<T> : IEnumerable<T>
        {
            private readonly T[] m_buffer;
            private int m_head;
            private int m_tail;
            private int m_length;
            private int m_key;
            private readonly object m_lock = new object();

            public RingBuffer(int size, IEnumerable<T> initial = null)
            {
                m_buffer = new T[size];
                if (initial != null)
                    foreach (var t in initial)
                        this.Enqueue(t);
            }

            public int Length { get { return m_length; } }

            public void Enqueue(T item)
            {
                lock (m_lock)
                {
                    m_key++;
                    m_buffer[m_head] = item;
                    m_head = (m_head + 1) % m_buffer.Length;
                    if (m_length == m_buffer.Length)
                        m_tail = (m_tail + 1) % m_buffer.Length;
                    else
                        m_length++;
                }
            }

            #region IEnumerable implementation
            public IEnumerator<T> GetEnumerator()
            {
                var k = m_key;
                for (var i = 0; i < m_length; i++)
                    if (m_key != k)
                        throw new InvalidOperationException("Buffer was modified while reading");
                    else
                        yield return m_buffer[(m_tail + i) % m_buffer.Length];
            }
            #endregion
            #region IEnumerable implementation
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            #endregion

            public T[] FlatArray(Func<T, bool> filter = null)
            {
                lock (m_lock)
                    if (filter == null)
                        return this.ToArray();
                    else
                        return this.Where(filter).ToArray();
            }

            public int Size { get { return m_buffer.Length; } }
        }

        private readonly DateTime[] m_timeouts;
        private readonly object m_lock = new object();
        private volatile bool m_anytimeouts = false;
        private RingBuffer<LogEntry> m_buffer;


        private readonly ControllerMultiLogTarget m_target = new ControllerMultiLogTarget(null, LogMessageType.Warning, null);
        private LogMessageType m_logLevel;

        public LogWriteHandler()
        {
            var fields = Enum.GetValues(typeof(LogMessageType));
            m_timeouts = new DateTime[fields.Length];
            m_buffer = new RingBuffer<LogEntry>(INACTIVE_SIZE);
        }

        public void RenewTimeout(LogMessageType type)
        {
            lock (m_lock)
            {
                m_timeouts[(int)type] = DateTime.Now.AddSeconds(30);
                m_anytimeouts = true;
                if (m_buffer == null || m_buffer.Size == INACTIVE_SIZE)
                    m_buffer = new RingBuffer<LogEntry>(ACTIVE_SIZE, m_buffer);
            }
        }

        public void SetServerFile(string path, LogMessageType level)
        {
            var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            m_target.AddTarget(new StreamLogDestination(path), level, null);
            UpdateLogLevel();
        }

        public void AppendLogDestination(ILogDestination destination, LogMessageType level)
        {
            m_target.AddTarget(destination, level, null);
            UpdateLogLevel();
        }

        public LogEntry[] AfterTime(DateTime offset, LogMessageType level)
        {
            RenewTimeout(level);
            UpdateLogLevel();

            offset = offset.ToUniversalTime();
            lock (m_lock)
            {
                if (m_buffer == null)
                    return new LogEntry[0];

                return m_buffer.FlatArray((x) => x.When > offset && x.Type >= level);
            }
        }

        public LogEntry[] AfterID(long id, LogMessageType level, int pagesize)
        {
            RenewTimeout(level);
            UpdateLogLevel();

            lock (m_lock)
            {
                if (m_buffer == null)
                    return new LogEntry[0];

                var buffer = m_buffer.FlatArray((x) => x.ID > id && x.Type >= level);
                // Return the <page_size> newest entries
                if (buffer.Length > pagesize)
                {
                    var index = buffer.Length - pagesize;
                    return buffer.Skip(index).Take(pagesize).ToArray();
                }
                else
                {
                    return buffer;
                }
            }
        }

        private int[] GetActiveTimeouts()
        {
            var i = 0;
            return (from n in m_timeouts
                    let ix = i++
                    where n > DateTime.Now
                    select ix).ToArray();
        }

        private void UpdateLogLevel()
        {
            m_logLevel =
                (LogMessageType)GetActiveTimeouts().Append((int)m_target.MinimumLevel).Min();
        }


        #region ILog implementation

        public void WriteMessage(Duplicati.Library.Logging.LogEntry entry)
        {
            if (entry.Level < m_logLevel)
                return;

            try
            {
                m_target.WriteMessage(entry);
            }
            catch
            {
            }

            lock (m_lock)
            {
                if (m_anytimeouts)
                {
                    var q = GetActiveTimeouts();

                    if (q.Length == 0)
                    {
                        UpdateLogLevel();
                        m_anytimeouts = false;
                        if (m_buffer == null || m_buffer.Size != INACTIVE_SIZE)
                            m_buffer = new RingBuffer<LogEntry>(INACTIVE_SIZE, m_buffer);

                    }
                }

                if (m_buffer != null)
                    m_buffer.Enqueue(new LogEntry(entry));
            }

        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            m_target.Dispose();
        }

        #endregion
    }
}


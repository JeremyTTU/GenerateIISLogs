using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenerateIISLogs.Utility
{
    public class ProgressControl
    {
        public string Header { get; private set; }
        public long ItemsTotal = 0;
        public long ItemsDone = 0;
        public long ItemsFail = 0;
        public long ItemsLeft => ItemsTotal - ItemsDone;

        public bool Enabled = false;

        public ProgressControl(string header, int itemsTotal)
        {
            Header = header;
            ItemsTotal = itemsTotal;
            Enabled = true;
        }

        public void Reset(string header, int itemsTotal)
        {
            Reset();
            Header = header;
            ItemsTotal = itemsTotal;
            Enabled = true;
        }

        public void IncrementDone()
        {
            Interlocked.Increment(ref ItemsDone);
        }

        public string GetStatus()
        {
            return Enabled ? $"{Header}: {ItemsDone} of {ItemsTotal}" : string.Empty;
        }

        internal void Reset()
        {
            ItemsTotal = 0;
            ItemsDone = 0;
            ItemsFail = 0;
        }

        internal void SetTotal(int length)
        {
            ItemsTotal = length;
        }
    }
}

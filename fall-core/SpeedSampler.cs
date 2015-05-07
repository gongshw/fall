using System;
using System.Collections.Generic;

namespace fall_core
{

    class SpeedSample
    {
        public DateTime time;
        public long size;
        public SpeedSample(DateTime time, long size)
        {
            this.time = time;
            this.size = size;
        }
    }

    public class SpeedSampler
    {
        private long samplingRate = 500;
        private int size = 200;
        private LinkedList<SpeedSample> samples = new LinkedList<SpeedSample>();

        private object samplesLock = new object();
        public void RecordNow(long size)
        {
            lock (samplesLock)
            {
                DateTime time = DateTime.Now;
                if (samples.Count == 0 || (time - samples.Last.Value.time).Milliseconds >= this.samplingRate)
                {
                    if (samples.Count >= this.size)
                    {
                        samples.RemoveFirst();
                    }
                    SpeedSample sample = new SpeedSample(DateTime.Now, size);
                    samples.AddLast(sample);
                }
            }
        }

        public double getSpeedIn(long period)
        {
            lock (samplesLock)
            {
                if (samples.Count >= 2)
                {
                    long sizeDiff = 0;
                    long timeSpan = 0;
                    DateTime now = DateTime.Now;
                    long sizeNow = samples.Last.Value.size;
                    LinkedListNode<SpeedSample> node = samples.Last.Previous;
                    do
                    {
                        timeSpan = (long)(now - node.Value.time).TotalMilliseconds;
                        sizeDiff = sizeNow - node.Value.size;
                        node = node.Previous;
                    } while (node != null && timeSpan < period);
                    return sizeDiff / (timeSpan / 1000.0);
                }
                else
                {
                    return 0;
                }
            }
        }

    }
}

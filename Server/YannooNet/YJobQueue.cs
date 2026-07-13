namespace YannooNet
{
    public class YJobQueue
    {
        readonly object _yLock = new object();
        readonly Queue<Action> _yJobQueue = new Queue<Action>();
        bool _flush = false;


        public void Push(Action job)
        {
            bool tempFlush = false;
            lock (_yLock)
            {
                _yJobQueue.Enqueue(job);
                if (_flush == false)
                {
                    _flush = true;
                    tempFlush = true;
                }
            }
            //flush 중이 아니라면, flush 플래그를 켠 스레드가 담당 스레드가 되어 큐가 빌 때까지 dequeue 수행
            if (tempFlush)
                Flush();
        }

        public void Flush()
        {
            while (true)
            {
                Action job;
                lock (_yLock)
                {
                    if (_yJobQueue.Count == 0)
                    {
                        _flush = false;
                        return;
                    }
                    job = _yJobQueue.Dequeue();
                }
                job.Invoke();
            }
        }

    }
}
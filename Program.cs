namespace lab7
{
    public class procEventArgs : EventArgs
    {
        public int id { get; set; }
        public int threadId { get; set; }
        public int clientId { get; set; }
        public int callTime { get; set; }
    }
    
    public class Client
    {
        private readonly Server serv;
        private int id;
        public event EventHandler<procEventArgs> request;
        public Client(in Server serv) {
            this.serv = serv;
            this.id = serv.getClientId();
            this.request += serv.proc;
        }
        protected virtual void onProc(procEventArgs e)
        {
            EventHandler<procEventArgs> handler = request;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public void callServ(int callTime)
        {
            procEventArgs e = new procEventArgs();
            e.clientId = id;
            e.callTime = callTime;
            onProc(e);
        }
    }
    public class Server
    {
        private int reqCount;
        private int reqProcCount;
        private int reqRejCount;
        private int lastClientId;
        object threadLock = new object();
        private int threadsCount;
        public int getReqCount() {
            return reqCount;
        }
        public int getReqProcCount() {
            return reqProcCount;
        }
        public int getReqRejCount() {
            return reqRejCount;
        }

        struct PoolRecord
        {
            public Thread thread; // объект потока
            public bool in_use; // флаг занятости
        }
        PoolRecord[] pool;

        public Server(int threadsCount) {
            reqCount = 0;
            reqProcCount = 0;
            reqRejCount = 0;
            lastClientId = 0;
            this.threadsCount = threadsCount;
            pool = new PoolRecord[threadsCount];
        }
        public void proc(object? sender, procEventArgs e)
        {
            lock(threadLock) {
                e.id = reqCount;
                reqCount++;
                for(int i = 0; i < threadsCount; i++) {
                    if(!pool[i].in_use) {
                        pool[i].in_use = true;
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        e.threadId = i;
                        pool[i].thread.Start(e);
                        reqProcCount++;
                        Console.WriteLine("Заявка с номером: {0} от клиента: {1} начата", e.id, e.clientId);
                        return;
                    }
                }
                Console.WriteLine("Заявка с номером: {0} от клиента: {1} отклонена", e.id, e.clientId);
                reqRejCount++;
            }
        }
        private void Answer(object? e) {
            if(e!=null) {
                procEventArgs arg =((procEventArgs)e);
                Thread.Sleep(arg.callTime);
                pool[arg.threadId].in_use = false;
                Console.WriteLine("Заявка с номером: {0} от клиента: {1} завершена", arg.id, arg.clientId);
            }
        }
        
        public int getClientId() {
            return lastClientId++;
        }
    }
    public class Program
    {
        static void Main(string[] args)
        {
            int threadsCount = 4;
            int clientsCount = 3;
            int reqCount = 20;
            int reqIntensity = 35;//per sec 
            int servIntensity = 6;//per sec 
            int sec = 100;
            int reqTime = sec / reqIntensity;
            int servTime = sec / servIntensity;
            Console.WriteLine($"Потоков: {threadsCount}");
            Console.WriteLine($"Клиентов: {clientsCount}");
            Console.WriteLine($"Запросов: {reqCount}");
            Console.WriteLine($"Отправляется {reqIntensity} запросов в секунду");
            Console.WriteLine($"Исполняется {servIntensity} запросов в секунду каждым потоком");
            Console.WriteLine("");
            Server s = new Server(threadsCount);
            Client[] c = new Client[clientsCount];
            for (int i = 0; i < clientsCount; i++)
                c[i] = new Client(s);
        
            for (int i = 0; i < reqCount; i++)
            {
                c[i%clientsCount].callServ(servTime);
                Thread.Sleep(reqTime);
            }
            Thread.Sleep(servTime);
            var streamIntensivity = (double)reqIntensity/ servIntensity;
            var idleProb = calculateIdleProb(threadsCount,streamIntensivity);
            var rejProb = calculaterejProb(threadsCount,streamIntensivity,idleProb);
            var relativeThroughput = 1 - rejProb;
            var absoluteThroughput = relativeThroughput * reqIntensity;
            var avgBusyChannels = absoluteThroughput / servIntensity;
            Console.WriteLine("");
            Console.WriteLine($"Отправлено запросов: {s.getReqCount()}");
            Console.WriteLine($"Выполнено запросов: {s.getReqProcCount()}");
            Console.WriteLine($"Отклонено запросов: {s.getReqRejCount()}");
            Console.WriteLine("");
    
            Console.WriteLine($"Теоретическая приведенная интенсивность потока заявок: {streamIntensivity}");
            Console.WriteLine($"Теоретическая вероятность простоя системы: {idleProb}");
            Console.WriteLine($"Теоретическая вероятность отказа системы: {rejProb}");
            Console.WriteLine($"Теоретическая относительная пропускная способность: {relativeThroughput}");
            Console.WriteLine($"Теоретическая абсолютная пропускная способность: {absoluteThroughput}");
            Console.WriteLine($"Теоретическая среднее число занятых каналов: {avgBusyChannels}");
            Console.WriteLine("");

            streamIntensivity = (double)s.getReqCount()*threadsCount/ s.getReqProcCount();
            idleProb = calculateIdleProb(threadsCount,streamIntensivity);
            rejProb = calculaterejProb(threadsCount,streamIntensivity,idleProb);
            relativeThroughput = 1 - rejProb;
            absoluteThroughput = relativeThroughput * reqIntensity;
            avgBusyChannels = absoluteThroughput / servIntensity;
            Console.WriteLine($"Практическая приведенная интенсивность потока заявок: {streamIntensivity}");
            Console.WriteLine($"Практическая вероятность простоя системы: {idleProb}");
            Console.WriteLine($"Практическая вероятность отказа системы: {rejProb}");
            Console.WriteLine($"Практическая относительная пропускная способность: {relativeThroughput}");
            Console.WriteLine($"Практическая абсолютная пропускная способность: {absoluteThroughput}");
            Console.WriteLine($"Практическая среднее число занятых каналов: {avgBusyChannels}");
            Console.WriteLine("-----------------------------------------------------------------");
        }
        
        static double calculateIdleProb(int threadsCount, double streamIntensivity)
        {
            double idleProb = 0;
            for (int i = 0; i <= threadsCount; i++)
            {
                idleProb = idleProb + Math.Pow(streamIntensivity, i) / Factorial(i);
            }
            return (1 / idleProb);
        }
        
        static double calculaterejProb(int threadsCount, double streamIntensivity, double idleProb)
        {
            return Math.Pow(streamIntensivity, threadsCount) / Factorial(threadsCount) * idleProb;
        }
        
        static double Factorial(double n)
        {
            double factorial = 1;
            for (int i = 1; i <= n; i++)
                factorial = factorial * i;
            return factorial;
        }
    }
}

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
        public void proc(object sender, procEventArgs e)
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
        private void Answer(object e) {
            procEventArgs arg =((procEventArgs)e);
            Thread.Sleep(arg.callTime);
            pool[arg.threadId].in_use = false;
            Console.WriteLine("Заявка с номером: {0} от клиента: {1} завершена", arg.id, arg.clientId);
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
            int reqCount= 20;
            int reqIntensity = 10;//per sec 
            int servIntensity = 2;//per sec 
            int reqTime = 1000 / reqIntensity;
            int servTime = 1000 / servIntensity;
            Server s = new Server(threadsCount);
            Client c = new Client(s);
            for (int i = 0; i < reqCount; i++)
            {
                c.callServ(servTime);
                Thread.Sleep(reqTime);
            }
        }
    }
}

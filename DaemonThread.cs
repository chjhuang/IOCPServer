using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace IOCPServer
{
    class DaemonThread : Object
    {
        private Thread m_thread;
        private IOCPServer m_asyncSocketServer;

        public DaemonThread(IOCPServer asyncSocketServer)
        {
            m_asyncSocketServer = asyncSocketServer;
            m_thread = new Thread(DaemonThreadStart);
            m_thread.Start();
        }
        private void TimeOutTest()
        {
            SocketAsyncEventArgs[] userTokenArray = null;
            m_asyncSocketServer._ObjectList.CopyList(ref userTokenArray);

            for (int i = 0; i < userTokenArray.Length; i++)
            {
                if (!m_thread.IsAlive)
                    break;
                try
                {
                    Console.WriteLine("连接{0}延时时间为{1}",((Socket)userTokenArray[i].UserToken).RemoteEndPoint,((Socket)userTokenArray[i].UserToken).ReceiveTimeout);
                    if (((Socket)userTokenArray[i].UserToken).SendTimeout == 0 &&
                        ((Socket)userTokenArray[i].UserToken).ReceiveTimeout-- <= 0) //超时Socket断开
                    {                                                 
                        lock (userTokenArray[i])
                        {    
                            m_asyncSocketServer.CloseClientSocket(userTokenArray[i]);
                        }
                    }
                }
                catch (Exception E)
                {

                }
            }
        }
        public void DaemonThreadStart()
        {
            while (m_thread.IsAlive)
            {
                TimeOutTest();

                for (int i = 0; i <2 * 1000 / 10; i++) //每2秒检测一次
                {
                    if (!m_thread.IsAlive)
                        break;
                    Thread.Sleep(10);
                }
            }
        }

        public void Close()
        {
            m_thread.Abort();
            m_thread.Join();
        }
    }
}

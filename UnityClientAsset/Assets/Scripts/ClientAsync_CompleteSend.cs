using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using TMPro;
using System.Linq;

public class ClientAsync_CompleteSend : MonoBehaviour
{
    Socket socket;
    public TMP_InputField tMP_InputField;
    public TMP_Text tMP_Text;


    //接收缓冲区
    byte[] readBuff = new byte[1024];

    //接收缓冲区的数据长度
    int buffCount = 0;

    string recvStr = " ";

    //点击链接按钮
    public void Connection()
    {
        //Socket
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //Async Connect
        // socket.BeginConnect("127.0.0.1", 1234, ConnectCallBack, socket);

        //精简代码：使用同步connect
        socket.Connect("127.0.0.1", 1234);
        socket.BeginReceive(readBuff, buffCount, 1024 - buffCount, 0, ReceiveCallBack, socket);


    }

    //Receive回调
    public void ReceiveCallBack(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;

            //获取接收数据长度
            int count = socket.EndReceive(ar);
            buffCount += count;

            //处理二进制消息
            OnReceiveData();

            ////---------------模拟粘包--------------
            ////等待
            //System.Threading.Thread.Sleep(1000 * 30);
            ////-------------------------------------

            //继续接收数据包
            socket.BeginReceive(readBuff, buffCount, 1024 - buffCount, 0, ReceiveCallBack, socket);
        }
        catch (SocketException ex)
        {
            Debug.Log("Socket Receive Fail");
            Debug.Log(ex);
        }

    }

    public void OnReceiveData()
    {
        Debug.Log("[Recv 1] buff Count = " + buffCount);
        Debug.Log("[Recv 2] readbuff = " + BitConverter.ToString(readBuff));

        //消息长度
        //1. 如果容量小于消息头的长度（2），不处理
        if (buffCount <= 2)
        {
            return;
        }

        //大于二的部分是消息体
        Int16 bodyLength = BitConverter.ToInt16(readBuff, 0);
        Debug.Log("[Recv 3] bodyLength = " + bodyLength);

        //2. 如果容量小于消息头加消息体，不处理
        if (buffCount < 2 + bodyLength)
        {
            return;
        }

        //3. 缓冲区长度大于一条完整信息
        string s = System.Text.Encoding.UTF8.GetString(readBuff, 2, buffCount);
        Debug.Log("[Recv 4] s = " + s);

        //3.1 更新缓冲区
        int start = 2 + bodyLength;
        int count = buffCount - start;
        Array.Copy(readBuff, start, readBuff, 0, count);
        buffCount -= start;

        Debug.Log("[Recv 5] buffCount = " + buffCount);

        //消息处理
        recvStr = s + "\n" + recvStr;

        //继续读取消息
        OnReceiveData();




    }

    // //Connect 回调
    // public void ConnectCallBack(IAsyncResult ar)
    // {
    //     try
    //     {
    //         Socket socket = (Socket) ar.AsyncState;
    //         socket.EndConnect(ar);
    //         Debug.Log("Socket Connect Succ");

    //         //Receive the message
    //         socket.BeginReceive(readBuff, 0, 1024, 0, ReceiveCallBack, socket);

    //     }
    //     catch (SocketException ex)
    //     {
    //         Debug.Log("Socket Connect Faill" + ex.ToString());
    //     }
    // }



    //-------------------------完整发送-------------------------------------
    //定义
    Queue<ByteArray> writeQueue = new Queue<ByteArray>();

    //点击发送按钮   
    public void Send()
    {
        //Send
        string sendStr = tMP_InputField.text;

        //组装协议
        byte[] bodyBytes = System.Text.Encoding.Default.GetBytes(sendStr);
        Int16 len = (Int16)bodyBytes.Length;
        byte[] lenBytes = BitConverter.GetBytes(len);
        byte[] sendBytes = lenBytes.Concat(bodyBytes).ToArray();

        //---------------------------------------------------

        //初始化ByteArray 然后 入队
        ByteArray ba = new ByteArray(sendBytes);
        writeQueue.Enqueue(ba);

        //当队中只有一条待发消息时候才发送
        if (writeQueue.Count == 1) 
        {
            socket.BeginSend(ba.bytes, ba.readIdx, ba.length, 0, SendCallback, socket);

        }

        //-----------------------------------------------------

    }


    public void SendCallback(IAsyncResult ar)
    {
        //获取state, 获取EndSend 的处理
        Socket socket = (Socket)ar.AsyncState;
        int count = socket.EndSend(ar);

        //判断书否发送完整
        ByteArray ba = writeQueue.First();
        ba.readIdx += count;
        if (ba.length == 0) 
        {
            writeQueue.Dequeue();
            ba = writeQueue.First();
        }

        //如果消息发送不完整，或存在第二条数据
        if (ba != null) 
        {
            socket.BeginSend(ba.bytes, ba.readIdx, ba.length, 0, SendCallback, socket);
        }


    }
    //---------------------------------------------------------------------------------

    public void Update()
    {
        tMP_Text.text = recvStr;
    }

}

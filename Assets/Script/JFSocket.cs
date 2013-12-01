using UnityEngine;
using System.Collections;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class JFSocket {

	//服务器IP地址.
	string ServerIP = "127.0.0.1";
	int ServerPoint = 8885;
	//服务器端口.

	//Socket客户端对象.
	private Socket clientSocket;
	//数据包表.
	public List<JFPackage.ComanndPackage> comanndPackages;

	private static JFSocket instance;
	public static JFSocket GetInstance()
	{
		if(instance == null)
		{
			instance = new JFSocket();
		}
		return instance;
	}

	JFSocket()
	{
		//创建Socket对象.
		clientSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
		IPAddress ipAddress = IPAddress.Parse (ServerIP);
		IPEndPoint ipEndPoint = new IPEndPoint(ipAddress,ServerPoint);
		//连接成功调用的方法.
		IAsyncResult result = clientSocket.BeginConnect(ipEndPoint,new AsyncCallback (ConnectCallback),clientSocket);
		//超时处理.
		bool success = result.AsyncWaitHandle.WaitOne(5000,true);
		if(!success)
		{
			//超时.
			Closed();
			Debug.Log("连接超时");
		}
		else
		{
			//开启线程接受服务器端数据.
			comanndPackages = new List<JFPackage.ComanndPackage>();
			Thread thread = new Thread(new ThreadStart(ReceiveSorket));
			thread.IsBackground = true;
			thread.Start();
		}
	}

	private void ConnectCallback(IAsyncResult asyncConnect)
	{
		Debug.Log("连接成功");
	}

	//接受服务器数据.
	private void ReceiveSorket ()
	{
		while (true)
		{
			//与服务器失去连接.
			if(!clientSocket.Connected)
			{
				Debug.Log("服务器连接中断");
				clientSocket.Close();
				break;
			}

			try
			{
				//接受数据保存在bytes中
				byte[] bytes = new byte[4096];
				int i = clientSocket.Receive(bytes);
				if(i <= 0)
				{
					clientSocket.Close();
					break;
				}
				//包头判定，长度为2
				if(bytes.Length > 2)
				{
					SplitPackage(bytes,0);
				}
				else
				{
					Debug.Log("包头长度<2");
				}
			}
			catch (Exception e)
			{
				Debug.Log("服务器错误：" + e);
				clientSocket.Close();
				break;
			}
		}
	}

	void SplitPackage (byte[] bytes, int index)
	{
		//拆包处理.
		while(true)
		{
			//包头数据.
			byte[] head = new byte[2];
			int headLenghtIndex = index + 2;
			//提出包头数据
			Array.Copy(bytes,index,head,0,2);
			//计算包的长度
			short length = BitConverter.ToInt16(head,0);
			//当包头长度大于0，则把相应长度的byte数组拷贝出来

			if(length > 0)
			{
				byte[] data = new byte[length];
				Array.Copy(bytes,headLenghtIndex,data,0,length);

				//强制转换
				JFPackage.ComanndPackage cp = new JFPackage.ComanndPackage();
				cp = (JFPackage.ComanndPackage)BytesToStruct(data,cp.GetType());
				Debug.Log(cp.ToString());
				//加入List
				comanndPackages.Add(cp);
				//下一个包
				index = headLenghtIndex + length;
			}
			else
				break;
		}
	}

	/// <summary>
	/// 向服务器发送信息.
	/// </summary>
	/// <param name="str">String.</param>
	public void SendMessage(string str)
	{
		byte[] msg = Encoding.UTF8.GetBytes(str);
		if(!clientSocket.Connected)
		{
			clientSocket.Close();
			return;
		}

		try
		{
			IAsyncResult asyncSend = clientSocket.BeginSend (msg,0,msg.Length,SocketFlags.None,new AsyncCallback (sendCallback),clientSocket);
			bool sucess = asyncSend.AsyncWaitHandle.WaitOne(5000,true);
			if(!sucess)
			{
				clientSocket.Close();
				Debug.Log("发送消息失败，连接超时");
			}
		}
		catch
		{
			Debug.Log("发送消息失败");
		}
	}

	/// <summary>
	/// 向服务器发送数据包
	/// </summary>
	/// <param name="obj">Object.</param>
	public void SendMessage(object obj)
	{
		if(!clientSocket.Connected)
		{
			clientSocket.Close();
			return;
		}

		try
		{
			//得到数据包的长度
			short size = (short)Marshal.SizeOf(obj);
			byte[] head = BitConverter.GetBytes(size);
			//把结构体对象转换成数据包
			byte[] data = structToBytes(obj);
			//合并一个数组
			byte[] newByte = new byte[head.Length + data.Length];
			Array.Copy(head,0,newByte,0,head.Length);
			Array.Copy(data,0,newByte,head.Length,data.Length);

			//计算出新的字节长度
			int length = Marshal.SizeOf(size) + Marshal.SizeOf(obj);

			//向服务端发送数据包
			IAsyncResult asyncSend = clientSocket.BeginSend(newByte,0,length,SocketFlags.None,new AsyncCallback (sendCallback),clientSocket);
			//监测超时
			bool success = asyncSend.AsyncWaitHandle.WaitOne(5000,true);
			if(!success)
			{
				clientSocket.Close();
				Debug.Log("发送消息失败，连接超时");
			}
		}
		catch (Exception e)
		{
			Debug.Log("发送消息失败:" + e);
		}
	}

	public byte[] structToBytes(object structObj)
	{
		int size = Marshal.SizeOf(structObj);
		IntPtr buffer =  Marshal.AllocHGlobal(size);
		byte[] bytes = new byte[size];
		try
		{
			Marshal.StructureToPtr(structObj,buffer,false);
			Marshal.Copy(buffer,bytes,0,size);
		}
		finally
		{
			Marshal.FreeHGlobal(buffer);
		}
		return bytes;
	}

	//字节数组转结构体.
	public object BytesToStruct(byte[] bytes,Type strcutType)
	{
		int size = Marshal.SizeOf(strcutType);
		IntPtr buffer = Marshal.AllocHGlobal(size);
		try
		{
			Marshal.Copy(bytes,0,buffer,size);
		}
		finally
		{
			Marshal.FreeHGlobal(buffer);
		}
		return Marshal.PtrToStructure(buffer,strcutType);
	}

	public void sendCallback(IAsyncResult asyncSend)
	{

	}

	//结束.
	void Closed ()
	{
		if(clientSocket != null && clientSocket.Connected)
		{
			clientSocket.Shutdown(SocketShutdown.Both);
			clientSocket.Close();
		}
		clientSocket = null;
	}
}

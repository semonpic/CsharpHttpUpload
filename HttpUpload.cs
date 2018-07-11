using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
namespace CSharpHttpUpload
{
   public class HttpUpload
    {
       public static readonly int G_BLOCK_LEN_PER = 1 * 1024 * 1024;

       public void UploadBigFile(string address, string upLoadZipFilePath, string upLoadZipFileName)
       {
           FileStream fileStream = fileStream = new FileStream(upLoadZipFilePath, FileMode.Open, FileAccess.Read);
           try
           {
              
               {
                   long FileLength = fileStream.Length;
                   List<long> PkgList = new List<long>();

                   long PkgNum = FileLength / Convert.ToInt64(G_BLOCK_LEN_PER);
                   for (long iIdx = 0; iIdx < FileLength / Convert.ToInt64(G_BLOCK_LEN_PER); iIdx++)
                   {
                       PkgList.Add(Convert.ToInt64(G_BLOCK_LEN_PER));
                   }
                   long s = FileLength % G_BLOCK_LEN_PER;
                   if (s != 0)
                   {
                       PkgList.Add(s);
                   }
                   string md5 = GetMD5HashFromFile(fileStream);
                   fileStream.Close();
                   fileStream = fileStream = new FileStream(upLoadZipFilePath, FileMode.Open, FileAccess.Read);

                   md5 = "file" + md5;// +System.IO.Path.GetExtension(upLoadZipFilePath);
                   for (long iPkgIdx = 0; iPkgIdx < PkgList.Count; iPkgIdx++)
                   {
                       long bufferSize = PkgList[(int)iPkgIdx];
                       byte[] buffer = new byte[bufferSize];

                       int bytesRead = fileStream.Read(buffer, 0, (int)bufferSize);

                       Upload_Request(address, upLoadZipFileName, md5, buffer, bytesRead, iPkgIdx, PkgList.Count);
                   }
               }

           }
           catch (Exception ex)
           {
               throw new Exception("发送文件异常", ex);

           }
           finally
           {
               if (fileStream != null)
               {
                   fileStream.Close();
               }
           }
       }


       string ParameterBuilder(string strBoundary, string key, string value)
       {
           StringBuilder sb = new StringBuilder();
           sb.Append("--");
           sb.Append(strBoundary);
           sb.Append("\r\n");
           sb.Append("Content-Disposition: form-data; name=\"");
           sb.Append(key);
           sb.Append("\"");
           sb.Append("\r\n");
           sb.Append("\r\n");
           sb.Append(value);
           sb.Append("\r\n");
           return sb.ToString();
       }


       /// <summary>

       /// 将本地文件上传到指定的服务器(HttpWebRequest方法)

       /// </summary>

       /// <param name="address">文件上传到的服务器</param>

       /// <param name="fileNamePath">要上传的本地文件（全路径）</param>

       /// <param name="saveName">文件上传后的名称</param>

       /// <returns>服务器反馈信息</returns>
       private JsonResult<UploadStatus> Upload_Request(string address, string filename, string saveName, byte[] buf, int buflen, long chunk, long chunks)
       {
           JsonResult<UploadStatus> tmpmsg = new JsonResult<UploadStatus>();
           //时间戳

           string strBoundary = "----------InfoPush" + DateTime.Now.Ticks.ToString("x");

           byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + strBoundary + "--\r\n");



           //请求头部信息

           StringBuilder sb = new StringBuilder();
           sb.Append(ParameterBuilder(strBoundary, "fileName", saveName));
           sb.Append(ParameterBuilder(strBoundary, "chunk", chunk.ToString()));
           sb.Append(ParameterBuilder(strBoundary, "chunks", chunks.ToString()));

           sb.Append("--");

           sb.Append(strBoundary);

           sb.Append("\r\n");

           sb.Append("Content-Disposition: form-data; name=\"");

           sb.Append("file");

           sb.Append("\"; filename=\"");

           sb.Append(filename);

           sb.Append("\"");

           sb.Append("\r\n");

           sb.Append("Content-Type: ");

           sb.Append("application/octet-stream");

           sb.Append("\r\n");

           sb.Append("\r\n");



           string strPostHeader = sb.ToString();

           byte[] postHeaderBytes = Encoding.UTF8.GetBytes(strPostHeader);



           // 根据uri创建HttpWebRequest对象

           HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(new Uri(address));

           httpReq.Method = "POST";



           //对发送的数据不使用缓存【重要、关键】

           httpReq.AllowWriteStreamBuffering = false;



           //设置获得响应的超时时间（300秒）

           //httpReq.Timeout = 300000;

           httpReq.ContentType = "multipart/form-data; boundary=" + strBoundary;

           long length = buflen + postHeaderBytes.Length + boundaryBytes.Length;

           // long fileLength = fs.Length;

           httpReq.ContentLength = length;

           try
           {
               //每次上传4k

               int bufferLength = 4096;
               byte[] buffer = new byte[bufferLength];

               //开始上传时间

               DateTime startTime = DateTime.Now;

               // int size = r.Read(buffer, 0, bufferLength);

               Stream postStream = httpReq.GetRequestStream();

               //发送请求头部消息

               postStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);

               postStream.Write(buf, 0, buflen);

               //添加尾部的时间戳
               postStream.Write(boundaryBytes, 0, boundaryBytes.Length);
               postStream.Close();
               //获取服务器端的响应

               WebResponse webRespon = httpReq.GetResponse();
               Stream s = webRespon.GetResponseStream();
               StreamReader sr = new StreamReader(s);
               string serverMsg = sr.ReadLine();
               tmpmsg = JsonConvert.DeserializeObject<JsonResult<UploadStatus>>(serverMsg);
               s.Close();
               sr.Close();

           }

           catch (Exception ex)
           {
               tmpmsg.errcode = 1;
               tmpmsg.errmsg = ex.ToString();
           }

           return tmpmsg;

       }


       private static string GetMD5HashFromFile(FileStream file)
       {
           try
           {

               System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
               byte[] retVal = md5.ComputeHash(file);
               file.Close();

               StringBuilder sb = new StringBuilder();
               for (int i = 0; i < retVal.Length; i++)
               {
                   sb.Append(retVal[i].ToString("x2"));
               }
               return sb.ToString();
           }
           catch (Exception ex)
           {
               throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
           }
       }
       private static string GetMD5HashFromFile(string fileName)
       {
           try
           {
               FileStream file = new FileStream(fileName, FileMode.Open);
               return GetMD5HashFromFile(file);

           }
           catch (Exception ex)
           {
               throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
           }
       }
    }
}

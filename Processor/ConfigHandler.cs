using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Web;
using System.Collections;
using System.IO;
using System.Threading;


/*
 * 需要设置的项目如下：
 * 主页
 * 主目录路径、 应用主文件夹
 * 超时时间 
 * 同时并发数
 * 缓冲区大小
 * 编码
 * 
 * 实现的功能：
 * 1、配置文件的路径一般都是在项目路径对应的根目录config.properties文件下，要有生成新文件的方法
 * 2、获取所有的配置文件项
 */
namespace IOCPServer
{
    /// <summary>
    /// 配置参数静态类，设置默认值
    /// </summary>
    public static class Config
    {
        private static string _WEB_ROOT = @"webapps";
        public static string WEB_ROOT
        {
            get
            {
                return _WEB_ROOT;
            }
            set
            {
                Interlocked.Exchange(ref _WEB_ROOT, value);
            }
        }
        
        private static string _INDEX_PATH = _WEB_ROOT + @"\index.html";
        public static string INDEX_PATH
        {
            get 
            {
                return _INDEX_PATH;
            }
            set
            {
                Interlocked.Exchange(ref _INDEX_PATH, value);
            }
        }

        private static Encoding _ENCODING = Encoding.UTF8;
        public static Encoding ENCODING
        {
            get
            {
                return _ENCODING;
            }
            set
            {
                Interlocked.Exchange(ref _ENCODING, value);
            }
        }

        private static long _TIMEOUT = -1;   //milliseconds, <0表示短连接
        public static long TIMEOUT
        {
            get
            {
                return _TIMEOUT;
            }
            set
            {
                Interlocked.Exchange(ref _TIMEOUT, value);
            }
        }

        private static int _SERVER_PORT = 8088;
        public static int SERVER_PORT
        {
            get
            {
                return _SERVER_PORT;
            }
            set
            {
                Interlocked.Exchange(ref _SERVER_PORT, value);
            }
        }

        private static int _MAX_CLIENT = 1024;
        public static int MAX_CLIENT
        {
            get
            {
                return _MAX_CLIENT;
            }
            set
            {
                Interlocked.Exchange(ref _MAX_CLIENT, value);
            }
        }

        private static int _BUFFER_SIZE = 1024;  //bytes
        public static int BUFFER_SIZE
        {
            get
            {
                return _BUFFER_SIZE;
            }
            set
            {
                Interlocked.Exchange(ref _BUFFER_SIZE, value);
            }
        }
    }

    /// <summary>
    /// 类名：ConfigHandler 
    /// </summary> 
    public class ConfigHandler : System.Collections.Hashtable
    {
        #region Properties
        /// <summary>
        /// 配置文件中存在的键
        /// </summary>
        private ArrayList keys = new ArrayList();
        
        /// <summary>
        /// 配置文件所在路径
        /// </summary>
        private String fileName = string.Empty;              //要读写的Properties文件名

        
        #endregion
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fileName">文件名</param>
        public ConfigHandler(String fileName)
        {
            this.setFileName(fileName);
            load(fileName);
        }
   
        private void setFileName(string filePath)
        {
            this.fileName = filePath;
        }

        /// <summary>
        /// 重写Add方法,实现按添加顺序排列
        /// </summary>
        /// <param name="key">key</param>

        ///<param name="value">value</param>
        /// <returns></returns>    
        public override void Add(object key, object value)
        {
            if (keys.Contains(key))
            {
                base[key] = value;
                return;
            }
            base.Add(key, value);
            keys.Add (key);
        }
        
        
        public override ICollection Keys
        {
            get
            {
                return keys;
            }
        }
 
        /// <summary>
        /// 导入文件
        /// </summary>
        /// <param name="filePath">要导入的文件</param>
        /// <returns></returns>
        private void load(string filePath)
        {
            char[] convertBuf = new char[1024];
            int limit;
            int keyLen;
            int valueStart;
            char c;
            string bufLine = string.Empty;
            bool hasSep;
            bool precedingBackslash;
 
            using (StreamReader sr = new StreamReader(filePath))
            {
                while(sr.Peek()>=0)
                {
                    bufLine = sr.ReadLine();
                    limit = bufLine.Length;
                    keyLen = 0;
                    valueStart = limit;
                    hasSep = false;
                    precedingBackslash = false;
                    if(bufLine.StartsWith("#"))
                        keyLen = bufLine.Length;

                    while (keyLen < limit)
                    {
                        c = bufLine[keyLen];
                        if ((c == '=' || c == ':') & !precedingBackslash)
                        {
                            valueStart = keyLen + 1;
                            hasSep = true;
                            break;
                        }
                        else if ((c == ' ' || c == '\t' || c == '\f') & !precedingBackslash)
                        {
                            valueStart = keyLen + 1;
                            break;
                        }
                        if (c == '\\')
                        {
                            precedingBackslash = !precedingBackslash;
                        }
                        else
                        {
                            precedingBackslash = false;
                        }
                        keyLen++;
                    }
 
                        
                    while (valueStart < limit) 
                    {
                        c = bufLine[valueStart];
                        if (c != ' ' && c != '\t' &&  c != '\f') 
                        {
                            if (!hasSep && (c == '=' ||  c == ':')) 
                            {
                                hasSep = true;
                            } 
                            else 
                            {
                                break;
                            }
                        }
                        valueStart++;
                    }
 
                    string key = bufLine.Substring(0,keyLen);
      
                    string values = bufLine.Substring(valueStart,limit-valueStart);
 
                    if(key=="")
                        key += "#";
                    while(key.StartsWith("#")&this.Contains(key))
                    {
                        key += "#";
                    }
      
                    this.Add(key,values);
                }
            }
        }
 
        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="filePath">要保存的Properties文件</param>
        /// <returns></returns>
        private void save(string filePath)
        {
            if(File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            FileStream fileStream = File.Create(filePath);
            StreamWriter sw = new StreamWriter(fileStream);
            foreach (object item in keys) 
            {
                String key = (String)item;
                String val = (String)this[key];
                if(key.StartsWith("#"))
                {
                    if(val== "")
                    {
                        sw.WriteLine(key);
                    }
                    else
                    {
                        sw.WriteLine(val);
                    }
                }
                else
                {
                    sw.WriteLine(key+"="+val);
                }
            }
            sw.Close();
            fileStream.Close();
         }

        /// <summary>
        /// 对外开放的设置属性接口
        /// </summary>
        /// <param name="key">要保存的属性key</param>
        /// <param name="value">要保存的属性value</param>
        /// <returns>
        /// <param name="success">返回是否设置成功</param>
        /// </returns>
        public bool setProperty(string key, string value)
        {
            bool success = false;
            try
            {
                this.Add(key, value);
                this.save(this.fileName);
                success = true;
            }
            catch {
                throw new Exception("设置配置文件失败");
            }
            return success;
        }

        ///<summary>
        /// 对外开放的设置属性接口
        /// </summary>
        /// 
        public bool setProperty(Dictionary<string,string> map)
        {
            bool success = false;
            try
            {
                foreach (string item in map.Keys)
                {
                    this.Add(item, map[item]);
                }
                
                this.save(this.fileName);
                success = true;
            }
            catch
            {
                throw new Exception("设置配置文件失败");
            }
            return success;
        }

        //public static Hashtable getProperties()
        //{
        //    return ;
        //}
    }
}




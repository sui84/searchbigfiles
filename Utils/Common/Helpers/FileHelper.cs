using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Common.Helpers
{
    public class FileHelper
    {
        const int MAXLINELEN = 500;
        /// 分割大文件成独立小文件并保存小文件至指定目录
        /// </summary>
        /// <param name="splitunit">分割单位（KB，MB）</param>
        /// <param name="intFlag">分割大小</param>
        /// <param name="destCatalog">保存路径</param>
        /// <param name="sourcefileurl">源文件路径</param>
        /// <returns>true表示分割成功，false表示分割失败</returns>
        public  bool SplitFileLines(string splitunit, int intFlag, string destCatalog, string sourcefileurl)
        {
            bool suc = false;
            try
            {
                int iFileSize = 0;
                switch (splitunit)
                {
                    case "Byte":
                        iFileSize = intFlag;
                        break;
                    case "KB":
                        iFileSize = 1024 * intFlag;
                        break;
                    case "MB":
                        iFileSize = 1024 * 1024 * intFlag;
                        break;
                    default:
                        iFileSize = 1024 * 1024 * 1024 * intFlag;
                        break;
                }

                using (FileStream fs = new FileStream(sourcefileurl, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader SplitFileReader = new BinaryReader(fs))
                    {
                        Byte[] TempBytes;
                        int ifilecount = (int)(fs.Length / iFileSize);
                        if (fs.Length % iFileSize != 0)
                        {
                            ifilecount++;
                        }
                        long lastfsize = 0;
                        for (int i = 1; i <= ifilecount; i++)
                        {
                            if (!Directory.Exists(destCatalog)) Directory.CreateDirectory(destCatalog);
                            string sTempFileName = destCatalog + "\\" + Path.GetFileNameWithoutExtension(sourcefileurl) + i.ToString().PadLeft(4, '0') + Path.GetExtension(sourcefileurl);
                            using (FileStream TempStream = new FileStream(sTempFileName, FileMode.OpenOrCreate))
                            {
                                using (BinaryWriter bw = new BinaryWriter(TempStream))
                                {
                                    int j = 1;
                                    if(i != ifilecount){
                                    fs.Seek(lastfsize + iFileSize, SeekOrigin.Begin);//fs读取后的内容为：123456789'
                                    for (j = 1; j <= MAXLINELEN; j++)
                                    {
                                        byte[] data = new byte[1];
                                        fs.Read(data, 0, 1);
                                        string s = System.Text.Encoding.Default.GetString(data);
                                        //Console.Write(s + " ");
                                        if (s == "\r" || s == "\n")
                                        {
                                            break;
                                        }
                                    }
                                    }
                                    fs.Seek(lastfsize, SeekOrigin.Begin);
                                    TempBytes = SplitFileReader.ReadBytes(iFileSize + j);
                                    lastfsize += iFileSize + j;
                                    bw.Write(TempBytes);
                                    bw.Close();
                                };
                                TempStream.Close();
                            };
                        }
                        fs.Close();
                        SplitFileReader.Close();
                    };  
                };
                suc = true;
            }
            catch(Exception ex) 
            {
                string error = ex.Message + ex.StackTrace;
                suc = false; }
            return suc;
        }

        /// <summary>
        /// 分割大文件成独立小文件并保存小文件至指定目录
        /// </summary>
        /// <param name="splitunit">分割单位（KB，MB）</param>
        /// <param name="intFlag">分割大小</param>
        /// <param name="destCatalog">保存路径</param>
        /// <param name="sourcefileurl">源文件路径</param>
        /// <returns>true表示分割成功，false表示分割失败</returns>
        public  bool SplitFileBySize(string splitunit, int intFlag, string destCatalog, string sourcefileurl)
        {
            bool suc = false;
            try
            {
                int iFileSize = 0;
                switch (splitunit)
                {
                    case "Byte":
                        iFileSize = intFlag;
                        break;
                    case "KB":
                        iFileSize = 1024 * intFlag;
                        break;
                    case "MB":
                        iFileSize = 1024 * 1024 * intFlag;
                        break;
                    default:
                        iFileSize = 1024 * 1024 * 1024 * intFlag;
                        break;
                }
                FileStream SplitFileStream = new FileStream(sourcefileurl, FileMode.Open);
                BinaryReader SplitFileReader = new BinaryReader(SplitFileStream);
                Byte[] TempBytes;
                int ifilecount = (int)(SplitFileStream.Length / iFileSize);
                if (SplitFileStream.Length % iFileSize != 0)
                {
                    ifilecount++;
                }
                for (int i = 1; i <= ifilecount; i++)
                {
                    string sTempFileName = destCatalog + "\\" + i.ToString().PadLeft(4, '0') + Path.GetExtension(sourcefileurl);
                    FileStream TempStream = new FileStream(sTempFileName, FileMode.OpenOrCreate);
                    BinaryWriter bw = new BinaryWriter(TempStream);
                    TempBytes = SplitFileReader.ReadBytes(iFileSize);
                    bw.Write(TempBytes);
                    bw.Close();
                    TempStream.Close();
                }
                SplitFileStream.Close();
                SplitFileReader.Close();
                suc = true;
            }
            catch (Exception ex) { 
                string error = ex.Message + ex.StackTrace; 
                suc = false; }
            return suc;
      }

        public void MergeFile(string dir,string destFile){
            foreach (string file in Directory.GetFiles(dir))
            {
                string[] lines = System.IO.File.ReadAllLines(file);
                File.AppendAllLines(destFile, lines, Encoding.UTF8);
                File.Delete(file);
            }     
       }

    }
}

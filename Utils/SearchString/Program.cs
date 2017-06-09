using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Common.Helpers;
using System.Data;
using System.Threading.Tasks;
using System.Threading;

namespace SearchString
{
    class Program
    {
        const int MAXFILESIZE = 200;  //大于200M的文件就分割
        static void Main(string[] args)
        {
            LogHelper logHelper = new LogHelper();
            try
            { 
                DbHelper dbhelper = new DbHelper();
                string filePath = string.Empty;
                if (args.Length == 0) filePath = Properties.Settings.Default.FilePath;


                //step1:找出所有文件
                int index = 0;
                FindFiles(filePath,index);


                //step2:分割大文件（>300M）
                SplitFiles();

                //step3:读取文件
                DataSet ds = dbhelper.GetDataFromCommand(@"select FileId,FilePath from files where splited is null order by FileSize desc", Properties.Settings.Default.ConnStr);
                SearchString(ds);

                //step4:重试失败
                DataSet fds = dbhelper.GetDataFromCommand(@"select FileId,FilePath from files where splited is null and failed=1 order by FileSize desc", Properties.Settings.Default.ConnStr);
                SearchString(fds);
                
            }
            catch (Exception ex)
            {

                logHelper.LogInfo(string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace), Properties.Settings.Default.LogPath);
            }
            //Thread.Sleep(500000000);
        }

        private static void SearchString(DataSet ds)
        {
            int cnt = ds.Tables[0].Rows.Count;
            if (cnt > 0)
            {
                List<string> strs = System.IO.File.ReadAllLines(Properties.Settings.Default.StringFilePath).ToList();
                LogHelper logHelper = new LogHelper();
                logHelper.LogInfo("Start---", Properties.Settings.Default.LogPath);
                // not work : end soon & take much time

                //Action[] aArr = new Action[cnt];
                //int j = 0;
                //for (int i = 0; i < cnt; i++)
                //{
                //    aArr[i] = new Action(() => FindString(ds.Tables[0].Rows[j++], strs));
                //}
                //System.Threading.Tasks.ParallelOptions po = new ParallelOptions();
                //po.MaxDegreeOfParallelism = Properties.Settings.Default.TheadNum;
                //System.Threading.Tasks.Parallel.Invoke(po, aArr);

                //for (int i = 0; i < cnt; i++)
                //{
                //    ThreadPool.QueueUserWorkItem(arg =>
                //    {
                //        FindString(ds.Tables[0].Rows[i], strs);
                //    });
                //}
                // 尝试了几种方法，在总共50G的文本文件里搜索6个字符串，还是把文件分割小些处理起来时间一样错误几率少了
                // don't use mulpile thread ,the time is less but more outofmemory
                // case1: 300M , 2 thread , 20 minutes , 8 outofmemory
                // case2: 300M , 1 thread , 15 minutes , 21 outofmemory
                // case3: 200M , 1 thread , 1 hour , 4 outofmemory
                // case3: 200M , 2 thread , 20 minutes , 0 outofmemory
                Parallel.For(0, ds.Tables[0].Rows.Count, (new ParallelOptions { MaxDegreeOfParallelism = Properties.Settings.Default.TheadNum }), (int i) =>
                {
                    //for (int i = 0; i < cnt; i++)
                    DbHelper dbhelper = new DbHelper();
                    LinqHelper linqHelper = new LinqHelper();
                    if (!Directory.Exists(Properties.Settings.Default.ResultPath))
                        Directory.CreateDirectory(Properties.Settings.Default.ResultPath);
                    string fileName = Path.Combine(Properties.Settings.Default.ResultPath, "Result" + Thread.CurrentThread.ManagedThreadId + ".txt");
                    if (!Directory.Exists(Path.Combine(Properties.Settings.Default.ResultPath, "Log")))
                        Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.ResultPath, "Log"));
                    string logfileName = Path.Combine(Properties.Settings.Default.ResultPath, "Log", Thread.CurrentThread.ManagedThreadId + ".txt");
                    string file = ds.Tables[0].Rows[i]["FilePath"].ToString();
                    try
                    {
                        List<string> lines = System.IO.File.ReadAllLines(file).ToList();
                        List<string> strList = linqHelper.SearchString(lines, strs);
                        if (strList.Count > 0) logHelper.LogInfo(file, fileName);
                        File.AppendAllLines(fileName, strList, Encoding.UTF8);
                        logHelper.LogInfo("Processed file:" + file, logfileName);
                    }
                    catch (Exception ex)
                    {
                        logHelper.LogInfo(string.Format("Exception in file:{0}\r\n{1}\r\n{2}", file, ex.Message, ex.StackTrace), logfileName);
                        string sql = string.Format(@"update files set Failed=1 where FileId={0}", ds.Tables[0].Rows[i]["FileId"]);
                        dbhelper.ExecuteCommand(sql, Properties.Settings.Default.ConnStr);
                    }
                    //}
                });
                FileHelper fileHelper = new FileHelper();
                fileHelper.MergeFile(Path.Combine(Properties.Settings.Default.ResultPath, "Log"), Properties.Settings.Default.LogPath);
                fileHelper.MergeFile(Properties.Settings.Default.ResultPath, Path.Combine(Properties.Settings.Default.ResultPath, "Result.txt"));
                logHelper.LogInfo("---End", Properties.Settings.Default.LogPath);
            }
        }

        //逐行读取，耗费了大量IO&CPU，而且时间漫长
        //static void FindString( DataRow dr, string[] strs)
        //{
        //    LogHelper logHelper = new LogHelper();
        //    string threadFile = string.Format(@"{0}\FindResult{1}.txt", Properties.Settings.Default.ResultPath, Thread.CurrentThread.ManagedThreadId);
        //    try
        //    {  
        //        string filePath = dr["FilePath"].ToString();
        //        StreamReader sr = File.OpenText(filePath);
        //        string nextLine;
        //        int lineNum = 0;
        //        while ((nextLine = sr.ReadLine()) != null)
        //        {
        //            lineNum += 1;
        //            foreach (string str in strs)
        //            {
        //                if (nextLine.IndexOf(str) > -1)
        //                {
        //                    logHelper.LogInfo(string.Format("FileId:{0}LineNum:{1} String {2} exist in Content:{3}", dr["FileId"], lineNum, str, nextLine), threadFile);
        //                    break;
        //                };
        //            }
        //        }
        //        sr.Close();
        //        DbHelper dbhelper = new DbHelper();

        //        string sql = string.Format(@"update files set LineCount={0},Searched={1},SearchedTime=getdate() where FileId={3}", lineNum, 1, dr["FilePath"]);
        //        logHelper.LogInfo(sql, threadFile);
        //        dbhelper.ExecuteCommand(sql, Properties.Settings.Default.ConnStr);
        //        logHelper.LogInfo(string.Format("filePath:{0}LineNum:{1}Finished!", filePath, lineNum), threadFile);
        //    }
        //    catch (Exception ex)
        //    {
        //        logHelper.LogInfo(string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace), threadFile);
        //    }
        //}

        static void FindFiles(string filePath, int index)
        {
            
            string[] files = Directory.GetFiles(filePath);
            foreach(string file in files){
                DbHelper dbhelper = new DbHelper();
                FileInfo fi = new FileInfo(file);
                dbhelper.ExecuteCommand(string.Format(@"insert into files(filepath,filesize,FileDepth)
                values('{0}',{1},{2})", file, fi.Length / 1024, index), Properties.Settings.Default.ConnStr);
            }
            string[] subDirs = Directory.GetDirectories(filePath);
            if (subDirs.Length > 0 && index < Properties.Settings.Default.FileDepth)
            {
                foreach (string dir in subDirs)
                {
                    FindFiles(dir,  index + 1);
                }
            }
            
        }

        static void SplitFiles()
        {
            DbHelper dbhelper = new DbHelper();
            FileHelper filehelper = new FileHelper();
            //bool splited =filehelper.SplitFileBySize("MB", MAXFILESIZE, @"D:\DB\txt\split2", @"D:\DB\txt\天涯论坛4000w数据(1).txt");
            //bool succ = filehelper.SplitFileLines("MB", MAXFILESIZE, @"D:\DB\txt\split3", @"D:\DB\txt\天涯论坛4000w数据(1).txt");
            DataSet ds = dbhelper.GetDataFromCommand(string.Format(@"select FileId,FilePath,FileDepth from files  where filesize>1024*{0} and splited is null order by FileSize", MAXFILESIZE), Properties.Settings.Default.ConnStr);
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                string filepath = dr["FilePath"].ToString();
                string newdir = Path.Combine(Path.GetDirectoryName(filepath), "Split");
                if (!Directory.Exists(newdir)) Directory.CreateDirectory(newdir);
                else
                {
                    foreach (string f in Directory.GetFiles(newdir))
                    {
                        if (f.StartsWith(Path.GetFileNameWithoutExtension(filepath)))
                            File.Delete(f);
                    }
                }
                bool splited = filehelper.SplitFileLines("MB", MAXFILESIZE, newdir, filepath);
                if (splited)
                {
                    FindFiles(newdir, Convert.ToInt32(dr["FileDepth"]) + 1);
                    dbhelper.ExecuteCommand(string.Format("update files set splited=1 where FileId={0}", dr["FileId"]), Properties.Settings.Default.ConnStr);
                }
            }
        }

        private static void WriteLog(string userName)
        {
            Console.WriteLine(string.Format("Begin Time {0}:ProcessId {2} ThreadId {3} {1}", DateTime.Now, userName, System.Diagnostics.Process.GetCurrentProcess().Id, Thread.CurrentThread.ManagedThreadId));
        }

    }
}

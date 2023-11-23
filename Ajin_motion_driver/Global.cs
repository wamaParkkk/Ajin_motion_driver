using System;
using System.IO;

namespace Ajin_motion_driver
{
    class Global
    {        
        static string logfilePath = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory, @"..\..\AjinMotionLog\"));
        public static string szMotFilePath = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory, @"..\..\Motion.mot"));
        
        public static void EventLog(string Msg)
        {
            string sDate = DateTime.Today.ToShortDateString();
            string sTime = DateTime.Now.ToString("HH:mm:ss");
            string sDateTime;
            sDateTime = "[" + sDate + ", " + sTime + "] ";

            WriteFile(sDateTime + Msg);
        }

        private static void WriteFile(string Msg)
        {
            string sDate = DateTime.Today.ToShortDateString();
            string FileName = sDate + ".txt";

            if (File.Exists(logfilePath + FileName))
            {
                StreamWriter writer;
                writer = File.AppendText(logfilePath + FileName);
                writer.WriteLine(Msg);
                writer.Close();
            }
            else
            {
                CreateFile(Msg);
            }
        }

        private static void CreateFile(string Msg)
        {
            string sDate = DateTime.Today.ToShortDateString();
            string FileName = sDate + ".txt";

            if (!File.Exists(logfilePath + FileName))
            {
                using (File.Create(logfilePath + FileName)) ;
            }

            StreamWriter writer;
            writer = File.AppendText(logfilePath + FileName);
            writer.WriteLine(Msg);
            writer.Close();
        }
    }
}

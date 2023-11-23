using System;
using System.Threading;

namespace Ajin_motion_driver
{
    public class HomeSearch
    {
        private static Thread[] HomeThreads = { null, null, null };
        private static bool[] threads_run = { false, false, false };

        public static MOTOR[] motor = new MOTOR[MotionDefine.Axis_max];        
  
        public static void Home(int axis)
        {
            /*
            Thread myNewThread = null;
            myNewThread = new Thread(() => SearchHome_Thread(axis));

            if (threads_run[axis])
            {
                HomeThreads[axis].Join();
            }

            if (myNewThread != null)
            {
                threads_run[axis] = true;
                HomeThreads[axis] = myNewThread;
                HomeThreads[axis].Start();
            } 
            */

            uint duHomeResult = 0;
            //++ 지정한 축에 원점검색을 진행합니다.
            duHomeResult = CAXM.AxmHomeSetStart(axis);
            Global.EventLog(string.Format("[Axis no:{0:D}] Home start", axis));
        }

        private static void SearchHome_Thread(int axis)
        {
			uint duHomeResult = 0;
			uint duHomeMainStepNumber = 0;
			uint duHomeStepNumber = 0;			
			
			motor[axis].m_bHomeEnd = false;
			int iHomeRate = 0;

			CAXM.AxmHomeSetStart(axis);
			
			Global.EventLog(string.Format("[Axis no:{0:D}] Home start", axis));

			CAXM.AxmHomeGetResult(axis, ref duHomeResult);

            try
            {
                while (duHomeResult != (uint)AXT_MOTION_HOME_RESULT.HOME_SUCCESS)
                {
                    CAXM.AxmHomeGetResult(axis, ref duHomeResult);
                    CAXM.AxmHomeGetRate(axis, ref duHomeMainStepNumber, ref duHomeStepNumber);

                    if (iHomeRate != (int)duHomeStepNumber)
                    {
                        iHomeRate = (int)duHomeStepNumber;
                        Global.EventLog(string.Format("[Axis no:{0:D}] Home searching...{1:D}", axis, (int)duHomeStepNumber));
                    }

                    Thread.Sleep(250);
                }

                motor[axis].m_bHomeEnd = true;

                Global.EventLog(string.Format("[Axis no:{0:D}] Home completed (Flag:{1:D})", axis, motor[axis].m_bHomeEnd));
            }
            catch (Exception)
            {

            }			
		}
    }
}

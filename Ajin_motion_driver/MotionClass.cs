using System;
using System.Threading;

namespace Ajin_motion_driver
{
    public class MotionClass
    {
        private static Thread drvThread;

        public static MOTOR[] motor = new MOTOR[MotionDefine.Axis_max];

        public static int m_lAxisCounts = 0;                   // 제어 가능한 축 갯수 선언 및 초기화.        
        public static uint i_Status;                           // 축의 모션 상태 반환 변수.        
        public static bool bThread_end;
        public static bool bSet_flag = false;

        public static double _dVelocity;
        public static double _dAccel;
        public static double _dDecel;
        public static double _dGearing;
        public static double _dPosition;

        public static void Ajin_Motion_Init()
        {
            drvThread = null;
            bool bRtn;                        

            for (int i = 0; i < MotionDefine.Axis_max; i++)
            {
                motor[i].dR_CmdPosition_step = 0.0;
                motor[i].dR_ActPosition_step = 0.0;
                motor[i].dR_CmdPosition_mm = 0.0;
                motor[i].dR_ActPosition_mm = 0.0;
                motor[i].dR_CmdVelocity = 0.0;
                motor[i].dR_Accel = 0.0;      
                motor[i].dR_Decel = 0.0;      
                motor[i].dR_Gearing = 0.0;    
                motor[i].sR_BusyStatus = "Ready"; 
                motor[i].sR_HomeStatus = "None"; 
                motor[i].sR_ServoStatus = "SVOFF";
                motor[i].sR_AlarmStatus = "NoAlarm";

                motor[i].m_bHomeEnd = false;
            }
            
            bRtn = DRV_INIT();
            if (bRtn)
            {
                bThread_end = false;
                
                drvThread = new Thread(Ajin_motion_thread);
                drvThread.Start();
            }
            else
            {
                Global.EventLog("Ajin motion driver initialization fail");
                DRV_CLOSE();
            }
        }

        public static bool DRV_INIT()
        {
            if (InitLibrary())
            {
                Global.EventLog("Axl library initialization completed");
            }
            else
            {
                return false;
            }

            if (AddAxisInfo())
            {
                Global.EventLog("Completed obtaining information on the axis");

                for (int i = 0; i < m_lAxisCounts; i++)
                {
                    // Servo On.
                    SetMotorServo(i, (uint)DigitalValue.On);
                    Global.EventLog(string.Format("Servo ON : Axis No = {0:D}", i));
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public static bool InitLibrary()
        {
            //++ AXL(AjineXtek Library)을 사용가능하게 하고 장착된 보드들을 초기화.          
            /*
            uint duRetCode = CAXL.AxlOpen(7);
            Global.EventLog(string.Format("Axl open return value = {0:D}", duRetCode));

            if (duRetCode != (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                Global.EventLog("Axl open fail");
                return false;
            }
            */
            //++ 지정한 Mot파일의 설정값들로 모션보드의 설정값들을 일괄변경 적용.
            if (CAXM.AxmMotLoadParaAll(Global.szMotFilePath) != (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                Global.EventLog("Mot File Not Found");
                return false;
            }

            return true;
        }

        public static bool AddAxisInfo()
        {
            //++ 유효한 전체 모션축수를 반환.
            CAXM.AxmInfoGetAxisCount(ref m_lAxisCounts);
            if (m_lAxisCounts > 0)
            {                
                Global.EventLog(string.Format("Total number of axes : {0:D}", m_lAxisCounts));

                return true;
            }
            else
            {
                Global.EventLog("No axis was searched");
                return false;
            }
        }
       
        public static void DRV_CLOSE()
        {
            bThread_end = true;
            
            if (drvThread != null)
            {
                drvThread.Abort();
                Global.EventLog("Ajin motion thread abort");
            }            

            CAXL.AxlClose();
            Global.EventLog("Ajin motion driver close");
        }

        #region PARAMETER READ THREAD
        public static void Ajin_motion_thread()
        {
            try
            {
                while (true)
                {
                    if (!bThread_end)
                    {
                        Parameter_read();

                        Thread.Sleep(10);
                    }                    
                }
            }
            catch (Exception ex)
            {
                Global.EventLog(string.Format("Ajin motion thread error : {0}", ex));
            }
        }
        #endregion

        #region MOTOR STATUS 
        public static void Parameter_read()
        {            
            double m_dCmdPosition_step = 0.0;
            double m_dActPosition_step = 0.0;
            double m_dCmdVelocity = 0.0;
            double m_dGearing;

            uint duState = 0;

            for (int i_AxisNo=0; i_AxisNo<m_lAxisCounts; i_AxisNo++)
            {
                // 지령(Command)위치를 반환.(Pulse값)
                CAXM.AxmStatusGetCmdPos(i_AxisNo, ref m_dCmdPosition_step);
                // 지령(Command)위치 값을 mm값으로 변환.
                motor[i_AxisNo].dR_CmdPosition_step = m_dCmdPosition_step;
                m_dGearing = motor[i_AxisNo].dR_Gearing;
                motor[i_AxisNo].dR_CmdPosition_mm = m_dCmdPosition_step / m_dGearing;

                // 실제(Feedback)위치를 반환.(Pulse값)
                CAXM.AxmStatusGetActPos(i_AxisNo, ref m_dActPosition_step);
                // 실제(Feedback)위치 값을 mm값으로 변환.
                motor[i_AxisNo].dR_ActPosition_step = m_dActPosition_step;
                motor[i_AxisNo].dR_ActPosition_mm = m_dActPosition_step / m_dGearing;


                // 구동 속도를 반환.
                CAXM.AxmStatusReadVel(i_AxisNo, ref m_dCmdVelocity);
                motor[i_AxisNo].dR_CmdVelocity = m_dCmdVelocity;                


                // 모션 상태 확인.
                if (InMotion(i_AxisNo))
                {
                    motor[i_AxisNo].sR_BusyStatus = "Moving";
                }
                else
                {
                    motor[i_AxisNo].sR_BusyStatus = "Ready";
                }

                // Home 및 Limit 상태 확인.
                if (IsORG(i_AxisNo))
                {
                    motor[i_AxisNo].sR_HomeStatus = "Home";
                }
                else if (IsPELM(i_AxisNo))
                {
                    motor[i_AxisNo].sR_HomeStatus = "+Limit";
                }
                else if (IsNELM(i_AxisNo))
                {
                    motor[i_AxisNo].sR_HomeStatus = "-Limit";
                }
                else
                {
                    motor[i_AxisNo].sR_HomeStatus = "None";
                }

                // Servo on/off 상태 확인.
                if (IsServoOn(i_AxisNo))
                {
                    motor[i_AxisNo].sR_ServoStatus = "SVON";
                }
                else
                {
                    motor[i_AxisNo].sR_ServoStatus = "SVOFF";
                }

                // Servo alarm 신호의 상태 확인.
                if (IsAlarm(i_AxisNo))
                {
                    motor[i_AxisNo].sR_AlarmStatus = "Alarm";
                }
                else
                {
                    motor[i_AxisNo].sR_AlarmStatus = "NoAlarm";
                }                
            }                       
        }

        private static bool InMotion(int nAxisNo)
        {
            i_Status = 0;
            CAXM.AxmStatusReadInMotion(nAxisNo, ref i_Status);

            if (i_Status == 0)
                return false;   // Ready.           
            else
                return true;    // Moving.            
        }

        private static bool IsORG(int nAxisNo)
        {
            uint duState = 0, duRetCode;
            //++ 지정한 축의 원점신호의 상태를 확인.
            duRetCode = CAXM.AxmHomeReadSignal(nAxisNo, ref duState);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if (Convert.ToBoolean(duState))
                    return true;    // Home.                
                else
                    return false;   // None or Limit sensor.

            }
            else
            {
                Global.EventLog(string.Format("Home read signal error return code : {0:D}", duRetCode));
                return false;
            }
        }

        private static bool IsPELM(int nAxisNo)
        {
            uint duState1 = 0, duState2 = 0, duRetCode;
            //++ (+)End Limit신호의 상태를 확인.
            duRetCode = CAXM.AxmSignalReadLimit(nAxisNo, ref duState1, ref duState2);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if (Convert.ToBoolean(duState1))
                    return true;    // (+)Limit.                
                else
                    return false;
            }
            else
            {
                Global.EventLog(string.Format("Signal read +limit error return code : {0:D}", duRetCode));
                return false;
            }
        }

        private static bool IsNELM(int nAxisNo)
        {
            uint duState1 = 0, duState2 = 0, duRetCode;
            //++ (-)End Limit신호의 상태를 확인.
            duRetCode = CAXM.AxmSignalReadLimit(nAxisNo, ref duState1, ref duState2);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if (Convert.ToBoolean(duState2))
                    return true;    // (-)Limit.                
                else
                    return false;
            }
            else
            {
                Global.EventLog(string.Format("Signal read -limit error return code : {0:D}", duRetCode));
                return false;
            }
        }

        private static bool IsServoOn(int nAxisNo)
        {
            uint duOnOff = 0;
            CAXM.AxmSignalIsServoOn(nAxisNo, ref duOnOff);
            if (Convert.ToBoolean(duOnOff))
                return true;    // Servo on.            
            else
                return false;   // Servo off.            
        }

        private static bool IsAlarm(int nAxisNo)
        {
            uint duState1 = 0, duRetCode;

            duRetCode = CAXM.AxmSignalReadServoAlarm(nAxisNo, ref duState1);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if (Convert.ToBoolean(duState1))
                    return true;    // Servo Alarm.                
                else
                    return false;   // Servo noAlarm.                
            }
            else
            {
                Global.EventLog(string.Format("Signal read servo alarm error return code : {0:D}", duRetCode));
                return false;
            }
        }        
        #endregion

        #region SETTING FUNCTION
        public static void SetMotorServo(int nAxisNo, uint duOnOff)
        {
            uint duRetCode = CAXM.AxmSignalServoOn(nAxisNo, duOnOff);

            if (duOnOff == (uint)DigitalValue.Off)
            {
                ClearHomeEnd(nAxisNo);
            }
        }

        public static void SetMotorHome(int nAxisNo)
        {
            HomeSearch.Home(nAxisNo);
        }

        public static void SetMotorSStop(int nAxisNo)
        {
            SStop(nAxisNo);
        }

        public static void SetMotorEStop(int nAxisNo)
        {
            EStop(nAxisNo);
        }

        public static void SetAlarmReset(int nAxisNo)
        {
            AlarmReset(nAxisNo);
        }

        public static void SetZeroset(int nAxisNo)
        {
            SetCmdPos(nAxisNo, 0);
            SetActPos(nAxisNo, 0);
        }

        public static void MotorJogP(int nAxisNo, double dVelocity, double dAccel, double dDecel)
        {
            uint duRetCode = CAXM.AxmMoveVel(nAxisNo, dVelocity, dAccel, dDecel);
            if (duRetCode != (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                Global.EventLog(string.Format("AxmMoveVel(Jog+) return error code : ", duRetCode));
            }
        }

        public static void MotorJogN(int nAxisNo, double dVelocity, double dAccel, double dDecel)
        {
            uint duRetCode = CAXM.AxmMoveVel(nAxisNo, -dVelocity, dAccel, dDecel);
            if (duRetCode != (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                Global.EventLog(string.Format("AxmMoveVel(Jog-) return error code : ", duRetCode));
            }
        }

        public static void SetMotorVelocity(int nAxisNo, double dValue)
        {
            if (dValue > 0)
            {
                _dVelocity = dValue;
            }
            else
            {
                _dVelocity = motor[nAxisNo].dW_Velocity;
            }
            motor[nAxisNo].dW_Velocity = _dVelocity;

            if (InMotion(nAxisNo))
            {
                CAXM.AxmOverrideAccelVelDecel(nAxisNo, _dVelocity, _dAccel, _dDecel);
            }
            else
            {
                CAXM.AxmOverrideSetMaxVel(nAxisNo, 1200000);
            }
        }

        public static void SetMotorAccel(int nAxisNo, double dValue)
        {
            if (dValue >= 0)
            {
                _dAccel = dValue;
            }
            else
            {
                _dAccel = motor[nAxisNo].dW_Accel;
            }
            motor[nAxisNo].dW_Accel = _dAccel;
        }

        public static void SetMotorDecel(int nAxisNo, double dValue)
        {
            if (dValue >= 0)
            {
                _dDecel = dValue;
            }
            else
            {
                _dDecel = motor[nAxisNo].dW_Decel;
            }
            motor[nAxisNo].dW_Decel = _dDecel;
        }

        public static void SetMotorGearing(int nAxisNo, double dValue)
        {
            if (dValue >= 0)
            {
                _dGearing = dValue;
            }
            else
            {
                _dGearing = motor[nAxisNo].dW_Gearing;
            }
            motor[nAxisNo].dW_Gearing = _dGearing;
        }

        public static void MotorMove(int nAxisNo, double dValue)
        {
            _dGearing = motor[nAxisNo].dW_Gearing;
            if (_dGearing < 0)
            {
                Global.EventLog("Gearing Setting Error : Gearing Value < 0");
                return;
            }

            _dPosition = dValue * _dGearing;
            motor[nAxisNo].dW_Position_mm = _dPosition;

            Global.EventLog(string.Format("Setting(mm) : axis_no = {0:0}, position = {1:0.000}, velocity = {2:0.0}, accel = {3:0.0}, decel = {4:0.0}", nAxisNo, _dPosition, _dVelocity, _dAccel, _dDecel));

            if (StartMove(nAxisNo, _dPosition, _dVelocity, _dAccel, _dDecel))
                Global.EventLog("Start of the motor movement was successful");
            else
                Global.EventLog("Failed to start motor movement");
        }

        private static bool StartMove(int axis, double dPosi, double dVel, double dAcc, double dDec)
        {
            CAXM.AxmMotSetAbsRelMode(axis, (uint)AXT_MOTION_ABSREL.POS_ABS_MODE);                       // 0:절대좌표, 1:상대좌표
            CAXM.AxmMotSetProfileMode(axis, (uint)AXT_MOTION_PROFILE_MODE.SYM_S_CURVE_MODE);
            CAXM.AxmMotSetProfilePriority(axis, (uint)AXT_MOTION_PROFILE_PRIORITY.PRIORITY_VELOCITY);   // 0:속도, 1:가속도

            uint duRetCode = CAXM.AxmMoveStartPos(axis, dPosi, dVel, dAcc, dDec);

            Global.EventLog(string.Format("axis_no = {0:0}, position = {1:0.000}, velocity = {2:0.0}, accel = {3:0.0}, decel = {4:0.0}, result = {5:D2}", axis, _dPosition, _dVelocity, _dAccel, _dDecel, duRetCode));

            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS) 
                return true;
            else
                return false;           
        }

        private static void ClearHomeEnd(int axis)
        {
            motor[axis].m_bHomeEnd = false;
        }

        private static void SetHomeEnd(int axis)
        {
            motor[axis].m_bHomeEnd = true;
        }

        private static void SStop(int axis)
        {            
            uint duRetCode = CAXM.AxmMoveSStop(axis);
            if (duRetCode != (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                Global.EventLog(string.Format("AxmMoveSStop return error code : {0:D}", duRetCode));
            }                
        }

        private static void EStop(int axis)
        {
            uint duRetCode = CAXM.AxmMoveEStop(axis);
            if (duRetCode != (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                Global.EventLog(string.Format("AxmMoveEStop return error code : {0:D}", duRetCode));
            }
        }

        private static void AlarmReset(int axis)
        {
            CAXM.AxmSignalServoAlarmReset(axis, (uint)DigitalValue.On);
            Thread.Sleep(200);
            CAXM.AxmSignalServoAlarmReset(axis, (uint)DigitalValue.Off);
        }

        private static void SetCmdPos(int axis, double dPosition)
        {
            CAXM.AxmStatusSetCmdPos(axis, dPosition);
        }

        private static void SetActPos(int axis, double dPosition)
        {
            CAXM.AxmStatusSetActPos(axis, dPosition);
        }
        #endregion
    }
}

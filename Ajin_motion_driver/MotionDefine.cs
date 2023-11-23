using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ajin_motion_driver
{
    public enum DigitalValue
    {
        Off = 0,
        On = 1
    };

    public struct MOTOR
    {
        // Actual.
        public double dR_CmdPosition_step;    // Command Pulse.
        public double dR_ActPosition_step;    // Actual Pulse.
        public double dR_CmdPosition_mm;      // Command mm.
        public double dR_ActPosition_mm;      // Actual mm.
        public double dR_CmdVelocity;         // 모터 속도.
        public double dR_Accel;               // 모터 가속.
        public double dR_Decel;               // 모터 감속.
        public double dR_Gearing;             // Step/mm.
        public string sR_BusyStatus;          // 모터 상태.
        public string sR_HomeStatus;          // 모터 Home 상태.
        public string sR_ServoStatus;         // Servo on/off 상태.
        public string sR_AlarmStatus;         // Servo Alarm 상태.

        // Setting.
        public double dW_Position_step;
        public double dW_Position_mm;
        public double dW_Velocity;
        public double dW_Accel;
        public double dW_Decel;
        public double dW_Gearing;

        public bool m_bHomeEnd;
    }

    public class MotionDefine
    {
        // 최대 축 갯수 선언.
        public const uint Axis_max = 4;

        public const uint axis_r = 0;   // r축
        public const uint axis_z = 1;   // z축
        public const uint axis_x = 2;   // x축                
    }
}

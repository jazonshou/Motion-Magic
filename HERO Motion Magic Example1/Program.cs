/**
 * Example using the Motion Magic Control Mode of Talon SRX and the Magnetic Encoder.  Other sensors can be used by
 * changing the selected sensor type below.

 * MotionMagic control mode requires Talon firmware 11.8 or greater.

 * The test setup is ...
 *      A single Talon SRX (Device ID 0) http://www.ctr-electronics.com/talon-srx.html
 *      A VEX VersaPlanetary Gearbox http://www.vexrobotics.com/versaplanetary.html 
 *      Gearbox uses the CTRE Magnetic Encoder http://www.vexrobotics.com/vexpro/all/new-for-2016/217-5046.html
 *      Ribbon cable http://www.ctr-electronics.com/talon-srx-data-cable-4-pack.html
 *
 *      Talon SRX ribbon cable is connected to the Magnetic Encoder.  This provies the Talon with rotar position.
 *      See Talon SRX Software Reference Manual for gain-tuning suggestions.
 *
 * Press the top left shoulder button for direct-control of Talon's motor output using the left-y-axis.
 * Press the bottom left shoulder button to set the target position of the Talon's closed loop servo 
 * using the left-y-axis.  Notice the geared output will ramp up initiallly then ramp down as it approaches 
 * the target position.
 */
using CTRE.Phoenix;
using CTRE.Phoenix.Controller;
using CTRE.Phoenix.MotorControl;
using CTRE.Phoenix.MotorControl.CAN;
using Microsoft.SPOT;
using System;
using System.Threading;
using Math = System.Math;

namespace HERO_Motion_Magic_Example
{
    public class Program
    {
        /** talon to control */
        private TalonSRX rightLeader = new TalonSRX(0);
        private TalonSRX leftLeader= new TalonSRX(1);
        private TalonSRX rightFollower = new TalonSRX(2);
        private TalonSRX leftFollower = new TalonSRX(3); 
        private TalonSRX intake = new TalonSRX(12); 
        /** desired mode to put talon in */
        private ControlMode _mode = ControlMode.PercentOutput;
        /** attached gamepad to HERO, tested with Logitech F710 */
        private GameController _gamepad = new GameController(UsbHostDevice.GetInstance());
        /** constant slot to use */
        const int kSlotIdx = 0;
        /** How long to wait for receipt when setting a param.  Many setters take an optional timeout that API will wait for.
            This is benefical for initial setup (before movement), though typically not desired 
            when changing parameters concurrently with robot operation (gain scheduling for example).*/
        const int kTimeoutMs = 50;

        /**
         * Setup all of the configuration parameters.
         */
        public void SetupConfig()
        {
            /* specify sensor characteristics */
            leftLeader.ConfigSelectedFeedbackSensor(FeedbackDevice.QuadEncoder, 0);
            leftLeader.SetSensorPhase(false);
            rightLeader.ConfigSelectedFeedbackSensor(FeedbackDevice.QuadEncoder, 0);
            rightLeader.SetSensorPhase(false); // make sure positive motor output means sensor moves in position direction */
            // call ConfigEncoderCodesPerRev or ConfigPotentiometerTurns for Quadrature or Analog sensor types.
            
            /* brake or coast during neutral */
            leftLeader.SetNeutralMode(NeutralMode.Brake);
            rightLeader.SetNeutralMode(NeutralMode.Brake);

            intake.SetNeutralMode(NeutralMode.Brake);

            /* closed-loop and motion-magic parameters */
            leftLeader.Config_kF(kSlotIdx, 0.1451f, kTimeoutMs); // 8874 native sensor units per 100ms at full motor output (+1023)
            leftLeader.Config_kP(kSlotIdx, 0.425f, kTimeoutMs);
            leftLeader.Config_kI(kSlotIdx, 0.00085f, kTimeoutMs);
            leftLeader.Config_kD(kSlotIdx, 0.8f, kTimeoutMs);
            leftLeader.Config_IntegralZone(kSlotIdx, 50, kTimeoutMs);
            leftLeader.SelectProfileSlot(kSlotIdx, 0); /* select this slot */
            leftLeader.ConfigNominalOutputForward(0f, kTimeoutMs);
            leftLeader.ConfigNominalOutputReverse(0f, kTimeoutMs);
            leftLeader.ConfigPeakOutputForward(1.0f, kTimeoutMs);
            leftLeader.ConfigPeakOutputReverse(-1.0f, kTimeoutMs);
            leftLeader.ConfigMotionCruiseVelocity(8000, kTimeoutMs); // 8000 native units
            leftLeader.ConfigMotionAcceleration(16000, kTimeoutMs); // 16000 native units per sec, (0.5s to reach cruise velocity).

            
            rightLeader.Config_kF(kSlotIdx, 0.1451f, kTimeoutMs); // 8874 native sensor units per 100ms at full motor output (+1023)
            rightLeader.Config_kP(kSlotIdx, 0.425f, kTimeoutMs);
            rightLeader.Config_kI(kSlotIdx, 0.00085f, kTimeoutMs);
            rightLeader.Config_kD(kSlotIdx, 0.8f, kTimeoutMs);
            rightLeader.Config_IntegralZone(kSlotIdx, 50, kTimeoutMs);
            rightLeader.SelectProfileSlot(kSlotIdx, 0); /* select this slot */
            rightLeader.ConfigNominalOutputForward(0f, kTimeoutMs);
            rightLeader.ConfigNominalOutputReverse(0f, kTimeoutMs);
            rightLeader.ConfigPeakOutputForward(1.0f, kTimeoutMs);
            rightLeader.ConfigPeakOutputReverse(-1.0f, kTimeoutMs);
            rightLeader.ConfigMotionCruiseVelocity(8000, kTimeoutMs); // 8000 native units
            rightLeader.ConfigMotionAcceleration(16000, kTimeoutMs); // 16000 native units per sec, (0.5s to reach cruise velocity).


            /* Home the relative sensor, 
                alternatively you can throttle until limit switch,
                use an absolute signal like CtreMagEncoder_Absolute or analog sensor.
                */
            leftLeader.SetInverted(false);
            rightLeader.SetInverted(true);
            leftFollower.SetInverted(false); 
            rightFollower.SetInverted(true);

            leftLeader.SetSelectedSensorPosition(0);
            rightLeader.SetSelectedSensorPosition(0);

            leftFollower.Follow(leftLeader);
            rightFollower.Follow(rightLeader);
        }
        /** spin in this routine forever */

        public void RunForever()
        {
            SetupConfig(); /* configuration */
            /* robot loop */
            while (true)
            {
                /* get joystick params */
                float leftY = -1f * _gamepad.GetAxis(1);
                float rightX = -1f * _gamepad.GetAxis(2);
                float rightY = -1f * _gamepad.GetAxis(5); //check if correct axis
                //Joystick buttons
                bool btnTopLeftShoulder = _gamepad.GetButton(5);
                bool btnBtmLeftShoulder = _gamepad.GetButton(7);
                bool buttonTop= _gamepad.GetButton(6);
                bool buttonBottom = _gamepad.GetButton(8);
                //Deadband
                Deadband(ref leftY);
                Deadband(ref rightX);

                /* keep robot enabled if gamepad is connected and in 'D' mode */
                if (_gamepad.GetConnectionStatus() == UsbDeviceConnection.Connected)
                    Watchdog.Feed();

                /* set the control mode based on button pressed */
                if (btnTopLeftShoulder)
                    _mode = ControlMode.PercentOutput;
                if (btnBtmLeftShoulder)
                    _mode = ControlMode.MotionMagic;


                /* calc the Talon output based on mode */
                if (_mode == ControlMode.PercentOutput)
                {
                    //Tank Drive
                    leftLeader.Set(_mode, (float)System.Math.Pow(leftY, 3) - (float)System.Math.Pow(rightX, 3));
                    rightLeader.Set(_mode, (float)System.Math.Pow(leftY, 3) + (float)System.Math.Pow(rightX, 3)); 

                    //Intake
                    if (buttonTop) {
                        intake.Set(_mode, 1);
                    } else if (buttonBottom) {
                        intake.Set(_mode, -1);
                    } else {
                        intake.Set(_mode, 0);
                    }
                }

                else if (_mode == ControlMode.MotionMagic)
                {
                    float servoToRotation_leftY = leftY * 40960;// [-10, +10] rotations
                    float servoToRotation_rightY = rightY * 40960;
                    float servoToRotation_rightX = rightX * 40960;

                    //Tank Drive
                    leftLeader.Set(_mode, servoToRotation_leftY - servoToRotation_rightX);
                    rightLeader.Set(_mode, servoToRotation_leftY + servoToRotation_rightX);
                }
                /* instrumentation */
                Instrument.Process(leftLeader);
                //Instrument.Process(rightLeader);

                //Instrument.Process(intake);

                //Debug.Print("Left Encoder: " + leftLeader.GetSelectedSensorPosition() + "   Right Encoder: " + rightLeader.GetSelectedSensorPosition());

                /* wait a bit */
                System.Threading.Thread.Sleep(5);
            }
        }
        /** @param [in,out] value to zero if within plus/minus 10% */
        public static void Deadband(ref float val)
        {
            if (val > 0.10f) { /* do nothing */ }
            else if (val < -0.10f) { /* do nothing */ }
            else { val = 0; } /* clear val since its within deadband */
        }
        /** singleton instance and entry point into program */
        public static void Main()
        {
            Program program = new Program();
            program.RunForever();
        }
    }
}

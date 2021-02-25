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
        private TalonSRX leftFront = new TalonSRX(1);
        private TalonSRX leftBack = new TalonSRX(3); //swap
        private TalonSRX rightFront = new TalonSRX(0);
        private TalonSRX rightBack = new TalonSRX(2);
        private TalonSRX intake = new TalonSRX(12); //swap
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
            leftFront.ConfigSelectedFeedbackSensor(FeedbackDevice.QuadEncoder, 0);
            leftFront.SetSensorPhase(false);
            rightFront.ConfigSelectedFeedbackSensor(FeedbackDevice.QuadEncoder, 0);
            rightFront.SetSensorPhase(false);// make sure positive motor output means sensor moves in position direction */
            // call ConfigEncoderCodesPerRev or ConfigPotentiometerTurns for Quadrature or Analog sensor types.
            leftBack.ConfigFactoryDefault();
            rightBack.ConfigFactoryDefault();
            

            /* brake or coast during neutral */
            leftFront.SetNeutralMode(NeutralMode.Brake);
            rightFront.SetNeutralMode(NeutralMode.Brake);

            intake.SetNeutralMode(NeutralMode.Brake);

            /* closed-loop and motion-magic parameters */
            leftFront.Config_kF(kSlotIdx, 0.0276f, kTimeoutMs); // 8874 native sensor units per 100ms at full motor output (+1023)
            leftFront.Config_kP(kSlotIdx, 0.55f, kTimeoutMs);
            leftFront.Config_kI(kSlotIdx, 0.0f, kTimeoutMs);
            leftFront.Config_kD(kSlotIdx, 20f, kTimeoutMs);
            leftFront.Config_IntegralZone(kSlotIdx, 30, kTimeoutMs);
            leftFront.SelectProfileSlot(kSlotIdx, 0); /* select this slot */
            leftFront.ConfigNominalOutputForward(0f, kTimeoutMs);
            leftFront.ConfigNominalOutputReverse(0f, kTimeoutMs);
            leftFront.ConfigPeakOutputForward(1.0f, kTimeoutMs);
            leftFront.ConfigPeakOutputReverse(-1.0f, kTimeoutMs);
            leftFront.ConfigMotionCruiseVelocity(8000, kTimeoutMs); // 8000 native units
            leftFront.ConfigMotionAcceleration(16000, kTimeoutMs); // 16000 native units per sec, (0.5s to reach cruise velocity).

            
            rightFront.Config_kF(kSlotIdx, 0.0276f, kTimeoutMs); // 8874 native sensor units per 100ms at full motor output (+1023)
            rightFront.Config_kP(kSlotIdx, 0.55f, kTimeoutMs);
            rightFront.Config_kI(kSlotIdx, 0.0f, kTimeoutMs);
            rightFront.Config_kD(kSlotIdx, 20f, kTimeoutMs);
            rightFront.Config_IntegralZone(kSlotIdx, 30, kTimeoutMs);
            rightFront.SelectProfileSlot(kSlotIdx, 0); /* select this slot */
            rightFront.ConfigNominalOutputForward(0f, kTimeoutMs);
            rightFront.ConfigNominalOutputReverse(0f, kTimeoutMs);
            rightFront.ConfigPeakOutputForward(1.0f, kTimeoutMs);
            rightFront.ConfigPeakOutputReverse(-1.0f, kTimeoutMs);
            rightFront.ConfigMotionCruiseVelocity(8000, kTimeoutMs); // 8000 native units
            rightFront.ConfigMotionAcceleration(16000, kTimeoutMs); // 16000 native units per sec, (0.5s to reach cruise velocity).

            
            /* Home the relative sensor, 
                alternatively you can throttle until limit switch,
                use an absolute signal like CtreMagEncoder_Absolute or analog sensor.
                */
            leftFront.SetSelectedSensorPosition(0);
            rightFront.SetSelectedSensorPosition(0);

            //Reverse the right encoder
            rightFront.SetSensorPhase(true);
            leftFront.SetSensorPhase(false);

            leftBack.Follow(leftFront);
            rightBack.Follow(rightFront);
        }
        /** spin in this routine forever */
        public bool reset = false;

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
                bool btnTopLeftShoulder = _gamepad.GetButton(5);
                bool btnBtmLeftShoulder = _gamepad.GetButton(7);
                bool buttonTop= _gamepad.GetButton(6);
                bool buttonBottom = _gamepad.GetButton(8);
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
                    leftFront.Set(_mode, (float)System.Math.Pow(leftY, 3) - (float)System.Math.Pow(rightX, 3));
                    rightFront.Set(_mode, -(float)System.Math.Pow(leftY, 3) - (float)System.Math.Pow(rightX, 3)); //need to be reversed

                    //Intake
                    if (buttonTop) {
                        intake.Set(_mode, 1);
                    } else if (buttonBottom) {
                        intake.Set(_mode, -1);
                    } else {
                        intake.Set(_mode, 0);
                    }
                    Debug.Print("Left Encoder: " + leftFront.GetSelectedSensorPosition() + "   Right Encoder: " + rightFront.GetSelectedSensorPosition());
                }

                else if (_mode == ControlMode.MotionMagic)
                {
                    float servoToRotation_leftY = leftY * 40960;// [-10, +10] rotations
                    float servoToRotation_rightY = rightY * 40960;
                    float servoToRotation_rightX = rightX * 40960;

                    //Tank Drive
                    leftFront.Set(_mode, servoToRotation_leftY - servoToRotation_rightX);
                    rightFront.Set(_mode, -servoToRotation_leftY - servoToRotation_rightX);
                }
                /* instrumentation */
                Instrument.Process(leftFront);
                Instrument.Process(leftBack);
                Instrument.Process(rightFront);
                Instrument.Process(rightBack);

                Instrument.Process(intake);

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

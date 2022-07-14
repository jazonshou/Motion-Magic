# CTRE Motion Magic Test

In this project, I experimented with CTRE's [motion magic](https://docs.ctre-phoenix.com/en/stable/ch16_ClosedLoop.html#motion-magic-control-mode) to smoothly control motor movements. 

## Hardware

* [HERO Dev Board](https://store.ctr-electronics.com/hero-development-board/) - like a Raspberry Pi/Arduino, made for CTRE products 
* [Talon SRX](https://store.ctr-electronics.com/talon-srx/) - motor controller used to control DC motors; made to be compatible with HERO board
* [Mini CIM Motor](https://www.vexrobotics.com/217-3371.html) - DC motor used in the project; any 12v DC motor can be used with this setup

## Electrical

* Motor controllers communicate with HERO board via its CAN network
* Motor controllers are powered via an external 12v power source (ex. car battery)

## Software

* Programmed with C# in Visual Studio 2019; uses .NET Micro framework
* Configure Talon SRX's with [HERO Lifeboat](https://github.com/CrossTheRoadElec/Phoenix-Releases/releases)

### Basic Overview of CTRE's Motion Magic

Motion Magic generates motion profiles with smooth acceleration/deceleration for clean movement. You can customize cruise velocity, max acceleration, and max jerk for maximum accuracy. 

[image]

## Usage

Use at your own risk. You are probably better off copying CTRE's [example projects](https://github.com/CrossTheRoadElec/Phoenix-Examples-Languages/tree/master/HERO%20C%23). 

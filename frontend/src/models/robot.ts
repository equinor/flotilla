import { Battery, BatteryStatus } from './battery'
import { Pose, defaultPose } from './pose'

export enum RobotStatus {
    Available = 'Available',
    Offline = 'Offline',
    MissionInProgress = 'Mission in progress',
}

export enum RobotType {
    Taurob = 'Taurob',
    ExRobotics = 'ExRobotics',
    TurtleBot = 'TurtleBot',
    NoneType = 'NoneType',
}

export interface RobotInfo {
    name: string
    type: RobotType
}

export class Robot {
    robotInfo: RobotInfo
    battery: Battery
    pose: Pose = defaultPose
    status: RobotStatus = RobotStatus.Available

    constructor(robotInfo: RobotInfo, battery: Battery, pose: Pose, status: RobotStatus) {
        this.robotInfo = robotInfo
        this.battery = battery
        this.pose = pose
        this.status = status
    }
}

const taurobRobotInfo: RobotInfo = {
    name: 'William',
    type: RobotType.Taurob,
}

const exRobotInfo: RobotInfo = {
    name: 'Bertha',
    type: RobotType.ExRobotics,
}

const turtleRobotInfo: RobotInfo = {
    name: 'Edward',
    type: RobotType.TurtleBot,
}

export const defaultRobots: { [name: string]: Robot } = {
    taurob: new Robot(taurobRobotInfo, new Battery(BatteryStatus.Charging, 100), defaultPose, RobotStatus.Available),
    exRobotics: new Robot(
        exRobotInfo,
        new Battery(BatteryStatus.Normal, 59),
        defaultPose,
        RobotStatus.MissionInProgress
    ),
    turtle: new Robot(turtleRobotInfo, new Battery(BatteryStatus.Error), defaultPose, RobotStatus.Offline),
}

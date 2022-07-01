import { BatteryStatus } from './battery'
import { Pose } from './pose'
import { VideoStream } from './VideoStream'

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

export interface Robot {
    id: string
    name?: string
    model: RobotType
    serialNumber?: string
    battery?: number
    batteryStatus?: BatteryStatus
    pose?: Pose
    status?: RobotStatus
    enabled?: boolean
    host?: string
    logs?: string
    port?: number
    videoStreams?: VideoStream[]
}

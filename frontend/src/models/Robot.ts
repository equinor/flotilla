import { BatteryStatus } from './Battery'
import { Pose } from './Pose'
import { VideoStream } from './VideoStream'

export enum RobotStatus {
    Available = 'Available',
    Offline = 'Offline',
    Busy = 'Busy',
}

export enum RobotType {
    TaurobInspector = 'TaurobInspector',
    TaurobOperator = 'TaurobOperator',
    ExR2 = 'ExR2',
    Turtlebot = 'Turtlebot',
    Robot = 'Robot',
    AnymalX = 'AnymalX',
    AnymalD = 'AnymalD',
    NoneType = 'NoneType',
}

export namespace RobotType {
    export function toString(robotType: RobotType): string {
        switch (robotType) {
            case RobotType.TaurobInspector: {
                return 'Taurob Inspector'
            }
            case RobotType.TaurobOperator: {
                return 'Taurob Operator'
            }
            case RobotType.ExR2: {
                return 'ExR2'
            }
            case RobotType.Turtlebot: {
                return 'Turtlebot'
            }
            case RobotType.Robot: {
                return 'Robot'
            }
            case RobotType.AnymalX: {
                return 'ANYmal X'
            }
            case RobotType.AnymalD: {
                return 'ANYmal D'
            }
            default: {
                return 'Unknown'
            }
        }
    }
}

export interface Robot {
    id: string
    name?: string
    model: RobotType
    serialNumber?: string
    batteryLevel?: number
    batteryStatus?: BatteryStatus
    pressureLevel?: number
    pose?: Pose
    status?: RobotStatus
    enabled?: boolean
    host?: string
    logs?: string
    port?: number
    videoStreams?: VideoStream[]
    isarUri?: string
}

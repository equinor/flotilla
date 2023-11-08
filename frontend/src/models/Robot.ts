import { Area } from './Area'
import { BatteryStatus } from './Battery'
import { Pose } from './Pose'
import { RobotModel, placeholderRobotModel } from './RobotModel'
import { VideoStream } from './VideoStream'

export enum RobotStatus {
    Available = 'Available',
    Busy = 'Busy',
    Offline = 'Offline',
    Blocked = 'Blocked',
    SafeZone = 'Safe Zone',
}

export interface Robot {
    id: string
    name?: string
    model: RobotModel
    serialNumber?: string
    currentInstallation: string
    batteryLevel?: number
    batteryStatus?: BatteryStatus
    pressureLevel?: number
    pose?: Pose
    status: RobotStatus
    enabled?: boolean
    host?: string
    logs?: string
    port?: number
    videoStreams?: VideoStream[]
    isarUri?: string
    currentArea?: Area
    missionQueueFrozen?: boolean
}
export const placeholderRobot: Robot = {
    id: 'placeholderRobotId',
    model: placeholderRobotModel,
    currentInstallation: 'PlaceholderInstallation',
    status: RobotStatus.Available,
}

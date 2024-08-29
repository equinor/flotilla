import { Area } from './Area'
import { BatteryStatus } from './Battery'
import { DocumentInfo } from './DocumentInfo'
import { Installation, placeholderInstallation } from './Installation'
import { Pose } from './Pose'
import { RobotModel, placeholderRobotModel } from './RobotModel'
import { VideoStream } from './VideoStream'

export enum RobotStatus {
    Available = 'Available',
    Busy = 'Busy',
    Offline = 'Offline',
    Blocked = 'Blocked',
    SafeZone = 'Safe Zone',
    Recharging = 'Recharging',
    ConnectionIssues = 'Connection Issues',
}

export enum RobotFlotillaStatus {
    Normal = 'Normal',
    SafeZone = 'SafeZone',
    Recharging = 'Recharging',
}

export interface Robot {
    id: string
    name?: string
    model: RobotModel
    serialNumber?: string
    currentInstallation: Installation
    batteryLevel?: number
    batteryStatus?: BatteryStatus
    pressureLevel?: number
    pose?: Pose
    status: RobotStatus
    isarConnected: boolean
    deprecated: boolean
    host?: string
    logs?: string
    port?: number
    documentation?: DocumentInfo[]
    videoStreams?: VideoStream[]
    isarUri?: string
    currentArea?: Area
    flotillaStatus?: RobotFlotillaStatus
}
export const placeholderRobot: Robot = {
    id: 'placeholderRobotId',
    model: placeholderRobotModel,
    currentInstallation: placeholderInstallation,
    status: RobotStatus.Available,
    isarConnected: true,
    deprecated: false,
}

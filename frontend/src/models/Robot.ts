import { InspectionArea } from './InspectionArea'
import { BatteryStatus } from './Battery'
import { DocumentInfo } from './DocumentInfo'
import { Installation, placeholderInstallation } from './Installation'
import { Pose } from './Pose'
import { RobotModel, placeholderRobotModel } from './RobotModel'

export enum RobotStatus {
    Available = 'Available',
    Busy = 'Busy',
    Offline = 'Offline',
    Blocked = 'Blocked',
    BlockedProtectiveStop = 'BlockedProtectiveStop',
    Docked = 'Docked',
    Recharging = 'Recharging',
    ConnectionIssues = 'Connection Issues',
}

export enum RobotFlotillaStatus {
    Normal = 'Normal',
    Docked = 'Docked',
    Recharging = 'Recharging',
}

export interface Robot {
    id: string
    name?: string
    model: RobotModel
    serialNumber?: string
    currentInstallation: Installation
    batteryLevel?: number
    batteryState?: BatteryStatus
    pressureLevel?: number
    pose?: Pose
    status: RobotStatus
    robotCapabilities?: RobotCapabilitiesEnum[]
    isarConnected: boolean
    deprecated: boolean
    host?: string
    logs?: string
    port?: number
    documentation?: DocumentInfo[]
    isarUri?: string
    currentInspectionArea?: InspectionArea
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

enum RobotCapabilitiesEnum {
    take_thermal_image = 'take_thermal_image',
    take_image = 'take_image',
    take_video = 'take_video',
    take_thermal_video = 'take_thermal_video',
    take_gas_measurement = 'take_gas_measurement',
    record_audio = 'record_audio',
    auto_return_to_home = 'auto_return_to_home',
    return_to_home = 'return_to_home',
}

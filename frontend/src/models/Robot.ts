import { DocumentInfo } from './DocumentInfo'
import { Installation, placeholderInstallation } from './Installation'

export enum RobotType {
    TaurobInspector = 'TaurobInspector',
    TaurobOperator = 'TaurobOperator',
    Robot = 'Robot',
    Turtlebot = 'Turtlebot',
    AnymalX = 'AnymalX',
    AnymalD = 'AnymalD',
    NoneType = 'NoneType',
}

export enum RobotStatus {
    Available = 'Available',
    Busy = 'Busy',
    Offline = 'Offline',
    BlockedProtectiveStop = 'BlockedProtectiveStop',
    Home = 'Home',
    Recharging = 'Recharging',
    ReturningHome = 'ReturningHome',
    ReturnHomePaused = 'ReturnHomePaused',
    Paused = 'Paused',
    ConnectionIssues = 'Connection Issues',
    UnknownStatus = 'UnknownStatus',
    InterventionNeeded = 'InterventionNeeded',
    Lockdown = 'Lockdown',
    GoingToLockdown = 'GoingToLockdown',
    GoingToRecharging = 'GoingToRecharging',
    Maintenance = 'Maintenance',
    Pausing = 'Pausing',
    PausingReturnHome = 'PausingReturnHome',
    Stopping = 'Stopping',
    StoppingReturnHome = 'StoppingReturnHome',
}

export interface RobotWithoutTelemetry {
    id: string
    name?: string
    serialNumber?: string
    currentInstallation: Installation
    status: RobotStatus
    robotCapabilities?: RobotCapabilitiesEnum[]
    isarConnected: boolean
    disconnectTime?: Date
    deprecated: boolean
    host?: string
    logs?: string
    port?: number
    documentation?: DocumentInfo[]
    isarUri?: string
    currentInspectionAreaId?: string
    type: RobotType
}
export const placeholderRobot: RobotWithoutTelemetry = {
    id: 'placeholderRobotId',
    currentInstallation: placeholderInstallation,
    status: RobotStatus.Available,
    isarConnected: true,
    deprecated: false,
    disconnectTime: undefined,
    type: RobotType.Robot,
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

export const getRobotTypeString = (type: RobotType): string => {
    if (type === RobotType.TaurobInspector || type === RobotType.TaurobOperator) return 'Taurob'
    return type.toString()
}

export interface RobotPropertyUpdate {
    robotId: string
    propertyName: string
    propertyValue: any
}

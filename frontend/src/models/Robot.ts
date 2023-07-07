import { AssetDeck } from './AssetDeck'
import { BatteryStatus } from './Battery'
import { Pose } from './Pose'
import { RobotModel } from './RobotModel'
import { VideoStream } from './VideoStream'

export enum RobotStatus {
    Available = 'Available',
    Offline = 'Offline',
    Busy = 'Busy',
}

export interface Robot {
    id: string
    name?: string
    model: RobotModel
    serialNumber?: string
    currentAsset: string
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
    currentAssetDeck?: AssetDeck
}

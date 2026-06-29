import { Pose } from './Pose'
import { Position } from './Position'

interface Analysis {
    id: string
    name: string
    createdAt: string
    anonymizedSAS: string
    visualizedSAS: string
}

export interface InspectionRecord {
    id: string
    inspectionId: string
    installationCode: string
    sasToken: string
    createdAt: Date
    inspectionType: string
    tag: string
    targetPosition: Position
    robotPose: Pose
    analyses: Analysis[]
    inspectionDescription: string
    robotName: string
    timestamp: number
    analysisGroupId: string
}

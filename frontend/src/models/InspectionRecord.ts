import { AnalysisType } from './MissionDefinition'
import { Pose } from './Pose'
import { Position } from './Position'

interface AnalysisResult {
    analysisId: string
    analysisType: string
    value?: string
    unit?: string
    confidence?: number
    warning?: string
}

interface Analysis {
    id: string
    name: string
    createdAt: string
    anonymizedSAS: string
    visualizedSAS: string
    result?: AnalysisResult
}

export enum FileType {
    VIDEO,
    IMAGE,
    SOUND,
    VALUE,
}

export interface InspectionRecord {
    id: string
    inspectionId: string
    installationCode: string
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

export interface FlotillaAnalysisResultMessage {
    inspectionId: string
    analysisType: AnalysisType
    installationCode: string
}

export interface InspectionData {
    inspectionId: string
    analysisId: string
    visualisedSAS: string
    fileType: FileType
    anonymizedSAS: string
    analysisType: string
    tag: string
    createdAt: Date
    targetPosition: Position
    robotPose: Pose
    inspectionDescription: string
    value?: string
    unit?: string
    confidence?: number
    warning?: string
}

const imageFileEndings = ['jpg', 'jpeg', 'png', 'gif']

const videoFileEndings = ['mp4', 'mpg', 'mpeg', 'm4v']

const sasURLToFileType = (sasURL: string): FileType => {
    const reg: RegExp = /\.([a-zA-Z0-9]+)(\?skoid=)/
    const fileEnding = sasURL.match(reg)?.[1]
    if (!fileEnding) return FileType.VALUE
    if (imageFileEndings.includes(fileEnding?.toLowerCase())) return FileType.IMAGE
    if (videoFileEndings.includes(fileEnding.toLowerCase())) return FileType.VIDEO
    return FileType.VALUE
}

export const inspectionRecordToInspectionData = (record: InspectionRecord): InspectionData => {
    const analysis = record.analyses[record.analyses.length - 1]
    const sas = analysis.visualizedSAS ?? analysis.anonymizedSAS
    const fileType = sasURLToFileType(sas)
    return {
        inspectionId: record.inspectionId,
        analysisId: analysis.id,
        visualisedSAS: analysis.visualizedSAS,
        anonymizedSAS: analysis.anonymizedSAS,
        analysisType: record.inspectionType,
        tag: record.tag,
        createdAt: record.createdAt,
        targetPosition: record.targetPosition,
        robotPose: record.robotPose,
        fileType: fileType,
        inspectionDescription: record.inspectionDescription,
        value: analysis.result?.value,
        unit: analysis.result?.unit,
        confidence: analysis.result?.confidence,
        warning: analysis.result?.warning,
    }
}

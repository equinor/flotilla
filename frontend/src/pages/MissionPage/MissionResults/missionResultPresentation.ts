import { hasInspectionAnalysis, hasInspectionFinding, InspectionData } from 'models/InspectionRecord'
import { Task } from 'models/Task'

export type ResultFocus = 'inspection' | 'analysis'

export interface ResultMedia {
    src: string
    type: 'image' | 'video' | 'unsupported'
}

export interface MissionResult {
    inspection: InspectionData
    taskNumber: number
    sensorType: Task['sensorType']
    source?: ResultMedia
    analysis?: ResultMedia
    hasAnalysis: boolean
    hasFinding: boolean
}

const mediaFromUrl = (src: string | undefined): ResultMedia | undefined => {
    if (!src) return undefined
    let extension: string | undefined
    try {
        extension = new URL(src).pathname.split('.').pop()?.toLowerCase()
    } catch {
        return { src, type: 'unsupported' }
    }
    const type = ['jpg', 'jpeg', 'png', 'gif'].includes(extension ?? '')
        ? 'image'
        : ['mp4', 'mpg', 'mpeg', 'm4v'].includes(extension ?? '')
          ? 'video'
          : 'unsupported'
    return { src, type }
}

export const getMissionResults = (
    tasks: Pick<Task, 'id' | 'sensorType'>[],
    inspections: InspectionData[]
): MissionResult[] => {
    const byId = new Map(inspections.map((inspection) => [inspection.inspectionId, inspection]))
    const results = tasks.flatMap((task, index) => {
        const inspection = byId.get(task.id)
        if (!inspection) return []
        const source = mediaFromUrl(
            inspection.anonymizedSAS ??
                (inspection.mediaSAS !== inspection.visualizedSAS ? inspection.mediaSAS : undefined)
        )
        const analysis = mediaFromUrl(inspection.visualizedSAS !== source?.src ? inspection.visualizedSAS : undefined)
        const hasFinding = hasInspectionFinding(inspection)
        const hasAnalysis = hasInspectionAnalysis(inspection)
        if (!source && !hasAnalysis) return []
        return [
            {
                inspection,
                taskNumber: index + 1,
                sensorType: task.sensorType,
                source,
                analysis,
                hasAnalysis,
                hasFinding,
            },
        ]
    })
    return results.sort((left, right) => Number(right.hasFinding) - Number(left.hasFinding))
}

export const getResultFocus = (result: MissionResult, preferred: ResultFocus): ResultFocus =>
    preferred === 'analysis' && result.hasAnalysis ? 'analysis' : result.source ? 'inspection' : 'analysis'

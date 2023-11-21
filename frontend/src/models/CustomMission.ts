import { InspectionType } from './Inspection'
import { Mission } from './Mission'
import { Pose } from './Pose'
import { Position } from './Position'

export interface CustomMissionQuery {
    name: string
    description?: string
    comment?: string
    installationCode?: string
    robotId: string
    desiredStartTime: Date
    tasks: CustomTaskQuery[]
}

export interface CustomTaskQuery {
    taskOrder: number
    inspectionTarget: Position
    tagId?: string
    description?: string
    robotPose: Pose
    inspections: CustomInspectionQuery[]
}

export interface CustomInspectionQuery {
    inspectionType: InspectionType
    inspectionTarget: Position
    videoDuration?: number
    analysisType?: string
}

export const createCustomMission = (mission: Mission): CustomMissionQuery => {
    const customMission: CustomMissionQuery = {
        name: mission.name,
        description: mission.description,
        comment: mission.comment,
        installationCode: mission.installationCode,
        robotId: mission.robot.id,
        desiredStartTime: new Date(),
        tasks: mission.tasks.map<CustomTaskQuery>((task) => {
            const customTask: CustomTaskQuery = {
                taskOrder: task.taskOrder,
                inspectionTarget: task.inspectionTarget,
                tagId: task.tagId,
                description: task.description,
                robotPose: task.robotPose,
                inspections: task.inspections.map<CustomInspectionQuery>((inspection) => {
                    const customInspection: CustomInspectionQuery = {
                        inspectionType: inspection.inspectionType,
                        inspectionTarget: inspection.inspectionTarget,
                        videoDuration: inspection.videoDuration,
                        analysisType: inspection.analysisType,
                    }
                    return customInspection
                }),
            }
            return customTask
        }),
    }
    return customMission
}

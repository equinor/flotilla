import { InspectionType } from './Inspection'
import { Mission } from './Mission'
import { Pose } from './Pose'
import { Position } from './Position'

export interface CustomMissionQuery {
    name: string
    description?: string
    comment?: string
    assetCode?: string
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
    videoDuration?: number
    analysisTypes?: string
}

export function CreateCustomMission(mission: Mission): CustomMissionQuery {
    const customMission: CustomMissionQuery = {
        name: mission.name,
        description: mission.description,
        comment: mission.comment,
        assetCode: mission.assetCode,
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
                        videoDuration: inspection.videoDuration,
                        analysisTypes: inspection.analysisTypes,
                    }
                    return customInspection
                }),
            }
            return customTask
        }),
    }
    return customMission
}

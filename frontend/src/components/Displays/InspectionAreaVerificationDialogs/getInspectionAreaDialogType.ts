import { InspectionArea } from 'models/InspectionArea'
import { RobotWithoutTelemetry } from 'models/Robot'

export enum InspectionAreaDialogType {
    unknownNewInspectionArea,
    conflictingMissionInspectionAreas,
    conflictingRobotInspectionArea,
}

export const getUniqueInspectionAreas = (inspectionAreas: InspectionArea[]): InspectionArea[] =>
    inspectionAreas.filter((area, index, self) => self.findIndex((i) => i.id === area.id) === index)

export const getInspectionAreaDialogType = (
    robot: RobotWithoutTelemetry | undefined,
    missionInspectionAreas: InspectionArea[]
): InspectionAreaDialogType | null => {
    const uniqueInspectionAreas = getUniqueInspectionAreas(missionInspectionAreas)

    if (uniqueInspectionAreas.length === 0) return InspectionAreaDialogType.unknownNewInspectionArea
    if (uniqueInspectionAreas.length > 1) return InspectionAreaDialogType.conflictingMissionInspectionAreas

    const robotIsInDifferentArea =
        !!robot?.currentInspectionAreaId && uniqueInspectionAreas[0]?.id !== robot.currentInspectionAreaId
    if (robotIsInDifferentArea) return InspectionAreaDialogType.conflictingRobotInspectionArea

    return null
}

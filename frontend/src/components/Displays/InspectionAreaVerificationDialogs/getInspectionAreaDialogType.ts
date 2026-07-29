import { InspectionArea } from 'models/InspectionArea'
import { RobotWithoutTelemetry } from 'models/Robot'

export enum InspectionAreaDialogType {
    unknownNewInspectionArea,
    conflictingMissionInspectionAreas,
    conflictingRobotInspectionArea,
    unknown,
}

export const getUniqueInspectionAreas = (inspectionAreas: InspectionArea[]): InspectionArea[] =>
    inspectionAreas.filter((area, index, self) => self.findIndex((i) => i.id === area.id) === index)

/**
 * Decides which inspection-area verification dialog (if any) must be shown before a
 * mission can be scheduled. Returns null when scheduling can proceed directly.
 *
 * Kept as a pure function so the decision happens in the click handler rather than in a
 * render effect, which avoids duplicate scheduling requests under React StrictMode.
 */
export const getInspectionAreaDialogType = (
    robot: RobotWithoutTelemetry | undefined,
    missionInspectionAreas: InspectionArea[]
): InspectionAreaDialogType | null => {
    if (!robot) return InspectionAreaDialogType.unknown

    const uniqueInspectionAreas = getUniqueInspectionAreas(missionInspectionAreas)

    if (uniqueInspectionAreas.length > 1) return InspectionAreaDialogType.conflictingMissionInspectionAreas
    if (uniqueInspectionAreas.length === 0) return InspectionAreaDialogType.unknownNewInspectionArea
    if (robot.currentInspectionAreaId && uniqueInspectionAreas[0]?.id !== robot.currentInspectionAreaId)
        return InspectionAreaDialogType.conflictingRobotInspectionArea

    return null
}

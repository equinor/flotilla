import {
    ConflictingMissionInspectionAreasDialog,
    ConflictingRobotInspectionAreaDialog,
} from './ConflictingInspectionAreaDialog'
import { UnknownInspectionAreaDialog } from './UnknownInspectionAreaDialog'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { InspectionArea } from 'models/InspectionArea'
import { RobotWithoutTelemetry } from 'models/Robot'

interface IProps {
    closeDialog: () => void
    robotId: string
    missionInspectionAreas: InspectionArea[]
}

enum DialogTypes {
    unknownNewInspectionArea,
    conflictingMissionInspectionAreas,
    conflictingRobotInspectionArea,
    unknown,
}

const getUniqueInspectionAreas = (missionInspectionAreas: InspectionArea[]) =>
    missionInspectionAreas.filter(
        (inspectionArea, index, self) => self.findIndex((i) => i.id === inspectionArea.id) === index
    )

// Returns which verification dialog to show, or undefined when the mission can
// be scheduled without asking the operator to confirm.
export const getInspectionAreaDialogType = (
    robot: RobotWithoutTelemetry | undefined,
    missionInspectionAreas: InspectionArea[]
): DialogTypes | undefined => {
    if (!robot) return DialogTypes.unknown

    const uniqueInspectionAreas = getUniqueInspectionAreas(missionInspectionAreas)
    if (uniqueInspectionAreas.length > 1) return DialogTypes.conflictingMissionInspectionAreas
    if (uniqueInspectionAreas.length === 0) return DialogTypes.unknownNewInspectionArea
    if (robot.currentInspectionAreaId && uniqueInspectionAreas[0]?.id !== robot.currentInspectionAreaId) {
        return DialogTypes.conflictingRobotInspectionArea
    }
    return undefined
}

export const ScheduleMissionWithInspectionAreaVerification = ({
    robotId,
    missionInspectionAreas,
    closeDialog,
}: IProps) => {
    const { enabledRobots } = useAssetContext()

    const selectedRobot = enabledRobots.find((robot) => robot.id === robotId)

    const dialogToOpen = getInspectionAreaDialogType(selectedRobot, missionInspectionAreas) ?? DialogTypes.unknown

    const unikMissionInspectionAreaNames = getUniqueInspectionAreas(missionInspectionAreas).map(
        (area) => area?.inspectionAreaName ?? ''
    )

    return (
        <>
            {dialogToOpen === DialogTypes.conflictingMissionInspectionAreas && (
                <ConflictingMissionInspectionAreasDialog
                    closeDialog={closeDialog}
                    missionInspectionAreaNames={unikMissionInspectionAreaNames}
                />
            )}
            {dialogToOpen === DialogTypes.conflictingRobotInspectionArea && selectedRobot?.currentInspectionAreaId && (
                <ConflictingRobotInspectionAreaDialog
                    closeDialog={closeDialog}
                    robotInspectionAreaId={selectedRobot?.currentInspectionAreaId}
                    desiredInspectionAreaName={unikMissionInspectionAreaNames![0]}
                />
            )}
            {dialogToOpen === DialogTypes.unknownNewInspectionArea && (
                <UnknownInspectionAreaDialog closeDialog={closeDialog} />
            )}
        </>
    )
}

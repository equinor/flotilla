import {
    ConflictingMissionInspectionAreasDialog,
    ConflictingRobotInspectionAreaDialog,
} from './ConflictingInspectionAreaDialog'
import { UnknownInspectionAreaDialog } from './UnknownInspectionAreaDialog'
import { InspectionArea } from 'models/InspectionArea'
import { RobotWithoutTelemetry } from 'models/Robot'
import { getUniqueInspectionAreas, InspectionAreaDialogType } from './getInspectionAreaDialogType'

interface IProps {
    dialogType: InspectionAreaDialogType
    closeDialog: () => void
    robot: RobotWithoutTelemetry | undefined
    missionInspectionAreas: InspectionArea[]
}

export const ScheduleMissionWithInspectionAreaVerification = ({
    dialogType,
    closeDialog,
    robot,
    missionInspectionAreas,
}: IProps) => {
    const uniqueMissionInspectionAreaNames = getUniqueInspectionAreas(missionInspectionAreas).map(
        (area) => area?.inspectionAreaName ?? ''
    )

    return (
        <>
            {dialogType === InspectionAreaDialogType.conflictingMissionInspectionAreas && (
                <ConflictingMissionInspectionAreasDialog
                    closeDialog={closeDialog}
                    missionInspectionAreaNames={uniqueMissionInspectionAreaNames}
                />
            )}
            {dialogType === InspectionAreaDialogType.conflictingRobotInspectionArea &&
                robot?.currentInspectionAreaId && (
                    <ConflictingRobotInspectionAreaDialog
                        closeDialog={closeDialog}
                        robotInspectionAreaId={robot.currentInspectionAreaId}
                        desiredInspectionAreaName={uniqueMissionInspectionAreaNames[0]}
                    />
                )}
            {dialogType === InspectionAreaDialogType.unknownNewInspectionArea && (
                <UnknownInspectionAreaDialog closeDialog={closeDialog} />
            )}
        </>
    )
}

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

export const InspectionAreaVerificationDialog = ({
    dialogType,
    closeDialog,
    robot,
    missionInspectionAreas,
}: IProps) => {
    const uniqueMissionInspectionAreaNames = getUniqueInspectionAreas(missionInspectionAreas).map(
        (area) => area?.inspectionAreaName ?? ''
    )

    switch (dialogType) {
        case InspectionAreaDialogType.conflictingMissionInspectionAreas:
            return (
                <ConflictingMissionInspectionAreasDialog
                    closeDialog={closeDialog}
                    missionInspectionAreaNames={uniqueMissionInspectionAreaNames}
                />
            )
        case InspectionAreaDialogType.conflictingRobotInspectionArea:
            if (!robot?.currentInspectionAreaId) return null
            return (
                <ConflictingRobotInspectionAreaDialog
                    closeDialog={closeDialog}
                    robotInspectionAreaId={robot.currentInspectionAreaId}
                    desiredInspectionAreaName={uniqueMissionInspectionAreaNames[0]}
                />
            )
        case InspectionAreaDialogType.unknownNewInspectionArea:
            return <UnknownInspectionAreaDialog closeDialog={closeDialog} />
    }
}

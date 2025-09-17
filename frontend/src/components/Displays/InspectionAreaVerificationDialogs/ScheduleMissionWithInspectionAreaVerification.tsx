import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import {
    ConflictingMissionInspectionAreasDialog,
    ConflictingRobotInspectionAreaDialog,
} from './ConflictingInspectionAreaDialog'
import { UnknownInspectionAreaDialog } from './UnknownInspectionAreaDialog'
import { useAssetContext } from 'components/Contexts/RobotContext'
import { InspectionArea } from 'models/InspectionArea'

interface IProps {
    scheduleMissions: () => void
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

export const ScheduleMissionWithInspectionAreaVerification = ({
    robotId,
    missionInspectionAreas,
    scheduleMissions,
    closeDialog,
}: IProps) => {
    const { enabledRobots } = useAssetContext()
    const [dialogToOpen, setDialogToOpen] = useState<DialogTypes>(DialogTypes.unknown)
    const [selectedRobot, setSelectedRobot] = useState<Robot>()

    const unikMissionInspectionAreas = missionInspectionAreas.filter(
        (inspectionArea, index, self) => self.findIndex((i) => i.id === inspectionArea.id) === index
    )

    useEffect(() => {
        setSelectedRobot(enabledRobots.find((robot) => robot.id === robotId))
    }, [robotId, enabledRobots])

    useEffect(() => {
        if (!selectedRobot) return

        if (unikMissionInspectionAreas.length > 1) {
            setDialogToOpen(DialogTypes.conflictingMissionInspectionAreas)
            return
        }

        if (unikMissionInspectionAreas.length === 0) {
            setDialogToOpen(DialogTypes.unknownNewInspectionArea)
            return
        }
        if (
            selectedRobot.currentInspectionAreaId &&
            unikMissionInspectionAreas[0]?.id !== selectedRobot.currentInspectionAreaId
        ) {
            setDialogToOpen(DialogTypes.conflictingRobotInspectionArea)
            return
        }

        scheduleMissions()
    }, [unikMissionInspectionAreas, selectedRobot])

    const unikMissionInspectionAreaNames = unikMissionInspectionAreas.map((area) => area?.inspectionAreaName ?? '')

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

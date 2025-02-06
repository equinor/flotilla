import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import {
    ConflictingMissionInspectionAreasDialog,
    ConflictingRobotInspectionAreaDialog,
} from './ConflictingInspectionAreaDialog'
import { UnknownInspectionAreaDialog } from './UnknownInspectionAreaDialog'
import { useRobotContext } from 'components/Contexts/RobotContext'

interface IProps {
    scheduleMissions: () => void
    closeDialog: () => void
    robotId: string
    missionInspectionAreaNames: string[]
}

enum DialogTypes {
    unknownNewInspectionArea,
    conflictingMissionInspectionAreas,
    conflictingRobotInspectionArea,
    unknown,
}

export const ScheduleMissionWithInspectionAreaVerification = ({
    robotId,
    missionInspectionAreaNames,
    scheduleMissions,
    closeDialog,
}: IProps): JSX.Element => {
    const { enabledRobots } = useRobotContext()
    const [dialogToOpen, setDialogToOpen] = useState<DialogTypes>(DialogTypes.unknown)
    const [selectedRobot, setSelectedRobot] = useState<Robot>()

    const unikMissionInspectionAreaNames = missionInspectionAreaNames.filter(
        (inspectionAreaName, index) =>
            inspectionAreaName !== '' && missionInspectionAreaNames.indexOf(inspectionAreaName) === index
    )

    useEffect(() => {
        setSelectedRobot(enabledRobots.find((robot) => robot.id === robotId))
    }, [robotId, enabledRobots])

    useEffect(() => {
        if (!selectedRobot) return

        if (unikMissionInspectionAreaNames.length > 1) {
            setDialogToOpen(DialogTypes.conflictingMissionInspectionAreas)
            return
        }

        if (unikMissionInspectionAreaNames.length === 0) {
            setDialogToOpen(DialogTypes.unknownNewInspectionArea)
            return
        }

        if (
            selectedRobot.currentInspectionArea?.inspectionAreaName &&
            unikMissionInspectionAreaNames[0] !== selectedRobot.currentInspectionArea?.inspectionAreaName
        ) {
            setDialogToOpen(DialogTypes.conflictingRobotInspectionArea)
            return
        }

        scheduleMissions()
    }, [unikMissionInspectionAreaNames, selectedRobot?.currentInspectionArea?.inspectionAreaName])

    return (
        <>
            {dialogToOpen === DialogTypes.conflictingMissionInspectionAreas && (
                <ConflictingMissionInspectionAreasDialog
                    closeDialog={closeDialog}
                    missionInspectionAreaNames={unikMissionInspectionAreaNames!}
                />
            )}
            {dialogToOpen === DialogTypes.conflictingRobotInspectionArea &&
                selectedRobot?.currentInspectionArea?.inspectionAreaName && (
                    <ConflictingRobotInspectionAreaDialog
                        closeDialog={closeDialog}
                        robotInspectionAreaName={selectedRobot?.currentInspectionArea?.inspectionAreaName}
                        desiredInspectionAreaName={unikMissionInspectionAreaNames![0]}
                    />
                )}
            {dialogToOpen === DialogTypes.unknownNewInspectionArea && (
                <UnknownInspectionAreaDialog closeDialog={closeDialog} />
            )}
        </>
    )
}

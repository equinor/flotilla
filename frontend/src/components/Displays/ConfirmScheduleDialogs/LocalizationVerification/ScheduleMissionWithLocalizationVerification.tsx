import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import { ConfirmLocalizationDialog } from './ConfirmLocalizationDialog'
import {
    ConflictingMissionInspectionAreasDialog,
    ConflictingRobotInspectionAreaDialog,
} from './ConflictingLocalizationDialog'
import { UnknownInspectionAreaDialog } from './UnknownInspectionAreaDialog'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'

interface IProps {
    scheduleMissions: () => void
    closeDialog: () => void
    robotId: string
    missionInspectionAreaNames: string[]
}

enum DialogTypes {
    verifyInspectionArea,
    unknownNewInspectionArea,
    conflictingMissionInspectionAreas,
    conflictingRobotInspectionArea,
    unknown,
}

export const ScheduleMissionWithLocalizationVerificationDialog = ({
    robotId,
    missionInspectionAreaNames,
    scheduleMissions,
    closeDialog,
}: IProps): JSX.Element => {
    const { enabledRobots } = useRobotContext()
    const [dialogToOpen, setDialogToOpen] = useState<DialogTypes>(DialogTypes.unknown)
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const { ongoingMissions } = useMissionsContext()

    const unikMissionInspectionAreaNames = missionInspectionAreaNames.filter(
        (inspectionAreaName, index) =>
            inspectionAreaName !== '' && missionInspectionAreaNames.indexOf(inspectionAreaName) === index
    )

    const ongoingLocalizationMissionOnSameInspectionAreaExists =
        ongoingMissions.filter(
            (mission) =>
                mission.robot?.id === selectedRobot?.id &&
                mission.inspectionArea?.inspectionAreaName === unikMissionInspectionAreaNames[0]
        ).length > 0

    useEffect(() => {
        setSelectedRobot(enabledRobots.find((robot) => robot.id === robotId))
    }, [robotId, enabledRobots])

    useEffect(() => {
        if (!selectedRobot) return

        if (
            (unikMissionInspectionAreaNames.length === 1 &&
                selectedRobot.currentInspectionArea?.inspectionAreaName &&
                unikMissionInspectionAreaNames[0] === selectedRobot?.currentInspectionArea?.inspectionAreaName) ||
            ongoingLocalizationMissionOnSameInspectionAreaExists
        ) {
            scheduleMissions()
            return
        }

        if (unikMissionInspectionAreaNames.length > 1) {
            setDialogToOpen(DialogTypes.conflictingMissionInspectionAreas)
            return
        }

        if (unikMissionInspectionAreaNames.length === 0) {
            setDialogToOpen(DialogTypes.unknownNewInspectionArea)
            return
        }

        if (
            !selectedRobot.currentInspectionArea?.inspectionAreaName &&
            !ongoingLocalizationMissionOnSameInspectionAreaExists
        ) {
            setDialogToOpen(DialogTypes.verifyInspectionArea)
            return
        }

        if (unikMissionInspectionAreaNames[0] !== selectedRobot.currentInspectionArea?.inspectionAreaName) {
            setDialogToOpen(DialogTypes.conflictingRobotInspectionArea)
            return
        }
        // To ignore scheduleMissions dependency
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [unikMissionInspectionAreaNames, selectedRobot?.currentInspectionArea?.inspectionAreaName])

    return (
        <>
            {dialogToOpen === DialogTypes.verifyInspectionArea && (
                <ConfirmLocalizationDialog
                    closeDialog={closeDialog}
                    scheduleMissions={scheduleMissions}
                    robot={selectedRobot!}
                    newInspectionAreaName={unikMissionInspectionAreaNames![0]}
                />
            )}
            {dialogToOpen === DialogTypes.conflictingMissionInspectionAreas && (
                <ConflictingMissionInspectionAreasDialog
                    closeDialog={closeDialog}
                    missionInspectionAreaNames={unikMissionInspectionAreaNames!}
                />
            )}
            {dialogToOpen === DialogTypes.conflictingRobotInspectionArea && (
                <ConflictingRobotInspectionAreaDialog
                    closeDialog={closeDialog}
                    robotInspectionAreaName={selectedRobot?.currentInspectionArea?.inspectionAreaName!}
                    desiredInspectionAreaName={unikMissionInspectionAreaNames![0]}
                />
            )}
            {dialogToOpen === DialogTypes.unknownNewInspectionArea && (
                <UnknownInspectionAreaDialog closeDialog={closeDialog} />
            )}
        </>
    )
}

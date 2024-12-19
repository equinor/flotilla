import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import { ConfirmLocalizationDialog } from './ConfirmLocalizationDialog'
import { ConflictingMissionDecksDialog, ConflictingRobotDeckDialog } from './ConflictingLocalizationDialog'
import { UnknownDeckDialog } from './UnknownDeckDialog'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'

interface IProps {
    scheduleMissions: () => void
    closeDialog: () => void
    robotId: string
    missionDeckNames: string[]
}

enum DialogTypes {
    verifyDeck,
    unknownNewDeck,
    conflictingMissionDecks,
    conflictingRobotDeck,
    unknown,
}

export const ScheduleMissionWithLocalizationVerificationDialog = ({
    robotId,
    missionDeckNames,
    scheduleMissions,
    closeDialog,
}: IProps): JSX.Element => {
    const { enabledRobots } = useRobotContext()
    const [dialogToOpen, setDialogToOpen] = useState<DialogTypes>(DialogTypes.unknown)
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const { ongoingMissions } = useMissionsContext()

    const unikMissionDeckNames = missionDeckNames.filter(
        (deckName, index) => deckName !== '' && missionDeckNames.indexOf(deckName) === index
    )

    const ongoingLocalizationMissionOnSameDeckExists =
        ongoingMissions.filter(
            (mission) =>
                mission.robot?.id === selectedRobot?.id && mission.inspectionArea?.deckName === unikMissionDeckNames[0]
        ).length > 0

    useEffect(() => {
        setSelectedRobot(enabledRobots.find((robot) => robot.id === robotId))
    }, [robotId, enabledRobots])

    useEffect(() => {
        if (!selectedRobot) return

        if (
            (unikMissionDeckNames.length === 1 &&
                selectedRobot.currentInspectionArea?.deckName &&
                unikMissionDeckNames[0] === selectedRobot?.currentInspectionArea?.deckName) ||
            ongoingLocalizationMissionOnSameDeckExists
        ) {
            scheduleMissions()
            return
        }

        if (unikMissionDeckNames.length > 1) {
            setDialogToOpen(DialogTypes.conflictingMissionDecks)
            return
        }

        if (unikMissionDeckNames.length === 0) {
            setDialogToOpen(DialogTypes.unknownNewDeck)
            return
        }

        if (!selectedRobot.currentInspectionArea?.deckName && !ongoingLocalizationMissionOnSameDeckExists) {
            setDialogToOpen(DialogTypes.verifyDeck)
            return
        }

        if (unikMissionDeckNames[0] !== selectedRobot.currentInspectionArea?.deckName) {
            setDialogToOpen(DialogTypes.conflictingRobotDeck)
            return
        }
    }, [unikMissionDeckNames, selectedRobot?.currentInspectionArea?.deckName])

    return (
        <>
            {dialogToOpen === DialogTypes.verifyDeck && (
                <ConfirmLocalizationDialog
                    closeDialog={closeDialog}
                    scheduleMissions={scheduleMissions}
                    robot={selectedRobot!}
                    newDeckName={unikMissionDeckNames![0]}
                />
            )}
            {dialogToOpen === DialogTypes.conflictingMissionDecks && (
                <ConflictingMissionDecksDialog closeDialog={closeDialog} missionDeckNames={unikMissionDeckNames!} />
            )}
            {dialogToOpen === DialogTypes.conflictingRobotDeck && selectedRobot?.currentInspectionArea?.deckName && (
                <ConflictingRobotDeckDialog
                    closeDialog={closeDialog}
                    robotDeckName={selectedRobot?.currentInspectionArea?.deckName}
                    desiredDeckName={unikMissionDeckNames![0]}
                />
            )}
            {dialogToOpen === DialogTypes.unknownNewDeck && <UnknownDeckDialog closeDialog={closeDialog} />}
        </>
    )
}

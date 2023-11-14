import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import { ConfirmLocalizationDialog } from './ConfirmLocalizationDialog'
import { ConflictingMissionDecksDialog, ConflictingRobotDeckDialog } from './ConflictingLocalizationDialog'
import { UnknownDeckDialog } from './UnknownDeckDialog'
import { useRobotContext } from 'components/Contexts/RobotContext'

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

    const unikMissionDeckNames = missionDeckNames.filter(
        (deckName, index) => deckName !== '' && missionDeckNames.indexOf(deckName) === index
    )

    useEffect(() => {
        setSelectedRobot(enabledRobots.find((robot) => robot.id === robotId))
    }, [robotId, enabledRobots])

    useEffect(() => {
        if (!selectedRobot) return

        if (
            unikMissionDeckNames.length === 1 &&
            selectedRobot.currentArea?.deckName &&
            unikMissionDeckNames[0] === selectedRobot?.currentArea?.deckName
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

        if (!selectedRobot.currentArea?.deckName) {
            setDialogToOpen(DialogTypes.verifyDeck)
            return
        }

        if (unikMissionDeckNames[0] !== selectedRobot.currentArea?.deckName) {
            setDialogToOpen(DialogTypes.conflictingRobotDeck)
            return
        }
        // To ignore scheduleMissions dependency
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [unikMissionDeckNames, selectedRobot?.currentArea?.deckName])

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
            {dialogToOpen === DialogTypes.conflictingRobotDeck && (
                <ConflictingRobotDeckDialog
                    closeDialog={closeDialog}
                    robotDeckName={selectedRobot?.currentArea?.deckName!}
                    desiredDeckName={unikMissionDeckNames![0]}
                />
            )}
            {dialogToOpen === DialogTypes.unknownNewDeck && <UnknownDeckDialog closeDialog={closeDialog} />}
        </>
    )
}

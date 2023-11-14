import { Button, Dialog, List, Typography } from '@equinor/eds-core-react'
import { StyledDialog, VerticalContent } from './ScheduleMissionStyles'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

interface ConflictingRobotDeckDialogProps {
    closeDialog: () => void
    robotDeckName: string
    desiredDeckName: string
}

interface ConflictingMissionDecksDialogProps {
    closeDialog: () => void
    missionDeckNames: string[]
}

export const ConflictingRobotDeckDialog = ({
    closeDialog,
    robotDeckName,
    desiredDeckName,
}: ConflictingRobotDeckDialogProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledDialog open={true} onClose={closeDialog}>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Conflicting decks')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {TranslateText('The missions you are trying to add are on')} <b>{desiredDeckName}</b>{' '}
                        {TranslateText('but the robot is currently running missions on')} {<b>{robotDeckName}</b>}.
                    </Typography>
                    <Typography>{TranslateText('Will not be added dialog text')}</Typography>
                </VerticalContent>
            </Dialog.Content>
            <Dialog.Actions>
                <Button variant="outlined" color="danger" onClick={closeDialog}>
                    {TranslateText('Close')}
                </Button>
            </Dialog.Actions>
        </StyledDialog>
    )
}

export const ConflictingMissionDecksDialog = ({
    closeDialog,
    missionDeckNames,
}: ConflictingMissionDecksDialogProps) => {
    const { TranslateText } = useLanguageContext()

    const MissionDeckNamesList = (
        <List>
            {missionDeckNames.map((deckName) => (
                <List.Item>
                    <b>{deckName}</b>
                </List.Item>
            ))}
        </List>
    )

    return (
        <StyledDialog open={true} onClose={closeDialog}>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Conflicting decks')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {TranslateText('The missions you are trying to add are on these decks:')}
                        {MissionDeckNamesList}
                    </Typography>
                    <Typography>{TranslateText('You can only add missions from one deck.')}</Typography>
                </VerticalContent>
            </Dialog.Content>
            <Dialog.Actions>
                <Button variant="outlined" color="danger" onClick={closeDialog}>
                    {TranslateText('Close')}
                </Button>
            </Dialog.Actions>
        </StyledDialog>
    )
}

import { Button, Checkbox, Dialog, Typography } from '@equinor/eds-core-react'
import { DeckMapView } from 'utils/DeckMapView'
import { HorizontalContent, StyledDialog, VerticalContent } from './ScheduleMissionStyles'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import { ChangeEvent, useState } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'

interface ConfirmLocalizationDialogProps {
    closeDialog: () => void
    scheduleMissions: () => void
    robot: Robot
    newDeckName: string
}

export const ConfirmLocalizationDialog = ({
    closeDialog,
    scheduleMissions,
    robot,
    newDeckName,
}: ConfirmLocalizationDialogProps) => {
    const { TranslateText } = useLanguageContext()
    const { installationDecks } = useInstallationContext()
    const [isCheckboxClicked, setIsCheckboxClicked] = useState<boolean>(false)

    const newDeck = installationDecks.find((deck) => deck.deckName === newDeckName)

    return (
        <StyledDialog open={true} onClose={closeDialog}>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Confirm placement of robot')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {`${robot.name} (${robot.model.type}) ${TranslateText(
                            'needs to be placed on marked position on'
                        )} ${newDeckName} `}
                        <b>{TranslateText('before')}</b>
                        {` ${TranslateText('clicking confirm')}.`}
                    </Typography>
                    {newDeck && newDeck.defaultLocalizationPose && (
                        <DeckMapView deck={newDeck} markedRobotPosition={newDeck.defaultLocalizationPose}></DeckMapView>
                    )}
                    <HorizontalContent>
                        <Checkbox
                            crossOrigin={undefined}
                            onChange={(e: ChangeEvent<HTMLInputElement>) => setIsCheckboxClicked(e.target.checked)}
                        />
                        <Typography>
                            {`${TranslateText('I confirm that')} ${robot.name} (${robot.model.type}) ${TranslateText(
                                'has been placed on marked position on'
                            )} `}
                            <b>{newDeckName}</b>
                        </Typography>
                    </HorizontalContent>
                </VerticalContent>
            </Dialog.Content>
            <Dialog.Actions>
                <HorizontalContent>
                    <Button variant="outlined" onClick={closeDialog}>
                        {TranslateText('Cancel')}
                    </Button>
                    <Button onClick={scheduleMissions} disabled={!isCheckboxClicked}>
                        {TranslateText('Confirm')}
                    </Button>
                </HorizontalContent>
            </Dialog.Actions>
        </StyledDialog>
    )
}

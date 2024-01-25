import { Button, Checkbox, Dialog, Typography } from '@equinor/eds-core-react'
import { DeckMapView } from 'utils/DeckMapView'
import { HorizontalContent, StyledDialog, VerticalContent } from './ScheduleMissionStyles'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import { ChangeEvent, useCallback, useState } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'

interface ScheduleMissionChecklistDialogProps {
    closeDialog: () => void
    scheduleMissions: () => void
    robot: Robot
    missionDeckName: string
}

interface CheckDialogProps {
    setIsCheckConfirmed: (b: boolean) => void
    robot: Robot
    deckName: string
}

const LocalisationDialog = ({ setIsCheckConfirmed, robot, deckName }: CheckDialogProps) => {
    const { TranslateText } = useLanguageContext()
    const { installationDecks } = useInstallationContext()

    const newDeck = installationDecks.find((deck) => deck.deckName === deckName)

    return (
        <>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Confirm placement of robot')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {`${robot.name} (${robot.model.type}) ${TranslateText(
                            'needs to be placed on marked position on'
                        )} ${deckName} `}
                        <b>{TranslateText('before')}</b>
                        {` ${TranslateText('clicking confirm')}.`}
                    </Typography>
                    {newDeck && newDeck.defaultLocalizationPose && (
                        <DeckMapView deck={newDeck} markedRobotPosition={newDeck.defaultLocalizationPose} />
                    )}
                    <HorizontalContent>
                        <Checkbox
                            crossOrigin={undefined}
                            onChange={(e: ChangeEvent<HTMLInputElement>) => setIsCheckConfirmed(e.target.checked)}
                        />
                        <Typography>
                            {`${TranslateText('I confirm that')} ${robot.name} (${robot.model.type}) ${TranslateText(
                                'has been placed on marked position on'
                            )} `}
                            <b>{deckName}</b>
                        </Typography>
                    </HorizontalContent>
                </VerticalContent>
            </Dialog.Content>
        </>
    )
}

const PressureDialog = ({ setIsCheckConfirmed, robot, deckName }: CheckDialogProps) => {
    const { TranslateText } = useLanguageContext()

    let statusText = ''
    if (robot.pressureLevel) {
        if (
            robot.pressureLevel * 1000 > robot.model.lowerPressureWarningThreshold &&
            robot.pressureLevel * 1000 < robot.model.upperPressureWarningThreshold
        ) {
            statusText = `${TranslateText('The current pressure level is')} ${
                robot.pressureLevel * 1000
            } ${TranslateText('which is within the suggested range')}`
        } else {
            statusText = `${TranslateText('Warning')}: ${TranslateText('The current pressure level is')} ${
                robot.pressureLevel * 1000
            } ${TranslateText('which is NOT within the suggested range')}`
        }
    } else {
        statusText = TranslateText(
            'The pressure measurement is not currently available, so start the mission at your own risk.'
        )
    }

    return (
        <>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Confirm pressure level of robot')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {`${robot.name} (${robot.model.type}) ${TranslateText(
                            'needs to be have a pressure level of between'
                        )} ${robot.model.lowerPressureWarningThreshold} 
                        ${TranslateText('and')} ${robot.model.upperPressureWarningThreshold} `}
                        <b>{TranslateText('before')}</b>
                        {` ${TranslateText('clicking confirm')}. `}
                        {statusText}
                    </Typography>
                    <HorizontalContent>
                        <Checkbox
                            crossOrigin={undefined}
                            onChange={(e: ChangeEvent<HTMLInputElement>) => setIsCheckConfirmed(e.target.checked)}
                        />
                        <Typography>
                            {`${TranslateText('I confirm that')} ${robot.name} (${robot.model.type}) ${TranslateText(
                                'has a pressure level making it safe for the area it is operating in'
                            )} `}
                            <b>{deckName}</b>
                        </Typography>
                    </HorizontalContent>
                </VerticalContent>
            </Dialog.Content>
        </>
    )
}

const BatteryDialog = ({ setIsCheckConfirmed, robot, deckName }: CheckDialogProps) => {
    const { TranslateText } = useLanguageContext()

    let statusText = ''
    if (robot.batteryLevel) {
        if (!robot.model.batteryWarningThreshold || robot.batteryLevel > robot.model.batteryWarningThreshold) {
            statusText = `${TranslateText('The current battery level is')} ${robot.batteryLevel}
                            ${TranslateText('which is above the suggested minimum')}`
        } else {
            statusText = `${TranslateText('Warning')}: ${TranslateText('The current battery level is')} ${
                robot.pressureLevel
            }
                            ${TranslateText('which is LOWER than the suggested limit')}`
        }
    } else {
        statusText = TranslateText(
            'The battery measurement is not currently available, so start the mission at your own risk.'
        )
    }

    return (
        <>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Confirm battery level of robot')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {`${robot.name} (${robot.model.type}) ${TranslateText(
                            'needs to be have a battery level greater than'
                        )} ${robot.model.batteryWarningThreshold} `}
                        <b>{TranslateText('before')}</b>
                        {` ${TranslateText('clicking confirm')}. `}
                        {statusText}
                    </Typography>
                    <HorizontalContent>
                        <Checkbox
                            crossOrigin={undefined}
                            onChange={(e: ChangeEvent<HTMLInputElement>) => setIsCheckConfirmed(e.target.checked)}
                        />
                        <Typography>
                            {`${TranslateText('I confirm that')} ${robot.name} (${robot.model.type}) ${TranslateText(
                                'has a battery level making it safe for the area it is operating in'
                            )} `}
                            <b>{deckName}</b>
                        </Typography>
                    </HorizontalContent>
                </VerticalContent>
            </Dialog.Content>
        </>
    )
}

const FinalConfirmationDialog = () => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('The robot is ready to run missions')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {TranslateText('Confirm that you wish to add the selected mission(s) to the robot queue')}
                    </Typography>
                </VerticalContent>
            </Dialog.Content>
        </>
    )
}

export const ScheduleMissionChecklistDialog = ({
    closeDialog,
    scheduleMissions,
    robot,
    missionDeckName,
}: ScheduleMissionChecklistDialogProps) => {
    const { TranslateText } = useLanguageContext()
    const localisationRequired = true // This is a placeholder for when this becomes a robot parameter
    const pressureCheckRequired = robot.model.lowerPressureWarningThreshold !== null
    const [isLocalisationCheckboxClicked, setIsLocalisationCheckboxClicked] = useState<boolean>(false)
    const [isPressureCheckboxClicked, setIsPressureCheckboxClicked] = useState<boolean>(false)
    const [isBatteryCheckboxClicked, setIsBatteryCheckboxClicked] = useState<boolean>(false)

    const isReadyToSchedule =
        (!localisationRequired || isLocalisationCheckboxClicked) &&
        (!pressureCheckRequired || isPressureCheckboxClicked) &&
        isBatteryCheckboxClicked

    const CurrentConfirmationDialog = useCallback(() => {
        if (localisationRequired && !isLocalisationCheckboxClicked) {
            return (
                <LocalisationDialog
                    robot={robot}
                    setIsCheckConfirmed={setIsLocalisationCheckboxClicked}
                    deckName={missionDeckName}
                />
            )
        } else if (pressureCheckRequired && !isPressureCheckboxClicked) {
            return (
                <PressureDialog
                    robot={robot}
                    setIsCheckConfirmed={setIsPressureCheckboxClicked}
                    deckName={missionDeckName}
                />
            )
        } else if (!isBatteryCheckboxClicked) {
            return (
                <BatteryDialog
                    robot={robot}
                    setIsCheckConfirmed={setIsBatteryCheckboxClicked}
                    deckName={missionDeckName}
                />
            )
        }
        return <FinalConfirmationDialog />
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [
        missionDeckName,
        isLocalisationCheckboxClicked,
        isPressureCheckboxClicked,
        isBatteryCheckboxClicked,
        localisationRequired,
        pressureCheckRequired,
    ])

    return (
        <StyledDialog open={true} onClose={closeDialog}>
            <CurrentConfirmationDialog />
            <Dialog.Actions>
                <HorizontalContent>
                    <Button variant="outlined" onClick={closeDialog}>
                        {TranslateText('Cancel')}
                    </Button>
                    {isReadyToSchedule && (
                        <Button onClick={scheduleMissions} disabled={!isLocalisationCheckboxClicked}>
                            {TranslateText('Add mission to the queue')}
                        </Button>
                    )}
                </HorizontalContent>
            </Dialog.Actions>
        </StyledDialog>
    )
}

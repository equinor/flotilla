import { Button, Checkbox, Dialog, Typography } from '@equinor/eds-core-react'
import { InspectionAreaMapView } from 'utils/InspectionAreaMapView'
import { HorizontalContent, StyledDialog, VerticalContent } from './ScheduleMissionStyles'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import { ChangeEvent, useState } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'

interface ConfirmLocalizationDialogProps {
    closeDialog: () => void
    scheduleMissions: () => void
    robot: Robot
    newInspectionAreaName: string
}

export const ConfirmLocalizationDialog = ({
    closeDialog,
    scheduleMissions,
    robot,
    newInspectionAreaName,
}: ConfirmLocalizationDialogProps) => {
    const { TranslateText } = useLanguageContext()
    const { installationInspectionAreas } = useInstallationContext()
    const [isCheckboxClicked, setIsCheckboxClicked] = useState<boolean>(false)

    const newInspectionArea = installationInspectionAreas.find(
        (inspectionArea) => inspectionArea.inspectionAreaName === newInspectionAreaName
    )

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
                        )} ${newInspectionAreaName} `}
                        <b>{TranslateText('before')}</b>
                        {` ${TranslateText('clicking confirm')}.`}
                    </Typography>
                    {newInspectionArea && newInspectionArea.defaultLocalizationPose && (
                        <InspectionAreaMapView
                            inspectionArea={newInspectionArea}
                            markedRobotPosition={newInspectionArea.defaultLocalizationPose}
                        ></InspectionAreaMapView>
                    )}
                    <HorizontalContent>
                        <Checkbox
                            onChange={(e: ChangeEvent<HTMLInputElement>) => setIsCheckboxClicked(e.target.checked)}
                        />
                        <Typography>
                            {`${TranslateText('I confirm that')} ${robot.name} (${robot.model.type}) ${TranslateText(
                                'has been placed on marked position on'
                            )} `}
                            <b>{newInspectionAreaName}</b>
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

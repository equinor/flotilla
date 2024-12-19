import { Button, Dialog, List, Typography } from '@equinor/eds-core-react'
import { StyledDialog, VerticalContent } from './ScheduleMissionStyles'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

interface ConflictingRobotInspectionAreaDialogProps {
    closeDialog: () => void
    robotInspectionAreaName: string
    desiredInspectionAreaName: string
}

interface ConflictingMissionInspectionAreasDialogProps {
    closeDialog: () => void
    missionInspectionAreaNames: string[]
}

export const ConflictingRobotInspectionAreaDialog = ({
    closeDialog,
    robotInspectionAreaName,
    desiredInspectionAreaName,
}: ConflictingRobotInspectionAreaDialogProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledDialog open={true} onClose={closeDialog}>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Conflicting inspection areas')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {TranslateText('The missions you are trying to add are on')} <b>{desiredInspectionAreaName}</b>{' '}
                        {TranslateText('but the robot is currently running missions on')}{' '}
                        {<b>{robotInspectionAreaName}</b>}.
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

export const ConflictingMissionInspectionAreasDialog = ({
    closeDialog,
    missionInspectionAreaNames,
}: ConflictingMissionInspectionAreasDialogProps) => {
    const { TranslateText } = useLanguageContext()

    const MissionInspectionAreaNamesList = (
        <List>
            {missionInspectionAreaNames.map((inspectionAreaName) => (
                <List.Item>
                    <b>{inspectionAreaName}</b>
                </List.Item>
            ))}
        </List>
    )

    return (
        <StyledDialog open={true} onClose={closeDialog}>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Conflicting inspection areas')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {TranslateText('The missions you are trying to add are on these inspection areas:')}
                        {MissionInspectionAreaNamesList}
                    </Typography>
                    <Typography>{TranslateText('You can only add missions from one inspection area.')}</Typography>
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

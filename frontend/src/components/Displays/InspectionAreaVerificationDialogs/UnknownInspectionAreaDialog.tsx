import { Button, Dialog, Typography } from '@equinor/eds-core-react'
import { StyledDialog, VerticalContent } from './ScheduleMissionStyles'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

interface UnknownInspectionAreaDialogProps {
    closeDialog: () => void
}

export const UnknownInspectionAreaDialog = ({ closeDialog }: UnknownInspectionAreaDialogProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledDialog open={true} onClose={closeDialog}>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Unknown inspection area')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>
                        {TranslateText('Cannot start selected mission as it has unknown inspection area.')}
                    </Typography>
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

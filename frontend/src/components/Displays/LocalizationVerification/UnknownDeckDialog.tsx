import { Button, Dialog, Typography } from '@equinor/eds-core-react'
import { StyledDialog, VerticalContent } from './ScheduleMissionStyles'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

interface UnknownDeckDialogProps {
    closeDialog: () => void
}

export const UnknownDeckDialog = ({ closeDialog }: UnknownDeckDialogProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledDialog open={true} onClose={closeDialog}>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText('Unknown deck')}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>{TranslateText('Cannot start selected mission as it has unknown deck.')}</Typography>
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

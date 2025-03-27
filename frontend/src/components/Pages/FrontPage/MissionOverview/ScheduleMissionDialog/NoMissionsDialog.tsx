import { Button, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { StyledDialog } from 'components/Styles/StyledComponents'

export const NoMissionsDialog = ({ closeDialog }: { closeDialog: () => void }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledDialog open={true} isDismissable>
            <StyledDialog.Header>
                <Typography variant="h3">{TranslateText('No missions available')}</Typography>
            </StyledDialog.Header>
            <StyledDialog.Content>
                <Typography variant="body_short">
                    {TranslateText('This installation does not have missions. Please create mission.')}
                </Typography>
            </StyledDialog.Content>
            <StyledDialog.Actions>
                <Button onClick={closeDialog} variant="outlined">
                    {TranslateText('Close')}
                </Button>
            </StyledDialog.Actions>
        </StyledDialog>
    )
}

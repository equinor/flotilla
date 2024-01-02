import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { CreateEchoMissionButton } from 'components/Displays/MissionButtons/CreateEchoMissionButton'
import { StyledDialog } from 'components/Styles/StyledComponents'

const StyledMissionSection = styled.div`
    display: flex;
    justify-content: end;
    gap: 10px;
`

export const NoMissionsDialog = ({ closeDialog }: { closeDialog: () => void }): JSX.Element => {
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
            <StyledMissionSection>
                <Button onClick={closeDialog} variant="outlined">
                    {TranslateText('Cancel')}
                </Button>
                <CreateEchoMissionButton />
            </StyledMissionSection>
        </StyledDialog>
    )
}

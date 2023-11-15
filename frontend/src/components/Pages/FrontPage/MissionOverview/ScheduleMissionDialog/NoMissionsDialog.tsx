import { Button, Card, Dialog, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionButton } from 'components/Displays/MissionButtons/MissionButton'

const StyledMissionDialog = styled.div`
    display: flex;
    justify-content: space-between;
`
const StyledAutoComplete = styled(Card)`
    display: flex;
    justify-content: center;
    padding: 8px;
    gap: 25px;
    box-shadow: none;
`
const StyledMissionSection = styled.div`
    display: flex;
    margin-left: auto;
    margin-right: 0;
    gap: 10px;
`

export const NoMissionsDialog = ({ closeDialog }: { closeDialog: () => void }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledMissionDialog>
            <Dialog open={true} isDismissable>
                <StyledAutoComplete>
                    <Typography variant="h5">
                        {TranslateText('This installation has no missions - Please create mission')}
                    </Typography>
                    <StyledMissionSection>
                        <MissionButton />
                        <Button onClick={closeDialog} variant="outlined">
                            {TranslateText('Cancel')}
                        </Button>
                    </StyledMissionSection>
                </StyledAutoComplete>
            </Dialog>
        </StyledMissionDialog>
    )
}

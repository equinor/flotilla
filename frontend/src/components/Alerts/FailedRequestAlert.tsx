import { Button, Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'

const StyledDiv = styled.div`
    flex-direction: column;
`

const StyledAlertTitle = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

const StyledButton = styled(Button)`
    :hover {
        background-color: #ff9797;
    }
    text-align: left;
    height: auto;
    padding: 5px 5px;
`

export const FailedRequestAlertContent = ({ translatedMessage }: { translatedMessage: string }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
                <Typography>{TranslateText('Request error')}</Typography>
            </StyledAlertTitle>
            <StyledButton variant="ghost" color="secondary">
                {translatedMessage}
            </StyledButton>
        </StyledDiv>
    )
}

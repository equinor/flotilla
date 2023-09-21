import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'

const StyledDiv = styled.div`
    align-items: center;
    > * {
        margin-left: 1rem;
    }
`

const Indent = styled.div`
    padding: 0px 30px;
`

const StyledAlertTitle = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

export function FailedRequestAlertContent({ message }: { message: string }) {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
                <Typography>{TranslateText('Request error')}</Typography>
            </StyledAlertTitle>
            <Indent>
                <Typography variant="h4">{TranslateText(message)}</Typography>
            </Indent>
        </StyledDiv>
    )
}

import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { TextAlignedButton } from 'components/Styles/StyledComponents'

const StyledDiv = styled.div`
    align-items: center;
`

const StyledAlertTitle = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

const Indent = styled.div`
    padding: 0px 9px;
`

export const FailedSafeZoneAlertContent = ({ message }: { message: string }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
                <Typography>{TranslateText('Safe zone failure')}</Typography>
            </StyledAlertTitle>
            <Indent>
                <TextAlignedButton variant="ghost" color="secondary">
                    {TranslateText(message)}
                </TextAlignedButton>
            </Indent>
        </StyledDiv>
    )
}

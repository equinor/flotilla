import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { TextAlignedButton } from 'components/Styles/StyledComponents'
import { AlertListContents } from './AlertsListItem'

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

export const FailedAlertContent = ({ title, message }: { title: string; message: string }) => {
    const { TranslateText } = useLanguageContext()
    const iconColor = tokens.colors.interactive.danger__resting.rgba
    const bannerColor = tokens.colors.ui.background__danger.hex

    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name={Icons.Failed} style={{ color: iconColor }} />
                <Typography>{TranslateText(title)}</Typography>
            </StyledAlertTitle>
            <Indent>
                <TextAlignedButton variant="ghost" color="secondary" style={{ backgroundColor: bannerColor }}>
                    {TranslateText(message)}
                </TextAlignedButton>
            </Indent>
        </StyledDiv>
    )
}

export const FailedAlertListContent = ({ title, message }: { title: string; message: string }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <AlertListContents
            icon={Icons.Failed}
            iconColor={tokens.colors.interactive.danger__resting.rgba}
            alertTitle={TranslateText(title)}
            alertText={TranslateText(message)}
        />
    )
}

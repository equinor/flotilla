import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { AlertType } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { TextAlignedButton } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { AlertListContents } from './AlertsListItem'
import { Icons } from 'utils/icons'

const StyledDiv = styled.div`
    flex-direction: column;
`

const StyledAlertTitle = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`
interface DockBannerProps {
    alertType: AlertType
}

export const DockAlertContent = ({ alertType }: DockBannerProps) => {
    const { TranslateText } = useLanguageContext()
    const buttonBackgroundColor =
        alertType === AlertType.RequestDock
            ? tokens.colors.interactive.warning__highlight.hex
            : tokens.colors.infographic.primary__mist_blue.hex

    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name="error_outlined" />
                <Typography>
                    {alertType === AlertType.RequestDock ? TranslateText('WARNING') : TranslateText('INFO')}
                </Typography>
            </StyledAlertTitle>
            <TextAlignedButton variant="ghost" color="secondary" style={{ backgroundColor: buttonBackgroundColor }}>
                {alertType === AlertType.RequestDock && TranslateText('Dock banner text')}
                {alertType === AlertType.DockSuccess && TranslateText('Dock successful text')}
                {alertType === AlertType.DismissDock && TranslateText('Dismiss dock banner text')}
            </TextAlignedButton>
        </StyledDiv>
    )
}

export const DockAlertListContent = ({ alertType }: DockBannerProps) => {
    const { TranslateText } = useLanguageContext()
    const isWarning = alertType === AlertType.RequestDock
    const titleMessage = isWarning ? TranslateText('WARNING') : TranslateText('INFO')
    const icon = isWarning ? Icons.Warning : Icons.Info
    const iconColor = isWarning
        ? tokens.colors.interactive.danger__resting.hex
        : tokens.colors.text.static_icons__default.hex

    let message = TranslateText('Dock banner text')
    if (alertType === AlertType.DockSuccess) message = TranslateText('Dock successful text')
    if (alertType === AlertType.DismissDock) message = TranslateText('Dismiss dock banner text')

    return <AlertListContents icon={icon} alertTitle={titleMessage} alertText={message} iconColor={iconColor} />
}

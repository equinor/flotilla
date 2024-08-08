import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
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
interface SafeZoneBannerProps {
    alertType: AlertType
    alertCategory: AlertCategory
}

export const SafeZoneAlertContent = ({ alertType, alertCategory }: SafeZoneBannerProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const buttonBackgroundColor =
        alertCategory === AlertCategory.WARNING
            ? tokens.colors.interactive.warning__highlight.hex
            : tokens.colors.infographic.primary__mist_blue.hex

    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name="error_outlined" />
                <Typography>
                    {alertCategory === AlertCategory.WARNING ? TranslateText('WARNING') : TranslateText('INFO')}
                </Typography>
            </StyledAlertTitle>
            <TextAlignedButton variant="ghost" color="secondary" style={{ backgroundColor: buttonBackgroundColor }}>
                {alertCategory === AlertCategory.WARNING && TranslateText('Safe zone banner text')}
                {alertCategory === AlertCategory.INFO &&
                    alertType === AlertType.SafeZoneSuccess &&
                    TranslateText('Safe Zone successful text')}
                {alertCategory === AlertCategory.INFO &&
                    alertType === AlertType.DismissSafeZone &&
                    TranslateText('Dismiss safe zone banner text')}
            </TextAlignedButton>
        </StyledDiv>
    )
}

export const SafeZoneAlertListContent = ({ alertType, alertCategory }: SafeZoneBannerProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    let titleMessage = TranslateText('INFO')
    let message = TranslateText('Safe zone banner text')
    let icon = Icons.Warning
    let iconColor = tokens.colors.interactive.danger__resting.rgba
    if (alertCategory === AlertCategory.WARNING) titleMessage = TranslateText('WARNING')
    if (alertCategory === AlertCategory.INFO && alertType === AlertType.SafeZoneSuccess)
        [message, icon, iconColor] = [
            TranslateText('Safe Zone successful text'),
            Icons.Info,
            tokens.colors.text.static_icons__default.rgba,
        ]
    if (alertCategory === AlertCategory.INFO && alertType === AlertType.DismissSafeZone)
        [message, icon, iconColor] = [
            TranslateText('Dismiss safe zone banner text'),
            Icons.Info,
            tokens.colors.text.static_icons__default.rgba,
        ]

    return <AlertListContents icon={icon} alertTitle={titleMessage} alertText={message} iconColor={iconColor} />
}

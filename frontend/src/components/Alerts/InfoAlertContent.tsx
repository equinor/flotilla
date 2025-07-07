import { Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { AlertListContents } from './AlertsListItem'
import { AlertContainer, AlertIndent, StyledAlertIcon, StyledAlertTitle } from './AlertStyles'

export const InfoAlertContent = ({ title, message }: { title: string; message: string }) => {
    const iconColor = tokens.colors.interactive.danger__resting.hex
    return (
        <AlertContainer>
            <StyledAlertTitle>
                <StyledAlertIcon name={Icons.Info} style={{ color: iconColor }} />
                <Typography>{title}</Typography>
            </StyledAlertTitle>
            <AlertIndent>
                <Typography group="navigation" variant="button">
                    {message}
                </Typography>
            </AlertIndent>
        </AlertContainer>
    )
}

export const InfoAlertListContent = ({ title, message }: { title: string; message: string }) => {
    return (
        <AlertListContents
            icon={Icons.Info}
            iconColor={tokens.colors.interactive.primary__resting.hex}
            alertTitle={title}
            alertText={message}
        />
    )
}

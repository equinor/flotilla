import { ReactNode } from 'react'
import { Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { AlertContainer, AlertIndent, StyledAlertIcon, StyledAlertTitle } from './AlertStyles'

export const AlertBannerLayout = ({
    icon,
    iconColor,
    title,
    message,
}: {
    icon: Icons
    iconColor: string
    title: string
    message: ReactNode
}) => (
    <AlertContainer>
        <StyledAlertTitle>
            <StyledAlertIcon name={icon} style={{ color: iconColor }} />
            <Typography>{title}</Typography>
        </StyledAlertTitle>
        <AlertIndent>
            <Typography group="navigation" variant="button">
                {message}
            </Typography>
        </AlertIndent>
    </AlertContainer>
)

import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { AlertListContents } from './AlertsListItem'
import { AlertButton, AlertContainer, AlertIndent, StyledAlertIcon, StyledAlertTitle } from './AlertStyles'

interface AlertProps {
    robotNames: string[]
}

export const BlockedRobotAlertContent = ({ robotNames }: AlertProps) => {
    const { TranslateText } = useLanguageContext()
    return (
        <AlertContainer>
            <StyledAlertTitle>
                <StyledAlertIcon
                    name={Icons.Warning}
                    style={{ color: tokens.colors.interactive.danger__resting.hex }}
                />
                <Typography>{TranslateText('Robot is blocked')}</Typography>
            </StyledAlertTitle>
            <AlertIndent>
                <AlertButton variant="ghost" color="secondary">
                    {robotNames.length === 1 &&
                        `${TranslateText('The robot')} ${robotNames[0]} ${TranslateText(
                            'is blocked and cannot perform tasks'
                        )}.`}
                    {robotNames.length > 1 && TranslateText('Several robots are blocked and cannot perform tasks.')}
                </AlertButton>
            </AlertIndent>
        </AlertContainer>
    )
}

export const BlockedRobotAlertListContent = ({ robotNames }: AlertProps) => {
    const { TranslateText } = useLanguageContext()
    let message = `${TranslateText('The robot')} ${robotNames[0]} ${TranslateText('is blocked and cannot perform tasks')}.`

    if (robotNames.length > 1) message = `${TranslateText('Several robots are blocked and cannot perform tasks')}.`

    return (
        <AlertListContents
            icon={Icons.Warning}
            iconColor={tokens.colors.interactive.danger__resting.hex}
            alertTitle={TranslateText('Robot is blocked')}
            alertText={message}
        />
    )
}

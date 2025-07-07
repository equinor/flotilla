import { Icon, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { AlertListContents } from './AlertsListItem'
import { AutoScheduleFailedMissionDict } from 'components/Contexts/AlertContext'
import { AlertContainer, AlertIndent, StyledAlertIcon, StyledAlertTitle } from './AlertStyles'

export const FailedAlertContent = ({ title, message }: { title: string; message: string }) => {
    const iconColor = tokens.colors.interactive.danger__resting.hex

    return (
        <AlertContainer>
            <StyledAlertTitle>
                <StyledAlertIcon name={Icons.Failed} style={{ color: iconColor }} />
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

export const FailedAlertListContent = ({ title, message }: { title: string; message: string }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <AlertListContents
            icon={Icons.Failed}
            iconColor={tokens.colors.interactive.danger__resting.hex}
            alertTitle={TranslateText(title)}
            alertText={TranslateText(message)}
        />
    )
}

export const FailedAutoMissionAlertContent = ({
    autoScheduleFailedMissionDict,
}: {
    autoScheduleFailedMissionDict: AutoScheduleFailedMissionDict
}) => {
    const { TranslateText } = useLanguageContext()
    const iconColor = tokens.colors.interactive.danger__resting.hex

    return (
        <AlertContainer>
            <StyledAlertTitle>
                <Icon name={Icons.Failed} style={{ color: iconColor }} />
                <Typography>{TranslateText('Failed to Auto Schedule Missions')}</Typography>
            </StyledAlertTitle>
            <AlertIndent>
                {Object.keys(autoScheduleFailedMissionDict).map((missionId) => (
                    <Typography variant="caption" key={missionId}>
                        {autoScheduleFailedMissionDict[missionId]}
                    </Typography>
                ))}
            </AlertIndent>
        </AlertContainer>
    )
}

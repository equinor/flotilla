import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { Icons } from 'utils/icons'
import { AutoScheduleFailedMissionDict } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { AlertContainer, AlertIndent, StyledAlertIcon, StyledAlertTitle } from './AlertStyles'

export const AutoScheduleFailContent = ({ failedMissions }: { failedMissions: AutoScheduleFailedMissionDict }) => {
    const { TranslateText } = useLanguageContext()
    const iconColor = tokens.colors.interactive.danger__resting.hex
    return (
        <AlertContainer>
            <StyledAlertTitle>
                <StyledAlertIcon name={Icons.Failed} style={{ color: iconColor }} />
                <Typography>{TranslateText('Failed to Auto Schedule Missions')}</Typography>
            </StyledAlertTitle>
            <AlertIndent>
                {Object.keys(failedMissions).map((missionId) => (
                    <Typography variant="caption" key={missionId}>
                        {failedMissions[missionId]}
                    </Typography>
                ))}
            </AlertIndent>
        </AlertContainer>
    )
}

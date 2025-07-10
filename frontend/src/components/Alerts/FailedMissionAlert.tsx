import { config } from 'config'
import { Mission, MissionStatus } from 'models/Mission'
import { MissionStatusDisplay } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { AlertListContents } from './AlertsListItem'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { AlertContainer, AlertButton } from './AlertStyles'

interface MissionsProps {
    missions: Mission[]
}

const FailedMission = ({ missions }: MissionsProps) => {
    const mission = missions[0]
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()
    const goToMission = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }

    return (
        <AlertButton onClick={goToMission} variant="ghost" color="secondary">
            <strong>{`'${mission.name}'`}</strong> {TranslateText('failed on robot')}{' '}
            <strong>{`'${mission.robot.name}'`}:</strong> {mission.statusReason}
        </AlertButton>
    )
}

const SeveralFailedMissions = ({ missions }: MissionsProps) => {
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()
    const goToHistory = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/front-page-history`
        navigate(path)
    }

    return (
        <AlertButton onClick={goToHistory} variant="ghost" color="secondary">
            {missions.length.toString() +
                ' ' +
                TranslateText("missions failed recently. See 'Mission History' for more information.")}
        </AlertButton>
    )
}

export const FailedMissionAlertContent = ({ missions }: MissionsProps) => {
    return (
        <AlertContainer>
            <MissionStatusDisplay status={MissionStatus.Failed} />
            {missions.length === 1 && <FailedMission missions={missions} />}
            {missions.length > 1 && <SeveralFailedMissions missions={missions} />}
        </AlertContainer>
    )
}

export const FailedMissionAlertListContent = ({ missions }: MissionsProps) => {
    const { TranslateText } = useLanguageContext()
    const mission = missions[0]
    let message = `${mission.name} ${TranslateText('failed on robot')} ${mission.robot.name}: ${mission.statusReason}`
    if (mission.statusReason === null)
        message = `${mission.name} ${TranslateText('failed on robot')} ${mission.robot.name}`
    if (missions.length > 1)
        message = `${missions.length.toString()} ${TranslateText("missions failed recently. See 'Mission History' for more information.")}.`
    return missions.length === 1 ? (
        <AlertListContents
            icon={Icons.Failed}
            alertTitle={TranslateText(MissionStatus.Failed)}
            alertText={message}
            iconColor={tokens.colors.interactive.danger__resting.hex}
            mission={mission}
        />
    ) : (
        <AlertListContents
            icon={Icons.Failed}
            alertTitle={TranslateText(MissionStatus.Failed)}
            alertText={message}
            iconColor={tokens.colors.interactive.danger__resting.hex}
        />
    )
}

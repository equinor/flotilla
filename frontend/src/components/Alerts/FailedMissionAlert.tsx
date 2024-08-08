import { config } from 'config'
import { Mission, MissionStatus } from 'models/Mission'
import styled from 'styled-components'
import { MissionStatusDisplay } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { TextAlignedButton } from 'components/Styles/StyledComponents'
import { AlertListContents } from './AlertsListItem'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'

const Indent = styled.div`
    padding: 0px 0px 0px 5px;
`

const StyledButton = styled(TextAlignedButton)`
    :hover {
        background-color: #ff9797;
    }
`

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
        <StyledButton onClick={goToMission} variant="ghost" color="secondary">
            <strong>'{mission.name}'</strong> {TranslateText('failed on robot')}{' '}
            <strong>'{mission.robot.name}':</strong> {mission.statusReason}
        </StyledButton>
    )
}

const SeveralFailedMissions = ({ missions }: MissionsProps) => {
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()
    const goToHistory = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/history`
        navigate(path)
    }

    return (
        <StyledButton onClick={goToHistory} variant="ghost" color="secondary">
            {missions.length.toString() +
                ' ' +
                TranslateText("missions failed recently. See 'Mission History' for more information.")}
        </StyledButton>
    )
}

export const FailedMissionAlertContent = ({ missions }: MissionsProps) => {
    return (
        <Indent>
            <MissionStatusDisplay status={MissionStatus.Failed} />
            {missions.length === 1 && <FailedMission missions={missions} />}
            {missions.length > 1 && <SeveralFailedMissions missions={missions} />}
        </Indent>
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
            iconColor={tokens.colors.interactive.danger__resting.rgba}
            mission={mission}
        />
    ) : (
        <AlertListContents
            icon={Icons.Failed}
            alertTitle={TranslateText(MissionStatus.Failed)}
            alertText={message}
            iconColor={tokens.colors.interactive.danger__resting.rgba}
        />
    )
}

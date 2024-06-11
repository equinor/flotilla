import { config } from 'config'
import { Mission, MissionStatus } from 'models/Mission'
import styled from 'styled-components'
import { MissionStatusDisplay } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { useNavigate } from 'react-router-dom'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { TextAlignedButton } from 'components/Styles/StyledComponents'

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

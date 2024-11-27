import { Icon, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { MissionProgressDisplay } from 'components/Displays/MissionDisplays/MissionProgressDisplay'
import { MissionStatusDisplayWithHeader } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { useNavigate } from 'react-router-dom'
import { MissionControlButtons } from 'components/Displays/MissionButtons/MissionControlButtons'
import { TaskType } from 'models/Task'
import { StyledButton } from 'components/Styles/StyledComponents'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { MissionAreaDisplay } from 'components/Displays/MissionDisplays/MissionAreaDispaly'

interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled.div`
    display: flex;
    padding: 16px;
    flex-direction: column;
    align-items: flex-start;
    gap: 16px;
    flex: 1 0 0;
    align-self: stretch;
`
const TopContent = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
`
const LeftSection = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
    flex: 1 0 0;
`

const Midcontent = styled.div`
    display: flex;
    align-items: flex-start;
    gap: 24px;
`

const BottomContent = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
`
const StyledGhostButton = styled(StyledButton)`
    padding: 0;
`

export const OngoingMissionCard = ({ mission }: MissionProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()

    let navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }

    let missionTaskType = TaskType.Inspection
    if (mission.tasks.every((task) => task.type === TaskType.ReturnHome)) missionTaskType = TaskType.ReturnHome
    if (mission.tasks.every((task) => task.type === TaskType.Localization)) missionTaskType = TaskType.Localization

    return (
        <StyledMissionCard>
            <TopContent>
                <LeftSection>
                    <Typography variant="h5" style={{ color: tokens.colors.text.static_icons__default.hex }}>
                        {mission.name}
                    </Typography>
                    <Midcontent>
                        <MissionStatusDisplayWithHeader status={mission.status} />
                        <MissionAreaDisplay mission={mission} />
                        <MissionProgressDisplay mission={mission} />
                    </Midcontent>
                </LeftSection>
                <MissionControlButtons
                    missionName={mission.name}
                    missionTaskType={missionTaskType}
                    robotId={mission.robot.id}
                    missionStatus={mission.status}
                />
            </TopContent>
            <BottomContent>
                <StyledGhostButton variant="ghost" onClick={routeChange}>
                    {TranslateText('Open mission')}
                    <Icon name={Icons.RightCheveron} size={16} />
                </StyledGhostButton>
            </BottomContent>
        </StyledMissionCard>
    )
}

export const OngoingMissionPlaceholderCard = (): JSX.Element => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledMissionCard style={{ backgroundColor: tokens.colors.ui.background__light.hex }}>
            <Typography variant="h5">{TranslateText('No ongoing missions')}</Typography>
        </StyledMissionCard>
    )
}

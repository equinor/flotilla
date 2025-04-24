import { Button, Icon, Typography } from '@equinor/eds-core-react'
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
import { Robot } from 'models/Robot'
import { NoMissionReason } from 'utils/IsRobotReadyToRunMissions'

interface MissionProps {
    mission: Mission
}

const StyledLargeScreenMissionCard = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    align-self: stretch;
    justify-content: space-between;
    padding: 16px;
    gap: 16px;
    flex: 1 0 0;

    @media (max-width: 960px) {
        display: none;
    }
`
const StyledSmallScreenMissionCard = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    align-self: stretch;
    padding: 8px;

    @media (min-width: 960px) {
        display: none;
    }
`
const ControlButtonSpacing = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
`
const StyledHeader = styled.div`
    display: flex;
    flex-direction: row;
    align-self: stretch;
    justify-content: space-between;
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
const StyledGhostButton = styled(StyledButton)`
    padding: 0;
`

export const OngoingMissionCard = ({ mission }: MissionProps) => {
    const { TranslateText } = useLanguageContext()

    const navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }

    let missionTaskType = TaskType.Inspection
    if (mission.tasks.every((task) => task.type === TaskType.ReturnHome)) missionTaskType = TaskType.ReturnHome

    const SmallScreenContent = (
        <StyledSmallScreenMissionCard>
            <StyledHeader onClick={routeChange}>
                <Typography variant="h5" style={{ color: tokens.colors.text.static_icons__default.hex }}>
                    {mission.name}
                </Typography>
                <Button variant="ghost_icon">
                    <Icon name={Icons.RightCheveron} size={24} />
                </Button>
            </StyledHeader>
            <ControlButtonSpacing>
                <Midcontent>
                    <MissionStatusDisplayWithHeader status={mission.status} />
                    <MissionProgressDisplay mission={mission} />
                </Midcontent>
                <MissionControlButtons
                    missionName={mission.name}
                    missionTaskType={missionTaskType}
                    robotId={mission.robot.id}
                    missionStatus={mission.status}
                />
            </ControlButtonSpacing>
        </StyledSmallScreenMissionCard>
    )

    const LargeScreenContent = (
        <StyledLargeScreenMissionCard>
            <ControlButtonSpacing>
                <LeftSection>
                    <Typography variant="h5" style={{ color: tokens.colors.text.static_icons__default.hex }}>
                        {mission.name}
                    </Typography>
                    <Midcontent>
                        <MissionStatusDisplayWithHeader status={mission.status} />
                        <MissionProgressDisplay mission={mission} />
                    </Midcontent>
                </LeftSection>
                <MissionControlButtons
                    missionName={mission.name}
                    missionTaskType={missionTaskType}
                    robotId={mission.robot.id}
                    missionStatus={mission.status}
                />
            </ControlButtonSpacing>
            <StyledGhostButton variant="ghost" onClick={routeChange}>
                {TranslateText('Open mission')}
                <Icon name={Icons.RightCheveron} size={16} />
            </StyledGhostButton>
        </StyledLargeScreenMissionCard>
    )

    return (
        <>
            {SmallScreenContent}
            {LargeScreenContent}
        </>
    )
}

export const OngoingMissionPlaceholderCard = ({ robot }: { robot?: Robot }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            <StyledSmallScreenMissionCard
                style={{ backgroundColor: tokens.colors.ui.background__light.hex, gap: '8px' }}
            >
                <Typography variant="h5">{TranslateText('No ongoing missions')}</Typography>
                {robot && <NoMissionReason robot={robot} />}
            </StyledSmallScreenMissionCard>
            <StyledLargeScreenMissionCard style={{ backgroundColor: tokens.colors.ui.background__light.hex }}>
                <Typography variant="h5">{TranslateText('No ongoing missions')}</Typography>
                {robot && <NoMissionReason robot={robot} />}
            </StyledLargeScreenMissionCard>
        </>
    )
}

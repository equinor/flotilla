import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { tokens } from '@equinor/eds-tokens'
import { Mission, MissionStatus } from 'models/Mission'
import styled from 'styled-components'
import { MissionProgressDisplay } from 'components/Displays/MissionDisplays/MissionProgressDisplay'
import { MissionStatusDisplayWithHeader } from 'components/Displays/MissionDisplays/MissionStatusDisplay'
import { useNavigate } from 'react-router-dom'
import { MissionControlButtons } from 'components/Displays/MissionButtons/MissionControlButtons'
import { StyledButton } from 'components/Styles/StyledComponents'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { Robot } from 'models/Robot'
import { NoMissionReason } from 'utils/IsRobotReadyToRunMissions'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'

interface MissionProps {
    mission: Mission
    isOpen?: boolean
    setIsOpen?: (isOpen: boolean | ((prev: boolean) => boolean)) => void
}

interface MissionQueueButtonViewProps {
    robotId?: string
    isOpen?: boolean
    setIsOpen?: (isOpen: boolean | ((prev: boolean) => boolean)) => void
}

interface PlaceholderProps {
    robot?: Robot
    isOpen?: boolean
    setIsOpen?: (isOpen: boolean | ((prev: boolean) => boolean)) => void
}

interface ReturnHomeProps {
    robot: Robot
    isOpen?: boolean
    setIsOpen?: (isOpen: boolean | ((prev: boolean) => boolean)) => void
    isPaused: boolean
}

interface GoingToLockdownProps {
    robot: Robot
    isOpen?: boolean
    setIsOpen?: (isOpen: boolean | ((prev: boolean) => boolean)) => void
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
    position: relative;

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

const StyledDropdownButton = styled(Button)`
    display: flex;
    padding: 0 8px;
    justify-content: center;
    align-items: center;
    gap: 4px;
    max-width: 200px;
`

const StyledPlaceholder = styled.div`
    position: relative;
    display: flex;
    flex-direction: column;
    flex: 1;
    padding: 16px;
    gap: 16px;
    background-color: ${tokens.colors.ui.background__light.hex};
    align-self: stretch;

    @media (max-width: 600px) {
        padding: 8px;
        gap: 8px;
    }
`

const StyledBottomRightButtonWrapper = styled.div`
    position: absolute;
    right: 16px;
    bottom: 16px;

    @media (max-width: 600px) {
        position: static;
    }
`

const StyledWrapper = styled.div`
    display: flex;
    flex-direction: column;
    gap: 8px;
`

export const OngoingMissionCard = ({ mission, isOpen, setIsOpen }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/mission-${mission.id}`
        navigate(path)
    }

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
                    isReturnToHomeMission={false}
                    robotId={mission.robot.id}
                    missionStatus={mission.status}
                />
            </ControlButtonSpacing>
            <MissionQueueButtonView robotId={mission.robot.id} isOpen={isOpen} setIsOpen={setIsOpen} />
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
                    isReturnToHomeMission={false}
                    robotId={mission.robot.id}
                    missionStatus={mission.status}
                />
            </ControlButtonSpacing>
            <ControlButtonSpacing>
                <StyledGhostButton variant="ghost" onClick={routeChange}>
                    {TranslateText('Open mission')}
                    <Icon name={Icons.RightCheveron} size={16} />
                </StyledGhostButton>
                <MissionQueueButtonView robotId={mission.robot.id} isOpen={isOpen} setIsOpen={setIsOpen} />
            </ControlButtonSpacing>
        </StyledLargeScreenMissionCard>
    )

    return (
        <>
            {SmallScreenContent}
            {LargeScreenContent}
        </>
    )
}

export const OngoingReturnHomeMissionCard = ({ robot, isOpen, setIsOpen, isPaused }: ReturnHomeProps) => {
    const { TranslateText } = useLanguageContext()
    const missionName = TranslateText('Return robot to home')

    const missionStatus = isPaused ? MissionStatus.Paused : MissionStatus.Ongoing

    const SmallScreenContent = (
        <StyledSmallScreenMissionCard>
            <StyledHeader>
                <Typography variant="h5" style={{ color: tokens.colors.text.static_icons__default.hex }}>
                    {missionName}
                </Typography>
            </StyledHeader>
            <ControlButtonSpacing>
                <Midcontent>
                    <MissionStatusDisplayWithHeader status={missionStatus} />
                </Midcontent>
                <MissionControlButtons
                    missionName={missionName}
                    isReturnToHomeMission={true}
                    robotId={robot.id}
                    missionStatus={missionStatus}
                />
            </ControlButtonSpacing>
            <MissionQueueButtonView robotId={robot.id} isOpen={isOpen} setIsOpen={setIsOpen} />
        </StyledSmallScreenMissionCard>
    )

    const LargeScreenContent = (
        <StyledLargeScreenMissionCard>
            <ControlButtonSpacing>
                <LeftSection>
                    <Typography variant="h5" style={{ color: tokens.colors.text.static_icons__default.hex }}>
                        {missionName}
                    </Typography>
                    <Midcontent>
                        <MissionStatusDisplayWithHeader status={missionStatus} />
                    </Midcontent>
                </LeftSection>
                <MissionControlButtons
                    missionName={missionName}
                    isReturnToHomeMission={true}
                    robotId={robot.id}
                    missionStatus={missionStatus}
                />
            </ControlButtonSpacing>
            <StyledBottomRightButtonWrapper>
                <MissionQueueButtonView robotId={robot.id} isOpen={isOpen} setIsOpen={setIsOpen} />
            </StyledBottomRightButtonWrapper>
        </StyledLargeScreenMissionCard>
    )

    return (
        <>
            {SmallScreenContent}
            {LargeScreenContent}
        </>
    )
}

export const OngoingLockdownMissionCard = ({ robot, isOpen, setIsOpen }: GoingToLockdownProps) => {
    const { TranslateText } = useLanguageContext()
    const missionName = TranslateText('Return robot to home')

    const SmallScreenContent = (
        <StyledSmallScreenMissionCard>
            <StyledHeader>
                <Typography variant="h5" style={{ color: tokens.colors.text.static_icons__default.hex }}>
                    {missionName}
                </Typography>
            </StyledHeader>
            <ControlButtonSpacing>
                <Midcontent>
                    <MissionStatusDisplayWithHeader status={MissionStatus.Ongoing} />
                </Midcontent>
            </ControlButtonSpacing>
            <MissionQueueButtonView robotId={robot.id} isOpen={isOpen} setIsOpen={setIsOpen} />
        </StyledSmallScreenMissionCard>
    )

    const LargeScreenContent = (
        <StyledLargeScreenMissionCard>
            <ControlButtonSpacing>
                <LeftSection>
                    <Typography variant="h5" style={{ color: tokens.colors.text.static_icons__default.hex }}>
                        {missionName}
                    </Typography>
                    <Midcontent>
                        <MissionStatusDisplayWithHeader status={MissionStatus.Ongoing} />
                    </Midcontent>
                </LeftSection>
            </ControlButtonSpacing>
            <StyledBottomRightButtonWrapper>
                <MissionQueueButtonView robotId={robot.id} isOpen={isOpen} setIsOpen={setIsOpen} />
            </StyledBottomRightButtonWrapper>
        </StyledLargeScreenMissionCard>
    )

    return (
        <>
            {SmallScreenContent}
            {LargeScreenContent}
        </>
    )
}

export const OngoingMissionPlaceholderCard = ({ robot, isOpen, setIsOpen }: PlaceholderProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledPlaceholder>
            <Typography variant="h5">{TranslateText('No ongoing missions')}</Typography>
            <StyledWrapper>
                {robot && <NoMissionReason robot={robot} />}
                <StyledBottomRightButtonWrapper>
                    <MissionQueueButtonView robotId={robot?.id} isOpen={isOpen} setIsOpen={setIsOpen} />
                </StyledBottomRightButtonWrapper>
            </StyledWrapper>
        </StyledPlaceholder>
    )
}

const MissionQueueButtonView = ({ robotId, isOpen, setIsOpen }: MissionQueueButtonViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { missionQueue } = useMissionsContext()

    const robotMissionQueue = missionQueue.filter((m) => m.robot.id === robotId)
    const queueLength = robotMissionQueue.length

    const handleToggleMissionQueue = () => {
        if (setIsOpen) {
            setIsOpen((prev) => !prev)
        }
    }
    return (
        <>
            {queueLength >= 1 && (
                <StyledDropdownButton variant="ghost" onClick={handleToggleMissionQueue}>
                    {` ${queueLength} ${TranslateText('missions in queue')}`}
                    <Icon name={isOpen ? Icons.UpChevron : Icons.DownChevron} size={16} />
                </StyledDropdownButton>
            )}
        </>
    )
}

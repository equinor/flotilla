import { Card } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { RobotCard, RobotCardPlaceholder } from './RobotCard'
import { useAssetContext } from 'components/Contexts/RobotContext'
import { tokens } from '@equinor/eds-tokens'
import { OngoingMissionCard, OngoingMissionPlaceholderCard, OngoingReturnHomeMissionCard } from './OngoingMissionCard'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { Robot, RobotStatus } from 'models/Robot'
import { RobotMissionQueueView } from './MissionQueueView'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { NextAutoScheduleMissionView } from '../AutoScheduleSection/NextAutoScheduleMissionView'
import { useState } from 'react'

const MissionControlStyle = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
`
const MissionControlBody = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 16px;
`
const MissionControlCardStyle = styled(Card)`
    display: flex;
    gap: 0px;
    flex-direction: column;

    @media (min-width: 960px) {
        width: 960px;
    }

    @media (max-width: 960px) {
        max-width: 90hw;
        align-self: stretch;
    }
`
const OngoingMissionControlCardStyle = styled.div`
    display: flex;
    gap: 0px;
    align-items: flex-start;

    @media (min-width: 960px) {
        flex-direction: row;
    }

    @media (max-width: 960px) {
        flex-direction: column;
    }
`

export const MissionControlSection = () => {
    const { enabledRobots } = useAssetContext()

    const missionControlCards = enabledRobots.map((robot, index) => {
        return <MissionControlCard key={index} robot={robot} />
    })

    return (
        <MissionControlStyle>
            <MissionControlBody>
                {enabledRobots.length > 0 && missionControlCards}
                {enabledRobots.length === 0 && <MissionControlPlaceholderCard />}
            </MissionControlBody>
            <NextAutoScheduleMissionView />
        </MissionControlStyle>
    )
}

const MissionControlCard = ({ robot }: { robot: Robot }) => {
    const { ongoingMissions } = useMissionsContext()
    const ongoingMission = ongoingMissions.find((mission) => mission.robot.id === robot.id)
    const [isOpen, setIsOpen] = useState<boolean>(true)

    let missionCard
    switch (robot.status) {
        case RobotStatus.ReturningHome:
            missionCard = (
                <OngoingReturnHomeMissionCard robot={robot} isOpen={isOpen} setIsOpen={setIsOpen} isPaused={false} />
            )
            break
        case RobotStatus.ReturnHomePaused:
            missionCard = (
                <OngoingReturnHomeMissionCard robot={robot} isOpen={isOpen} setIsOpen={setIsOpen} isPaused={true} />
            )
            break
        case RobotStatus.Busy:
            if (ongoingMission)
                missionCard = <OngoingMissionCard mission={ongoingMission} isOpen={isOpen} setIsOpen={setIsOpen} />
            else missionCard = <OngoingMissionPlaceholderCard robot={robot} isOpen={isOpen} setIsOpen={setIsOpen} />
            break
        default:
            missionCard = <OngoingMissionPlaceholderCard robot={robot} isOpen={isOpen} setIsOpen={setIsOpen} />
            break
    }

    return (
        <MissionControlCardStyle
            id={FrontPageSectionId.RobotCard + robot.id}
            style={{ boxShadow: tokens.elevation.raised }}
        >
            <OngoingMissionControlCardStyle>
                <RobotCard robot={robot} />
                {missionCard}
            </OngoingMissionControlCardStyle>
            {isOpen && <RobotMissionQueueView robot={robot} />}
        </MissionControlCardStyle>
    )
}

const MissionControlPlaceholderCard = () => {
    return (
        <MissionControlCardStyle style={{ boxShadow: tokens.elevation.raised }}>
            <OngoingMissionControlCardStyle>
                <RobotCardPlaceholder />
                <OngoingMissionPlaceholderCard />
            </OngoingMissionControlCardStyle>
        </MissionControlCardStyle>
    )
}

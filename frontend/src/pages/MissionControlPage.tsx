import { useAlertContext } from 'components/Contexts/AlertContext'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Header } from 'components/Header/Header'
import { NavBar } from 'components/Header/NavBar'
import { useContext } from 'react'
import { Card } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { tokens } from '@equinor/eds-tokens'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { RobotWithoutTelemetry, RobotStatus } from 'models/Robot'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { useState } from 'react'
import { NextAutoScheduleMissionView } from './FrontPage/AutoScheduleSection/NextAutoScheduleMissionView'
import {
    OngoingEmergencyMissionCard,
    OngoingMissionCard,
    OngoingMissionPlaceholderCard,
    OngoingReturnHomeMissionCard,
} from './FrontPage/MissionOverview/OngoingMissionCard'
import { RobotCard, RobotCardPlaceholder } from './FrontPage/MissionOverview/RobotCard'
import { RobotMissionQueueView } from './FrontPage/MissionOverview/MissionQueueView'
import { StyledPage } from 'components/Styles/StyledComponents'

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

export const MissionControlPage = () => {
    const { alerts } = useAlertContext()
    const { installation } = useContext(InstallationContext)
    const { enabledRobots } = useAssetContext()

    const missionControlCards = enabledRobots.map((robot) => {
        return <MissionControlCard key={robot.id} robot={robot} />
    })

    return (
        <>
            <Header alertDict={alerts} installation={installation} />
            <NavBar />
            <StyledPage>
                <MissionControlStyle>
                    <MissionControlBody>
                        {enabledRobots.length > 0 && missionControlCards}
                        {enabledRobots.length === 0 && <MissionControlPlaceholderCard />}
                    </MissionControlBody>
                    <NextAutoScheduleMissionView />
                </MissionControlStyle>
            </StyledPage>
        </>
    )
}

const MissionControlCard = ({ robot }: { robot: RobotWithoutTelemetry }) => {
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
        case RobotStatus.GoingToLockdown:
        case RobotStatus.GoingToRecharging:
            missionCard = <OngoingEmergencyMissionCard robot={robot} isOpen={isOpen} setIsOpen={setIsOpen} />
            break
        case RobotStatus.ReturnHomePaused:
            missionCard = (
                <OngoingReturnHomeMissionCard robot={robot} isOpen={isOpen} setIsOpen={setIsOpen} isPaused={true} />
            )
            break
        case RobotStatus.Paused:
        case RobotStatus.Busy:
            if (ongoingMission)
                missionCard = (
                    <OngoingMissionCard
                        mission={ongoingMission}
                        canBePaused={true}
                        canBeSkipped={true}
                        isOpen={isOpen}
                        setIsOpen={setIsOpen}
                    />
                )
            else missionCard = <OngoingMissionPlaceholderCard robot={robot} isOpen={isOpen} setIsOpen={setIsOpen} />
            break
        case RobotStatus.Stopping:
        case RobotStatus.Pausing:
        case RobotStatus.StoppingReturnHome:
            if (ongoingMission)
                missionCard = (
                    <OngoingMissionCard
                        mission={ongoingMission}
                        canBePaused={false}
                        canBeSkipped={false}
                        isOpen={isOpen}
                        setIsOpen={setIsOpen}
                    />
                )
            else missionCard = <OngoingMissionPlaceholderCard robot={robot} isOpen={isOpen} setIsOpen={setIsOpen} />
            break
        case RobotStatus.RechargingWithMission:
        case RobotStatus.GoingToRechargingWithMission:
            if (ongoingMission)
                missionCard = (
                    <OngoingMissionCard
                        mission={ongoingMission}
                        canBePaused={false}
                        canBeSkipped={true}
                        isOpen={isOpen}
                        setIsOpen={setIsOpen}
                    />
                )
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

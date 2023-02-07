import { Button, Card, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useApi } from 'api/ApiCaller'
import { Mission, MissionStatus } from 'models/Mission'
import { useState, useEffect } from 'react'
import styled from 'styled-components'
import { MissionStatusDisplay } from './MissionStatusDisplay'
import { RefreshProps } from '../FrontPage'
import { useNavigate } from 'react-router-dom'
import { clear } from '@equinor/eds-icons'
import { differenceInMinutes } from 'date-fns'

const StyledCard = styled(Card)`
    width: 100%;
    display: flex;
    padding: 7px 15px;
    gap: 0.2rem;
`

const Horizontal = styled.div`
    flex-direction: row;
    display: flex;
    justify-content: space-between;
`

const Indent = styled.div`
    padding: 0px 9px;
`

const SeveralMissionPad = styled.div`
    padding: 7px 20px;
`

const Center = styled.div`
    align-items: center;
`

interface MissionProps {
    mission: Mission
}

function FailedMission({ mission }: MissionProps) {
    let navigate = useNavigate()
    const goToMission = () => {
        let path = '/robotics-frontend/mission/' + mission.id
        navigate(path)
    }

    return (
        <Button as={Typography} onClick={goToMission} variant="ghost" color="secondary">
            <strong>'{mission.name}'</strong> failed on robot <strong>'{mission.robot.name}'</strong>
        </Button>
    )
}

export function FailedMissionAlertView({ refreshInterval }: RefreshProps) {
    // The amount of minutes in the past for failed missions to generate an alert
    const FailedMissionTimeInterval: number = 10

    const apiCaller = useApi()
    const [missionsToDisplay, setMissionsToDisplay] = useState<Mission[]>([])
    const [dismissedMissions, setDismissedMissions] = useState<Mission[]>([])
    const [recentFailedMissions, setRecentFailedMissions] = useState<Mission[]>([])

    const dismissCurrentMissions = () => {
        // We don't want to store old dismissed missions
        const relevantDismissed = dismissedMissions.filter(
            (m) => differenceInMinutes(Date.now(), new Date(m.endTime!)) <= FailedMissionTimeInterval
        )
        const newDismissed = missionsToDisplay.concat(relevantDismissed)
        setDismissedMissions(newDismissed)
    }

    const updateFailedMissions = () => {
        apiCaller.getMissionsByStatus(MissionStatus.Failed).then((missions) => {
            const newRecentFailedMissions = missions.filter(
                (m) => differenceInMinutes(Date.now(), new Date(m.endTime!)) <= FailedMissionTimeInterval
            )
            setRecentFailedMissions(newRecentFailedMissions)
        })
    }

    // Display failed missions except dismissed ones
    const displayRelevantMissions = () => {
        const relevantFailedMissions = recentFailedMissions.filter(
            (m) => !dismissedMissions.map((m) => m.id).includes(m.id)
        )
        setMissionsToDisplay(relevantFailedMissions)
    }

    useEffect(() => {
        displayRelevantMissions()
    }, [dismissedMissions, recentFailedMissions])

    useEffect(() => {
        const id = setInterval(() => {
            updateFailedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    var missionDisplay = <FailedMission mission={missionsToDisplay[0]} />

    var severalMissions = (
        <SeveralMissionPad>
            <Typography>
                <strong>{missionsToDisplay.length}</strong> missions failed in the last{' '}
                <strong>{FailedMissionTimeInterval}</strong> minutes. See 'Past Missions' for more information.
            </Typography>
        </SeveralMissionPad>
    )

    return (
        <>
            {missionsToDisplay.length > 0 && (
                <StyledCard variant="danger" style={{ boxShadow: tokens.elevation.raised }}>
                    <Horizontal>
                        <Center>
                            <MissionStatusDisplay status={MissionStatus.Failed} />
                            <Indent>
                                {missionsToDisplay.length === 1 && missionDisplay}
                                {missionsToDisplay.length > 1 && severalMissions}
                            </Indent>
                        </Center>
                        <Button variant="ghost_icon" onClick={dismissCurrentMissions}>
                            <Icon data={clear}></Icon>
                        </Button>
                    </Horizontal>
                </StyledCard>
            )}
        </>
    )
}

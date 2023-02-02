import { Button, Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useApi } from 'api/ApiCaller'
import { differenceInMinutes } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { useState, useEffect } from 'react'
import styled from 'styled-components'
import { MissionStatusDisplay } from './MissionStatusDisplay'
import { RefreshProps } from '../FrontPage'
import { useNavigate } from 'react-router-dom'

const StyledCard = styled(Card)`
    width: 100%;
    display: flex;
    padding: 15px 15px;
    gap: 0.2rem;
`

const Indent = styled.div`
    padding: 2px 15px;
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
        <div>
            <MissionStatusDisplay status={mission.missionStatus} />
            <Button as={Typography} onClick={goToMission} variant="ghost" color="secondary">
                <strong>'{mission.name}'</strong> failed on robot <strong>'{mission.robot.name}'</strong>
            </Button>
        </div>
    )
}

export function FailedMissionAlertView({ refreshInterval }: RefreshProps) {
    // The amount of minutes in the past for failed missions to generate an alert
    const FailedMissionTimeInterval: number = 10

    const apiCaller = useApi()
    const [missionsToDisplay, setMissionsToDisplay] = useState<Mission[]>([])

    const updateFailedMissions = () => {
        apiCaller.getMissionsByStatus(MissionStatus.Failed).then((missions) => {
            setMissionsToDisplay(
                missions.filter(
                    (m) => differenceInMinutes(Date.now(), new Date(m.startTime)) <= FailedMissionTimeInterval
                )
            )
        })
    }

    useEffect(() => {
        const id = setInterval(() => {
            updateFailedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    var missionDisplay = <FailedMission mission={missionsToDisplay[0]} />

    var severalMissions = (
        <>
            <MissionStatusDisplay status={MissionStatus.Failed} />
            <Indent>
                <Typography>
                    <strong>{missionsToDisplay.length}</strong> missions failed in the last{' '}
                    <strong>{FailedMissionTimeInterval}</strong> minutes. See 'Recent Missions' for more information.
                </Typography>
            </Indent>
        </>
    )

    return (
        <>
            {missionsToDisplay.length > 0 && (
                <StyledCard variant="danger" style={{ boxShadow: tokens.elevation.raised }}>
                    {missionsToDisplay.length === 1 && missionDisplay}
                    {missionsToDisplay.length > 1 && severalMissions}
                </StyledCard>
            )}
        </>
    )
}

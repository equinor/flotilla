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
import { addMinutes, max } from 'date-fns'

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
    // The default amount of minutes in the past for failed missions to generate an alert
    const DefaultTimeInterval: number = 10

    // The maximum amount of minutes in the past for failed missions to generate an alert
    const MaxTimeInterval: number = 60

    const DismissalTimeSessionKeyName: string = 'lastDismissalTime'

    const apiCaller = useApi()
    const [recentFailedMissions, setRecentFailedMissions] = useState<Mission[]>([])

    const getLastDismissalTime = (): Date => {
        const sessionValue = sessionStorage.getItem(DismissalTimeSessionKeyName)

        var lastTime: Date
        if (sessionValue === null || sessionValue === '') {
            lastTime = addMinutes(Date.now(), -DefaultTimeInterval)
        } else {
            lastTime = JSON.parse(sessionValue)
            const pastLimit: Date = addMinutes(Date.now(), -MaxTimeInterval)

            // If last dismissal time was more than {MaxTimeInterval} minutes ago, use the limit value instead
            lastTime = max([pastLimit, lastTime])
        }

        return lastTime
    }

    const dismissCurrentMissions = () => {
        sessionStorage.setItem(DismissalTimeSessionKeyName, JSON.stringify(Date.now()))
        setRecentFailedMissions([])
    }

    const updateRecentFailedMissions = () => {
        const lastDismissTime: Date = getLastDismissalTime()
        apiCaller.getMissionsByStatus(MissionStatus.Failed).then((missions) => {
            const newRecentFailedMissions = missions.filter((m) => new Date(m.endTime!) > lastDismissTime)
            setRecentFailedMissions(newRecentFailedMissions)
        })
    }

    useEffect(() => {
        const id = setInterval(() => {
            updateRecentFailedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    var missionDisplay = <FailedMission mission={recentFailedMissions[0]} />

    var severalMissions = (
        <SeveralMissionPad>
            <Typography>
                <strong>{recentFailedMissions.length}</strong> missions failed recently. See 'Past Missions' for more
                information.
            </Typography>
        </SeveralMissionPad>
    )

    return (
        <>
            {recentFailedMissions.length > 0 && (
                <StyledCard variant="danger" style={{ boxShadow: tokens.elevation.raised }}>
                    <Horizontal>
                        <Center>
                            <MissionStatusDisplay status={MissionStatus.Failed} />
                            <Indent>
                                {recentFailedMissions.length === 1 && missionDisplay}
                                {recentFailedMissions.length > 1 && severalMissions}
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

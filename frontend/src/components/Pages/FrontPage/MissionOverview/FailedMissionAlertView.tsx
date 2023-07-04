import { Button, Card, Icon, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { tokens } from '@equinor/eds-tokens'
import { BackendAPICaller } from 'api/ApiCaller'
import { Mission, MissionStatus } from 'models/Mission'
import { useState, useEffect, useCallback } from 'react'
import styled from 'styled-components'
import { MissionStatusDisplay } from './MissionStatusDisplay'
import { RefreshProps } from '../FrontPage'
import { useNavigate } from 'react-router-dom'
import { addMinutes, max } from 'date-fns'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'

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

const Center = styled.div`
    align-items: center;
`

interface MissionProps {
    mission: Mission
}

interface MissionsProps {
    missions: Mission[]
}

function FailedMission({ mission }: MissionProps) {
    let navigate = useNavigate()
    const goToMission = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }

    return (
        <Button as={Typography} onClick={goToMission} variant="ghost" color="secondary">
            <strong>'{mission.name}'</strong> {TranslateText('failed on robot')}{' '}
            <strong>'{mission.robot.name}':</strong> {mission.statusReason}
        </Button>
    )
}

function SeveralFailedMissions({ missions }: MissionsProps) {
    let navigate = useNavigate()
    const goToHistory = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/history`
        navigate(path)
    }

    return (
        <Button as={Typography} onClick={goToHistory} variant="ghost" color="secondary">
            <strong>{missions.length}</strong>{' '}
            {' ' + TranslateText("missions failed recently. See 'Mission History' for more information.")}
        </Button>
    )
}

export function FailedMissionAlertView({ refreshInterval }: RefreshProps) {
    const PageSize: number = 100
    // The default amount of minutes in the past for failed missions to generate an alert
    const DefaultTimeInterval: number = 10

    // The maximum amount of minutes in the past for failed missions to generate an alert
    const MaxTimeInterval: number = 60

    const DismissalTimeSessionKeyName: string = 'lastDismissalTime'
    const [recentFailedMissions, setRecentFailedMissions] = useState<Mission[]>([])

    const getLastDismissalTime = useCallback((): Date => {
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
    }, [DefaultTimeInterval, MaxTimeInterval, DismissalTimeSessionKeyName])

    const dismissCurrentMissions = () => {
        sessionStorage.setItem(DismissalTimeSessionKeyName, JSON.stringify(Date.now()))
        setRecentFailedMissions([])
    }

    const updateRecentFailedMissions = useCallback(() => {
        const lastDismissTime: Date = getLastDismissalTime()
        BackendAPICaller.getMissions({ statuses: [MissionStatus.Failed], pageSize: PageSize }).then((missions) => {
            const newRecentFailedMissions = missions.content.filter((m) => new Date(m.endTime!) > lastDismissTime)
            setRecentFailedMissions(newRecentFailedMissions)
        })
    }, [PageSize, getLastDismissalTime])

    useEffect(() => {
        const id = setInterval(() => {
            updateRecentFailedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval, updateRecentFailedMissions])

    var missionDisplay = <FailedMission mission={recentFailedMissions[0]} />
    var severalMissions = <SeveralFailedMissions missions={recentFailedMissions} />

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
                            <Icon name={Icons.Clear}></Icon>
                        </Button>
                    </Horizontal>
                </StyledCard>
            )}
        </>
    )
}

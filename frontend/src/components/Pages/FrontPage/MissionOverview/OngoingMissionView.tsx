import { Button, Typography, Icon } from '@equinor/eds-core-react'
import { Mission, MissionStatus } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { RefreshProps } from '../FrontPage'
import { NoOngoingMissionsPlaceholder } from './NoMissionPlaceholder'
import { OngoingMissionCard } from './OngoingMissionCard'
import { Text } from 'components/Contexts/LanguageContext'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { useErrorHandler } from 'react-error-boundary'
import { PaginatedResponse } from 'models/PaginatedResponse'
import { BackendAPICaller } from 'api/ApiCaller'
import { tokens } from '@equinor/eds-tokens'

const StyledOngoingMissionView = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
`
const OngoingMissionSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
`
const ButtonStyle = styled.div`
    display: block;
`
const OngoingMissionHeader = styled.div`
    display: flex;
    flex-direction: row;
    gap: 2rem;
`
const Square = styled.div`
    width: 12px;
    height: 12px;
`


export function OngoingMissionView({ refreshInterval }: RefreshProps) {
    const missionPageSize = 100
    const handleError = useErrorHandler()
    const [ongoingMissions, setOngoingMissions] = useState<Mission[]>([])
    const [pausedMissions, setPausedMissions] = useState<Mission[]>([])
    const [missionsToDisplay, setMissionsToDisplay] = useState<Mission[]>([])

    useEffect(() => {
        updateOngoingMissions()
        updatePausedMissions()
    }, [])

    useEffect(() => {
        const id = setInterval(() => {
            updateOngoingMissions()
            updatePausedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    const getCurrentMissions = (status: MissionStatus): Promise<PaginatedResponse<Mission>> => {
        return BackendAPICaller.getMissions({ status: status, pageSize: missionPageSize, orderBy: 'StartTime desc' })
    }

    const updateOngoingMissions = () => {
        getCurrentMissions(MissionStatus.Ongoing).then((missions) => {
            setOngoingMissions(missions.content)
        })
        //.catch((e) => handleError(e))
    }

    const updatePausedMissions = () => {
        getCurrentMissions(MissionStatus.Paused).then((missions) => {
            setPausedMissions(missions.content)
        })
        //.catch((e) => handleError(e))
    }

    useEffect(() => {
        const missions: Mission[] = ongoingMissions.concat(pausedMissions)
        setMissionsToDisplay(missions)
    }, [ongoingMissions, pausedMissions])

    var missionDisplay = missionsToDisplay.map(function (mission, index) {
        return <OngoingMissionCard key={index} mission={mission} />
    })

    let navigate = useNavigate()
    const routeChange = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/history`
        navigate(path)
    }

    const stopAll = () => {
        //TO DO
        //Go to safe zone
    }

    return (
        <StyledOngoingMissionView>
            <OngoingMissionHeader>
                <Typography variant="h1" color="resting">
                    {Text('Ongoing Missions')}
                </Typography>
                <Button color="danger" variant="outlined" onClick={stopAll}>
                    <Square style={{ background: tokens.colors.interactive.danger__resting.hex }} />
                        {Text('Stop all')}
                    </Button>
            </OngoingMissionHeader>
            <OngoingMissionSection>
                {missionsToDisplay.length > 0 && missionDisplay}
                {missionsToDisplay.length === 0 && <NoOngoingMissionsPlaceholder />}
            </OngoingMissionSection>
            <ButtonStyle>
                <Button variant="outlined" onClick={routeChange}>
                    <Icon name={Icons.Historic} />
                    {Text('History')}
                </Button>
            </ButtonStyle>
        </StyledOngoingMissionView>
    )
}

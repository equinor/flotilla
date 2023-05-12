import { Button, Typography, Icon } from '@equinor/eds-core-react'
import { Mission, MissionStatus } from 'models/Mission'
import { useCallback, useEffect, useState } from 'react'
import styled from 'styled-components'
import { RefreshProps } from '../FrontPage'
import { NoOngoingMissionsPlaceholder } from './NoMissionPlaceholder'
import { OngoingMissionCard } from './OngoingMissionCard'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { PaginatedResponse } from 'models/PaginatedResponse'
import { BackendAPICaller } from 'api/ApiCaller'

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

export function OngoingMissionView({ refreshInterval }: RefreshProps) {
    const missionPageSize = 100
    const [ongoingMissions, setOngoingMissions] = useState<Mission[]>([])
    const [pausedMissions, setPausedMissions] = useState<Mission[]>([])
    const [missionsToDisplay, setMissionsToDisplay] = useState<Mission[]>([])

    const getCurrentMissions = (status: MissionStatus): Promise<PaginatedResponse<Mission>> => {
        return BackendAPICaller.getMissions({ status: status, pageSize: missionPageSize, orderBy: 'StartTime desc' })
    }

    const updateOngoingMissions = useCallback(() => {
        getCurrentMissions(MissionStatus.Ongoing).then((missions) => {
            setOngoingMissions(missions.content)
        })
    }, [])

    const updatePausedMissions = useCallback(() => {
        getCurrentMissions(MissionStatus.Paused).then((missions) => {
            setPausedMissions(missions.content)
        })
    }, [])

    useEffect(() => {
        updateOngoingMissions()
        updatePausedMissions()
    }, [updateOngoingMissions, updatePausedMissions])

    useEffect(() => {
        const id = setInterval(() => {
            updateOngoingMissions()
            updatePausedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval, updateOngoingMissions, updatePausedMissions])

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

    return (
        <StyledOngoingMissionView>
            <Typography variant="h1" color="resting">
                {TranslateText('Ongoing Missions')}
            </Typography>
            <OngoingMissionSection>
                {missionsToDisplay.length > 0 && missionDisplay}
                {missionsToDisplay.length === 0 && <NoOngoingMissionsPlaceholder />}
            </OngoingMissionSection>
            <ButtonStyle>
                <Button variant="outlined" onClick={routeChange}>
                    <Icon name={Icons.Historic} />
                    {TranslateText('History')}
                </Button>
            </ButtonStyle>
        </StyledOngoingMissionView>
    )
}
